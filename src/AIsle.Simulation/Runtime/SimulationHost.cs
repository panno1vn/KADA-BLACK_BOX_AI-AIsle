using System;
using System.Collections.Generic;
using System.Linq;
using AIsle.Contracts.Population;
using AIsle.Contracts.Simulation;
using AIsle.Simulation.Decision;
using AIsle.Simulation.Runtime.Avoidance;

namespace AIsle.Simulation.Runtime
{
    public sealed class SimulationHost
    {
        private readonly LayoutDefinition _layout; private readonly ProductDefinition[] _catalog; private readonly SimulationConfig _config;
        private readonly Random _random; private readonly HashSet<string> _catalogCategories;
        private readonly string _resultId = "sim-" + Guid.NewGuid().ToString("N"); private readonly DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
        private readonly IRvoAvoidance _avoidance; private readonly ShelfInteractionRuntime _interactions; private readonly CheckoutQueueRuntime _checkout; private bool _avoidanceFailureReported;
        public readonly PathGrid Grid; public readonly List<NPCRuntimeState> Agents = new List<NPCRuntimeState>(); public readonly List<SimulationEvent> Events = new List<SimulationEvent>(); public readonly List<PurchaseRecord> Purchases = new List<PurchaseRecord>();
        public double Time { get; private set; } public double Revenue { get; private set; } public bool Completed { get; private set; }
        public int Spawned { get; private set; } public int Converted { get; private set; } public int MainBuyers { get; private set; } public int ImpulseBuyers { get; private set; }
        public int NotFound { get; private set; } public int Unreachable { get; private set; } public int StuckRecoveries { get; private set; }
        public int MaxShelfQueueLength { get; private set; }
        public string RunId => _resultId;
        internal ShelfInteractionRuntime Interactions => _interactions;
        internal CheckoutQueueRuntime Checkout => _checkout;

        public SimulationHost(LayoutDefinition layout, ProductDefinition[] catalog, PopulationDefinition population, SimulationConfig config, IRvoAvoidance avoidance = null)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout)); _catalog = catalog ?? Array.Empty<ProductDefinition>(); if (population == null) throw new ArgumentNullException(nameof(population)); _config = config ?? new SimulationConfig();
            SimulationConfigValidator.ThrowIfInvalid(_config);
            _random = new Random();
            _avoidance = avoidance ?? new Rvo2Adapter();
            _catalogCategories = new HashSet<string>(_catalog.Select(product => product.Category), StringComparer.Ordinal); Grid = new PathGrid(_layout, _config); _interactions = new ShelfInteractionRuntime(_layout, Grid, _config); _checkout = new CheckoutQueueRuntime(_layout, Grid, _config);
            var profiles = population.NPCProfiles ?? Array.Empty<NPCProfile>(); var spawns = MakeSpawnTimes(profiles.Length);
            for (var index = 0; index < profiles.Length; index++) { var agent = new NPCRuntimeState(profiles[index].Copy(), _layout.Entrance, spawns[index], _random); Agents.Add(agent); if (!string.IsNullOrWhiteSpace(agent.Profile.TargetCategory) && !_catalogCategories.Contains(agent.Profile.TargetCategory)) NotFound++; }
        }

        public void Step(double deltaSeconds = 0.0)
        {
            if (Completed) return; var dt = SimulationMath.Clamp(deltaSeconds <= 0.0 ? _config.TickSeconds : deltaSeconds, 0.01, 2.0); Time = Math.Min(_config.DurationMinutes * 60.0, Time + dt); var active = new List<NPCRuntimeState>();var eligibleMovers=new HashSet<NPCRuntimeState>();
            for (var index = 0; index < Agents.Count; index++)
            {
                var agent = Agents[index]; if (agent.Finished || Time < agent.Spawn) continue;var wasMoving=IsMoving(agent);
                if (agent.Status == "WAITING") { agent.Status = "DECIDING"; Spawned++; Emit(agent, "spawn", "spawned"); if (!string.IsNullOrWhiteSpace(agent.Profile.TargetCategory) && !_catalogCategories.Contains(agent.Profile.TargetCategory)) Emit(agent, "phantom-need", "requested unavailable category", targetCategory: agent.Profile.TargetCategory); }
                NeedAffectSystem.Update(agent, dt, _config); UpdateAgentState(agent, dt); if (!agent.Finished){active.Add(agent);if(wasMoving&&IsMoving(agent))eligibleMovers.Add(agent);}
            }
            MoveAgents(active, eligibleMovers, dt);
            MaxShelfQueueLength = Math.Max(MaxShelfQueueLength, _interactions.MaxQueueLength);
            for (var index = 0; index < active.Count; index++) RecordTrajectory(active[index], false);
            if (Time >= _config.DurationMinutes * 60.0 || Agents.All(agent => agent.Finished)) { Completed = true; Emit(null, "complete", "simulation complete"); }
        }

        public void RunToCompletion(int maxTicks = 100000)
        {
            for (var tick = 0; tick < maxTicks && !Completed; tick++) Step(_config.TickSeconds);
            if (!Completed) throw new InvalidOperationException("Simulation did not complete within the tick limit.");
        }

        public void Decide(NPCRuntimeState agent)
        {
            if (agent.Visited.Count >= _config.MaxShelfVisits) { RouteExit(agent); return; }
            var candidates = new List<UtilityCandidate>(); var blockedCount = 0; var shelves = _layout.Shelves ?? Array.Empty<ShelfDefinition>();
            for (var shelfIndex = 0; shelfIndex < shelves.Length; shelfIndex++)
            {
                var shelf = shelves[shelfIndex]; if (agent.Visited.Contains(shelf.Id)) continue; var accessPoints = _interactions.Preview(shelf.Id, agent.Position()); if (accessPoints.Count == 0) { blockedCount++; continue; }
                var accessLimit = Math.Min(2, accessPoints.Count); var choices = accessPoints.GetRange(0, accessLimit); var access = SimulationMath.WeightedChoice(choices, item => 1.0 / Math.Pow(Math.Max(item.Length, 0.01), _config.WeightedRandomSharpness), _random);
                var shelfProducts = _catalog.Where(product => product.ShelfId == shelf.Id).ToArray();
                var evaluation = ShoppingDecisionSystem.EvaluateTarget(agent, shelf, shelfProducts, access.Length, _config);
                var noise = _random.NextDouble() * _config.DecisionNoise;
                candidates.Add(new UtilityCandidate { Shelf = shelf, Total = evaluation.Total + noise });
            }
            candidates.Sort((left, right) => right.Total.CompareTo(left.Total));
            if (candidates.Count == 0) { if (blockedCount > 0) { Unreachable++; Emit(agent, "unreachable", "no reachable shelf; returning to entrance"); } RouteExit(agent); return; }
            var limit = Math.Min(Math.Max(1, _config.TopKChoices), candidates.Count); var top = candidates.GetRange(0, limit); var best = candidates[0].Total;
            var selected = SimulationMath.WeightedChoice(top, item => Math.Exp((item.Total - best) * _config.WeightedRandomSharpness), _random);
            if (!AssignShelfAccess(agent, selected.Shelf))
            {
                Unreachable++; Emit(agent, "unreachable", "no valid interaction slot or queue position; returning to entrance");
                if (!agent.Visited.Contains(selected.Shelf.Id)) agent.Visited.Add(selected.Shelf.Id);
                RouteExit(agent); return;
            }
            Emit(agent, "decision", "chose " + selected.Shelf.Label);
        }

        public SimResult BuildResult(string name)
        {
            for (var index = 0; index < Agents.Count; index++) if (Time >= Agents[index].Spawn && !double.IsPositiveInfinity(Agents[index].Spawn)) RecordTrajectory(Agents[index], true);
            return new SimResult
            {
                Id = _resultId, CreatedAt = _createdAt, Name = name ?? string.Empty,
                Summary = new SimulationSummary { DurationSeconds = Time, Revenue = Revenue, Purchases = Purchases.Count, Spawned = Spawned, Converted = Converted, MainBuyers = MainBuyers, ImpulseBuyers = ImpulseBuyers, NotFound = NotFound, Unreachable = Unreachable, StuckRecoveries = StuckRecoveries, Completed = Completed },
                Events = Events.ToArray(), Purchases = Purchases.ToArray(), Replay = new ReplayData { SampleSeconds = SimulationMath.Clamp(_config.TrajectorySampleSeconds, 0.05, 10.0), Agents = Agents.Where(agent => !double.IsPositiveInfinity(agent.Spawn)).Select(agent => new AgentTrajectory { Id = agent.Profile.Id, Spawn = agent.Spawn, Samples = agent.Trajectory.ToArray() }).ToArray() }
            };
        }

        public SimulationStateProjection ProjectState(bool running)
        {
            var agents = new SimulationAgentProjection[Agents.Count];
            var active = 0;
            var completedAgents = 0;
            for (var index = 0; index < Agents.Count; index++)
            {
                var agent = Agents[index];
                if (agent.Finished) completedAgents++;
                else if (Time >= agent.Spawn) active++;
                agents[index] = new SimulationAgentProjection
                {
                    Id = agent.Profile.Id,
                    X = agent.X,
                    Y = agent.Y,
                    Status = agent.Status,
                    TargetId = agent.Status == "CHECKOUT_QUEUE" || agent.Status == "CHECKOUT_SERVICE" ? "checkout"
                        : agent.Status == "LEAVING" ? "entrance"
                        : agent.CurrentShelf
                };
            }

            return new SimulationStateProjection
            {
                Time = Time,
                Running = running && !Completed,
                Completed = Completed,
                Agents = agents,
                Counters = new SimulationCountersProjection
                {
                    Active = active,
                    Spawned = Spawned,
                    CompletedAgents = completedAgents,
                    Converted = Converted,
                    Purchases = Purchases.Count,
                    Revenue = Revenue,
                    Unreachable = Unreachable,
                    StuckRecoveries = StuckRecoveries
                }
            };
        }

        private void UpdateAgentState(NPCRuntimeState agent, double dt)
        {
            if (agent.Status == "DECIDING") Decide(agent);
            else if (agent.Status == "DWELL") { agent.DwellLeft -= dt; if (agent.DwellLeft <= 0.0) FinishDwell(agent); }
            else if (agent.CheckoutPhase == CheckoutPhase.Serving) { agent.DwellLeft -= dt; if (agent.DwellLeft <= 0.0) FinishCheckout(agent); }
        }

        private void MoveAgents(List<NPCRuntimeState> active, HashSet<NPCRuntimeState> eligibleMovers, double dt)
        {
            var moving = new List<NPCRuntimeState>();
            var inputs = new List<RvoAgentInput>();
            for (var index = 0; index < active.Count; index++)
            {
                var agent = active[index];
                if (!eligibleMovers.Contains(agent)) continue;
                var input = PrepareMovement(agent, dt);
                if (input == null) continue;
                moving.Add(agent);
                inputs.Add(input);
            }

            if (inputs.Count == 0) return;
            var movingCount = inputs.Count;
            var movingSet = new HashSet<NPCRuntimeState>(moving);
            for (var index = 0; index < active.Count; index++)
            {
                var agent = active[index];
                if (movingSet.Contains(agent)) continue;
                inputs.Add(new RvoAgentInput
                {
                    X=agent.X,Y=agent.Y,VelocityX=0.0,VelocityY=0.0,
                    PreferredVelocityX=0.0,PreferredVelocityY=0.0,
                    Radius=_config.CollisionRadius*0.5,MaxSpeed=0.0
                });
            }
            IReadOnlyList<RvoVelocity> velocities;
            try
            {
                velocities = _avoidance.Solve(inputs, new RvoAvoidanceSettings
                {
                    NeighborDistance = _config.RvoNeighborDistance,
                    MaxNeighbors = _config.RvoMaxNeighbors,
                    TimeHorizon = _config.RvoTimeHorizon,
                    TimeHorizonObstacles = _config.RvoTimeHorizonObstacles
                }, dt);
                if (velocities == null || velocities.Count != inputs.Count) throw new InvalidOperationException("RVO2 returned an invalid velocity set.");
            }
            catch (Exception exception)
            {
                velocities = inputs.Select(item => new RvoVelocity(item.PreferredVelocityX, item.PreferredVelocityY)).ToArray();
                if (!_avoidanceFailureReported)
                {
                    _avoidanceFailureReported = true;
                    Emit(null, "avoidance-fallback", "RVO2 unavailable; preferred velocity fallback: " + exception.GetType().Name);
                }
            }

            for (var index = 0; index < movingCount; index++) ApplyMovement(moving[index], inputs[index], velocities[index], dt);
        }

        private RvoAgentInput PrepareMovement(NPCRuntimeState agent, double dt)
        {
            if (agent.Path == null || agent.PathIndex >= agent.Path.Count)
            {
                CompleteRoute(agent);
                return null;
            }
            var target = agent.Path[agent.PathIndex]; var dx = target.X-agent.X; var dy = target.Y-agent.Y; var distance = Math.Sqrt(dx*dx+dy*dy);
            var stopTolerance = Math.Max(0.01, Math.Min(0.05, _config.PathCellSize * 0.2));
            if (distance <= stopTolerance) { AdvanceWaypoint(agent,target); return null; }

            var directionX=dx/distance;var directionY=dy/distance;var maximumSpeed=Math.Max(0.0,agent.Profile.WalkingSpeed);var pace=0.94+(0.06*Math.Sin((Time*4.0)+agent.StridePhase));
            var preferredSpeed=maximumSpeed*pace;var isFinalWaypoint=agent.PathIndex==agent.Path.Count-1;
            if(isFinalWaypoint){var slowingRadius=Math.Max(_config.PathCellSize*2.0,maximumSpeed*_config.TickSeconds*2.0);preferredSpeed*=SimulationMath.Clamp(distance/slowingRadius,0.0,1.0);}
            var preferredVelocityX=directionX*preferredSpeed;var preferredVelocityY=directionY*preferredSpeed;
            var responseSeconds=Math.Max(dt,_config.TickSeconds*2.0);var blend=SimulationMath.Clamp(dt/responseSeconds,0.0,1.0);
            var smoothedVelocityX=agent.VelocityX+((preferredVelocityX-agent.VelocityX)*blend);var smoothedVelocityY=agent.VelocityY+((preferredVelocityY-agent.VelocityY)*blend);
            var forwardSpeed=Math.Max(0.0,(smoothedVelocityX*directionX)+(smoothedVelocityY*directionY));forwardSpeed=Math.Min(maximumSpeed,forwardSpeed);
            return new RvoAgentInput
            {
                X=agent.X,Y=agent.Y,VelocityX=agent.VelocityX,VelocityY=agent.VelocityY,
                PreferredVelocityX=directionX*forwardSpeed,PreferredVelocityY=directionY*forwardSpeed,
                Radius=_config.CollisionRadius*0.5,MaxSpeed=maximumSpeed
            };
        }

        private void ApplyMovement(NPCRuntimeState agent, RvoAgentInput input, RvoVelocity actualVelocity, double dt)
        {
            if (!IsMoving(agent) || agent.Path == null || agent.PathIndex >= agent.Path.Count) return;
            var speed=Math.Sqrt((actualVelocity.X*actualVelocity.X)+(actualVelocity.Y*actualVelocity.Y));
            if(!double.IsFinite(speed)){actualVelocity=new RvoVelocity(input.PreferredVelocityX,input.PreferredVelocityY);speed=Math.Sqrt((actualVelocity.X*actualVelocity.X)+(actualVelocity.Y*actualVelocity.Y));}
            var maximumSpeed=Math.Max(0.0,agent.Profile.WalkingSpeed);
            if(speed>maximumSpeed&&speed>0.0){var scale=maximumSpeed/speed;actualVelocity=new RvoVelocity(actualVelocity.X*scale,actualVelocity.Y*scale);}

            var before=agent.Position();var target=agent.Path[agent.PathIndex];
            var next=new Position2D(agent.X+(actualVelocity.X*dt),agent.Y+(actualVelocity.Y*dt));
            if(!Grid.LineIsWalkable(before,next))
            {
                actualVelocity=new RvoVelocity(input.PreferredVelocityX,input.PreferredVelocityY);
                next=new Position2D(agent.X+(actualVelocity.X*dt),agent.Y+(actualVelocity.Y*dt));
            }
            if(!Grid.LineIsWalkable(before,next)){agent.VelocityX=0.0;agent.VelocityY=0.0;agent.StuckFor+=dt;if(agent.StuckFor>=_config.StuckTimeout)RecoverRoute(agent,"path obstructed");return;}

            agent.VelocityX=actualVelocity.X;agent.VelocityY=actualVelocity.Y;agent.X=next.X;agent.Y=next.Y;
            var moved=SimulationMath.Distance(before,next);agent.StuckFor=moved<0.001?agent.StuckFor+dt:0.0;
            var stopTolerance=Math.Max(0.01,Math.Min(0.05,_config.PathCellSize*0.2));
            var remainingX=target.X-agent.X;var remainingY=target.Y-agent.Y;
            var reached=Math.Sqrt((remainingX*remainingX)+(remainingY*remainingY))<=stopTolerance;
            var passed=((target.X-before.X)*remainingX)+((target.Y-before.Y)*remainingY)<=0.0;
            if((reached||passed)&&Grid.LineIsWalkable(agent.Position(),target))AdvanceWaypoint(agent,target);
            if(agent.StuckFor>=_config.StuckTimeout)RecoverRoute(agent,"no movement progress");
        }

        private void AdvanceWaypoint(NPCRuntimeState agent, Position2D target)
        {
            agent.X=target.X;agent.Y=target.Y;agent.PathIndex++;
            if(agent.PathIndex>=agent.Path.Count){Stop(agent);CompleteRoute(agent);}
        }

        private bool AssignShelfAccess(NPCRuntimeState agent, ShelfDefinition shelf)
        {
            var available = _interactions.Free(shelf.Id, agent.Position());
            while (available.Count > 0)
            {
                var count = Math.Min(3, available.Count);
                var top = available.GetRange(0, count);
                var selected = SimulationMath.WeightedChoice(top, item => 1.0 / Math.Pow(Math.Max(item.Length, 0.01), _config.WeightedRandomSharpness), _random);
                if (_interactions.TryReserve(selected.Slot, agent.Profile.Id))
                {
                    ApplySlotRoute(agent, shelf.Id, selected.Slot, selected.Path);
                    Emit(agent, "slot-reserve", "reserved " + selected.Slot.Id);
                    return true;
                }
                available.Remove(selected);
            }

            var queue = _interactions.TryJoinQueue(shelf.Id, agent.Profile.Id, agent.Position());
            if (queue == null) return false;
            ApplyQueueRoute(agent, queue);
            Emit(agent, "queue-join", "joined " + shelf.Id + " " + queue.Side + " queue at " + queue.Index);
            return true;
        }

        private void ApplySlotRoute(NPCRuntimeState agent, string shelfId, ShelfInteractionSlot slot, List<Position2D> path)
        {
            agent.CurrentShelf = shelfId; agent.InteractionSlotId = slot.Id; agent.QueueIndex = -1; agent.ShelfAccessPhase = ShelfAccessPhase.ApproachSlot;
            SetKnownPath(agent, path, slot.Position, "TRANSIT");
        }

        private void ApplyQueueRoute(NPCRuntimeState agent, ShelfQueueAssignment queue)
        {
            agent.CurrentShelf = queue.ShelfId; agent.InteractionSlotId = string.Empty; agent.QueueSide = queue.Side; agent.QueueIndex = queue.Index; agent.ShelfAccessPhase = ShelfAccessPhase.ApproachQueue;
            SetKnownPath(agent, queue.Path, queue.Position, "TRANSIT");
        }

        private void SetKnownPath(NPCRuntimeState agent, List<Position2D> path, Position2D target, string status)
        {
            agent.Path = path; agent.PathIndex = path.Count > 1 ? 1 : 0; agent.Status = status; agent.RouteTarget = new Position2D(target.X, target.Y); agent.RouteStatus = status; agent.StuckFor = 0; agent.Replans = 0; Stop(agent);
        }

        private void ReleaseShelfAccess(NPCRuntimeState agent, bool promote)
        {
            var released = _interactions.ReleaseSlot(agent.Profile.Id);
            var queue = _interactions.LeaveQueue(agent.Profile.Id);
            agent.ShelfAccessPhase = ShelfAccessPhase.None; agent.InteractionSlotId = string.Empty; agent.QueueIndex = -1;
            if (!promote) return;
            if (released != null) PromoteQueue(released.ShelfId, released.Side);
            if (queue.HasValue) ReflowQueue(queue.Value.ShelfId, queue.Value.Side);
        }

        private void PromoteQueue(string shelfId, ShelfSide side)
        {
            while (true)
            {
                var promotion = _interactions.TryPromote(shelfId, side, NpcPosition);
                if (promotion == null) break;
                for (var index = 0; index < promotion.IneligibleNpcIds.Length; index++)
                {
                    var skipped = FindAgent(promotion.IneligibleNpcIds[index]);
                    if (skipped == null || skipped.Finished) continue;
                    skipped.ShelfAccessPhase = ShelfAccessPhase.None; skipped.QueueIndex = -1;
                    if (!string.IsNullOrEmpty(skipped.CurrentShelf) && !skipped.Visited.Contains(skipped.CurrentShelf)) skipped.Visited.Add(skipped.CurrentShelf);
                    Emit(skipped, "queue-abandon", "queue head could not reach a released interaction slot"); skipped.CurrentShelf = string.Empty; RouteExit(skipped);
                }
                if (string.IsNullOrEmpty(promotion.NpcId) || promotion.Slot == null) break;
                var agent = FindAgent(promotion.NpcId);
                if (agent == null || agent.Finished) { _interactions.ReleaseSlot(promotion.NpcId); continue; }
                ApplySlotRoute(agent, shelfId, promotion.Slot, promotion.Path);
                Emit(agent, "queue-promote", "promoted to " + promotion.Slot.Id);
            }
            ReflowQueue(shelfId, side);
        }

        private void ReflowQueue(string shelfId, ShelfSide side)
        {
            var assignments = _interactions.Reflow(shelfId, side, NpcPosition);
            for (var index = 0; index < assignments.Count; index++)
            {
                var assignment = assignments[index]; var agent = FindAgent(assignment.NpcId);
                if (agent == null || agent.Finished) continue;
                ApplyQueueRoute(agent, assignment);
                Emit(agent, "queue-advance", "advanced to queue position " + assignment.Index);
            }
        }

        private NPCRuntimeState FindAgent(string npcId) => Agents.FirstOrDefault(item => string.Equals(item.Profile.Id, npcId, StringComparison.Ordinal));
        private Position2D NpcPosition(string npcId) { var agent = FindAgent(npcId); return agent == null || agent.Finished ? null : agent.Position(); }

        private void CompleteRoute(NPCRuntimeState agent)
        {
            Stop(agent);
            if (agent.CheckoutPhase == CheckoutPhase.ApproachService)
            {
                if (!_checkout.MarkServing(agent.Profile.Id)) { RecoverRoute(agent, "checkout service reservation was lost"); return; }
                agent.CheckoutPhase = CheckoutPhase.Serving; agent.Status = "CHECKOUT_SERVICE"; agent.DwellLeft = Math.Max(0.5, _config.TickSeconds); Emit(agent, "checkout-service", "started checkout service");
            }
            else if (agent.CheckoutPhase == CheckoutPhase.ApproachQueue)
            {
                agent.CheckoutPhase = CheckoutPhase.WaitingQueue; agent.Status = "CHECKOUT_QUEUE"; Emit(agent, "checkout-queue-wait", "waiting at checkout queue position " + agent.QueueIndex);
            }
            else if (agent.ShelfAccessPhase == ShelfAccessPhase.ApproachSlot)
            {
                if (!_interactions.MarkOccupied(agent.Profile.Id)) { RecoverRoute(agent, "interaction slot reservation was lost"); return; }
                agent.ShelfAccessPhase = ShelfAccessPhase.Interacting; agent.Status = "DWELL"; agent.DwellLeft = agent.Profile.DwellSeconds * _config.DwellScale * (0.8 + (_random.NextDouble() * 0.4)); Emit(agent, "dwell", "started dwell at " + agent.InteractionSlotId);
            }
            else if (agent.ShelfAccessPhase == ShelfAccessPhase.ApproachQueue)
            {
                agent.ShelfAccessPhase = ShelfAccessPhase.WaitingQueue; agent.Status = "QUEUE"; Emit(agent, "queue-wait", "waiting at shelf queue position " + agent.QueueIndex);
            }
            else if (agent.Status == "TRANSIT") { agent.Status = "DWELL"; agent.DwellLeft = agent.Profile.DwellSeconds * _config.DwellScale * (0.8 + (_random.NextDouble() * 0.4)); Emit(agent, "dwell", "started dwell"); }
            else { agent.Finished = true; agent.Status = "LEFT"; Emit(agent, "left", "left the store"); }
        }

        private static void Stop(NPCRuntimeState agent){agent.VelocityX=0.0;agent.VelocityY=0.0;}

        private void FinishDwell(NPCRuntimeState agent)
        {
            var shelfId = agent.CurrentShelf; var shelf = (_layout.Shelves ?? Array.Empty<ShelfDefinition>()).First(item => item.Id == shelfId); var products = _catalog.Where(product => product.ShelfId == shelfId).ToArray(); var matched = products.Where(product => product.Category == agent.Profile.TargetCategory).ToArray(); NeedAffectSystem.ApplyShelfExperience(agent,shelf.Valence);
            if (!agent.BoughtMain && matched.Length>0){var product=matched[_random.Next(matched.Length)];var evaluation=ShoppingDecisionSystem.EvaluateMainPurchase(agent,product,_config);var roll=_random.NextDouble();var bought=roll<evaluation.Probability;Emit(agent,"purchase-roll","main purchase roll",evaluation.Probability,roll,bought);if(bought)Buy(agent,product,"main");}
            if(products.Length>0){var product=products[_random.Next(products.Length)];var evaluation=ShoppingDecisionSystem.EvaluateImpulsePurchase(agent,product,_config);var roll=_random.NextDouble();var bought=roll<evaluation.Probability;Emit(agent,"impulse-roll","impulse purchase roll",evaluation.Probability,roll,bought);if(bought)Buy(agent,product,"impulse_cross_sell");}
            agent.Visited.Add(shelfId); ReleaseShelfAccess(agent, true); agent.CurrentShelf=string.Empty;if(agent.BoughtMain||agent.BoughtImpulse)RouteExit(agent);else{agent.Status="DECIDING";NeedAffectSystem.Recover(agent);}
        }

        private void Buy(NPCRuntimeState agent, ProductDefinition product, string type)
        {
            Purchases.Add(new PurchaseRecord{Time=Time,NpcId=agent.Profile.Id,ProductId=product.Id,Type=type,Price=product.Price});Revenue+=product.Price;if(type=="main"&&!agent.BoughtMain){agent.BoughtMain=true;MainBuyers++;}if(type!="main"&&!agent.BoughtImpulse){agent.BoughtImpulse=true;ImpulseBuyers++;}if(!agent.Converted){agent.Converted=true;Converted++;}Emit(agent,"purchase","bought "+product.Name,productId:product.Id,purchaseType:type);
        }
        private void RouteExit(NPCRuntimeState agent){ReleaseShelfAccess(agent,true);if(agent.Converted){if(AssignCheckout(agent))return;Unreachable++;Emit(agent,"checkout-unreachable","checkout service or FIFO line has no reachable capacity");}if(SetPath(agent,_layout.Entrance,"LEAVING",false))return;FailRoute(agent,"no route to checkout or entrance");}
        private bool AssignCheckout(NPCRuntimeState agent){var assignment=_checkout.TryEnter(agent.Profile.Id,agent.Position());if(assignment==null)return false;ApplyCheckoutRoute(agent,assignment);Emit(agent,assignment.IsService?"checkout-reserve":"checkout-queue-join",assignment.IsService?"reserved checkout service":"joined checkout FIFO at "+assignment.QueueIndex);return true;}
        private void ApplyCheckoutRoute(NPCRuntimeState agent,CheckoutAssignment assignment){agent.QueueIndex=assignment.QueueIndex;agent.CheckoutPhase=assignment.IsService?CheckoutPhase.ApproachService:CheckoutPhase.ApproachQueue;SetKnownPath(agent,assignment.Path,assignment.Position,assignment.IsService?"CHECKOUT_SERVICE":"CHECKOUT_QUEUE");}
        private void FinishCheckout(NPCRuntimeState agent){if(!agent.CheckoutPaid){agent.CheckoutPaid=true;Emit(agent,"checkout","completed checkout");}_checkout.ReleaseService(agent.Profile.Id);agent.CheckoutPhase=CheckoutPhase.None;agent.QueueIndex=-1;PromoteCheckout();if(!SetPath(agent,_layout.Entrance,"LEAVING",false))FailRoute(agent,"entrance is unreachable");}
        private void PromoteCheckout(){var promotion=_checkout.TryPromote(NpcPosition);if(promotion!=null){var promoted=FindAgent(promotion.NpcId);if(promoted!=null&&!promoted.Finished){ApplyCheckoutRoute(promoted,promotion);Emit(promoted,"checkout-queue-promote","promoted to checkout service");}}ReflowCheckout();}
        private void ReflowCheckout(){var assignments=_checkout.Reflow(NpcPosition);for(var index=0;index<assignments.Count;index++){var assignment=assignments[index];var queued=FindAgent(assignment.NpcId);if(queued==null||queued.Finished)continue;ApplyCheckoutRoute(queued,assignment);Emit(queued,"checkout-queue-advance","advanced to checkout queue position "+assignment.QueueIndex);}}
        private bool SetPath(NPCRuntimeState agent,Position2D target,string status,bool keepReplans){var path=Grid.FindPath(agent.Position(),target);if(path==null)return false;agent.Path=path;agent.PathIndex=path.Count>1?1:0;agent.Status=status;agent.RouteTarget=new Position2D(target.X,target.Y);agent.RouteStatus=status;agent.StuckFor=0;Stop(agent);if(!keepReplans)agent.Replans=0;return true;}
        private void RecoverRoute(NPCRuntimeState agent,string reason){agent.Replans++;StuckRecoveries++;Emit(agent,"replan",reason);if(agent.RouteTarget!=null&&agent.Replans<=_config.MaxReplans&&SetPath(agent,agent.RouteTarget,agent.RouteStatus,true))return;if(agent.CheckoutPhase!=CheckoutPhase.None){ReleaseCheckout(agent);Emit(agent,"checkout-abandon","abandoned unreachable checkout position");if(SetPath(agent,_layout.Entrance,"LEAVING",false))return;}if(agent.Status=="TRANSIT"||agent.Status=="QUEUE"){var shelfId=agent.CurrentShelf;ReleaseShelfAccess(agent,true);if(!string.IsNullOrEmpty(shelfId)&&!agent.Visited.Contains(shelfId))agent.Visited.Add(shelfId);Emit(agent,"abandon","abandoned unreachable shelf");agent.CurrentShelf=string.Empty;RouteExit(agent);return;}FailRoute(agent,"exit route remained blocked after replanning");}
        private void ReleaseCheckout(NPCRuntimeState agent){var released=_checkout.ReleaseService(agent.Profile.Id);var left=_checkout.LeaveQueue(agent.Profile.Id);agent.CheckoutPhase=CheckoutPhase.None;agent.QueueIndex=-1;if(released)PromoteCheckout();else if(left)ReflowCheckout();}
        private void FailRoute(NPCRuntimeState agent,string reason){ReleaseShelfAccess(agent,true);ReleaseCheckout(agent);agent.Path.Clear();Stop(agent);agent.Finished=true;agent.Status="BLOCKED";Emit(agent,"blocked",reason);}

        private static bool IsMoving(NPCRuntimeState agent)=>agent.Status=="TRANSIT"||agent.Status=="LEAVING"||agent.CheckoutPhase==CheckoutPhase.ApproachQueue||agent.CheckoutPhase==CheckoutPhase.ApproachService;

        private void RecordTrajectory(NPCRuntimeState agent,bool force)
        {
            var interval=SimulationMath.Clamp(_config.TrajectorySampleSeconds,0.05,10.0);var changed=agent.LastTrajectoryStatus!=agent.Status;if(!force&&!changed&&Time-agent.LastTrajectoryTime+1e-9<interval)return;var sample=new TrajectorySample{Time=Math.Round(Time,3),X=Math.Round(agent.X,3),Y=Math.Round(agent.Y,3),Status=agent.Status,ShelfId=agent.CurrentShelf};if(agent.Trajectory.Count>0&&agent.Trajectory[agent.Trajectory.Count-1].Time==sample.Time)agent.Trajectory[agent.Trajectory.Count-1]=sample;else agent.Trajectory.Add(sample);agent.LastTrajectoryTime=Time;agent.LastTrajectoryStatus=agent.Status;
        }

        private double[] MakeSpawnTimes(int count)
        {
            var curve=_layout.SpawnRateCurve??Array.Empty<SpawnRatePoint>();if(curve.Length==0){var meanRate=count/Math.Max(_config.DurationMinutes,1e-9);curve=new[]{new SpawnRatePoint{Minute=0,Rate=meanRate},new SpawnRatePoint{Minute=_config.DurationMinutes,Rate=meanRate}};}var sampled=PoissonSpawnSampler.Sample(curve,_config.DurationMinutes*60.0,count);var result=new double[count];for(var index=0;index<count;index++)result[index]=index<sampled.Length?sampled[index]:double.PositiveInfinity;if(count>0)result[0]=0.0;return result;
        }

        private SimulationEvent Emit(NPCRuntimeState agent,string type,string message,double probability=0,double roll=0,bool bought=false,string targetCategory="",string productId="",string purchaseType="")
        {var item=new SimulationEvent{Time=Time,NpcId=agent==null?"system":agent.Profile.Id,Type=type,Message=message,Probability=probability,Roll=roll,Bought=bought,TargetCategory=targetCategory,ProductId=productId,PurchaseType=purchaseType};Events.Add(item);return item;}
        private sealed class UtilityCandidate{public ShelfDefinition Shelf;public double Total;}
    }
}
