using System;
using System.Collections.Generic;
using System.Linq;
using AIsle.Contracts.Population;
using AIsle.Contracts.Simulation;

namespace AIsle.Simulation.Runtime
{
    public sealed class SimulationHost
    {
        private const double DistancePenaltyFloor = 0.25;
        private readonly LayoutDefinition _layout; private readonly ProductDefinition[] _catalog; private readonly SimulationConfig _config;
        private readonly Random _random; private readonly HashSet<string> _catalogCategories;
        public readonly PathGrid Grid; public readonly List<NPCRuntimeState> Agents = new List<NPCRuntimeState>(); public readonly List<SimulationEvent> Events = new List<SimulationEvent>(); public readonly List<PurchaseRecord> Purchases = new List<PurchaseRecord>();
        public double Time { get; private set; } public double Revenue { get; private set; } public bool Completed { get; private set; }
        public int Spawned { get; private set; } public int Converted { get; private set; } public int MainBuyers { get; private set; } public int ImpulseBuyers { get; private set; }
        public int NotFound { get; private set; } public int Unreachable { get; private set; } public int StuckRecoveries { get; private set; }

        public SimulationHost(LayoutDefinition layout, ProductDefinition[] catalog, PopulationDefinition population, SimulationConfig config)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout)); _catalog = catalog ?? Array.Empty<ProductDefinition>(); if (population == null) throw new ArgumentNullException(nameof(population)); _config = config ?? new SimulationConfig();
            SimulationConfigValidator.ThrowIfInvalid(_config);
            _random = new Random();
            _catalogCategories = new HashSet<string>(_catalog.Select(product => product.Category), StringComparer.Ordinal); Grid = new PathGrid(_layout, _config);
            var profiles = population.NPCProfiles ?? Array.Empty<NPCProfile>(); var spawns = MakeSpawnTimes(profiles.Length);
            for (var index = 0; index < profiles.Length; index++) { var agent = new NPCRuntimeState(profiles[index].Copy(), _layout.Entrance, spawns[index], _random); Agents.Add(agent); if (!string.IsNullOrWhiteSpace(agent.Profile.TargetCategory) && !_catalogCategories.Contains(agent.Profile.TargetCategory)) NotFound++; }
        }

        public void Step(double deltaSeconds = 0.0)
        {
            if (Completed) return; var dt = SimulationMath.Clamp(deltaSeconds <= 0.0 ? _config.TickSeconds : deltaSeconds, 0.01, 2.0); Time = Math.Min(_config.DurationMinutes * 60.0, Time + dt); var active = new List<NPCRuntimeState>();
            for (var index = 0; index < Agents.Count; index++)
            {
                var agent = Agents[index]; if (agent.Finished || Time < agent.Spawn) continue;
                if (agent.Status == "WAITING") { agent.Status = "DECIDING"; Spawned++; Emit(agent, "spawn", "spawned"); if (!string.IsNullOrWhiteSpace(agent.Profile.TargetCategory) && !_catalogCategories.Contains(agent.Profile.TargetCategory)) Emit(agent, "phantom-need", "requested unavailable category", targetCategory: agent.Profile.TargetCategory); }
                NeedAffectSystem.Update(agent, dt, _config); UpdateAgent(agent, dt); if (!agent.Finished) active.Add(agent);
            }
            Separate(active);
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
                var shelf = shelves[shelfIndex]; if (agent.Visited.Contains(shelf.Id)) continue; var accessPoints = Grid.ShelfAccessPaths(shelf, agent.Position()); if (accessPoints.Count == 0) { blockedCount++; continue; }
                var accessLimit = Math.Min(2, accessPoints.Count); var choices = accessPoints.GetRange(0, accessLimit); var access = SimulationMath.WeightedChoice(choices, item => 1.0 / Math.Pow(Math.Max(item.Length, 0.01), _config.WeightedRandomSharpness), _random);
                var match = _catalog.Any(product => product.ShelfId == shelf.Id && product.Category == agent.Profile.TargetCategory); var needAmount = match ? agent.Need : 0.0;
                var needDelta = SimulationMath.Attenuate(agent.Need, _config.NeedAttenuationSharpness) - SimulationMath.Attenuate(Math.Max(0.0, agent.Need - needAmount), _config.NeedAttenuationSharpness);
                var need = _config.UtilityNeedWeight * needDelta; var explore = _config.UtilityExploreWeight * agent.Explore; var valence = _config.UtilityValenceWeight * ((shelf.Valence + 1.0) / 2.0);
                var travel = _config.DistancePenalty * Math.Max(access.Length * access.Length, DistancePenaltyFloor); var noise = _random.NextDouble() * _config.DecisionNoise;
                candidates.Add(new UtilityCandidate { Shelf = shelf, Path = access.Path, Target = access.Point, Total = need + explore + valence - travel + noise, Need = need, Explore = explore, Valence = valence, Travel = travel });
            }
            candidates.Sort((left, right) => right.Total.CompareTo(left.Total));
            if (candidates.Count == 0) { if (blockedCount > 0) { Unreachable++; Emit(agent, "unreachable", "no reachable shelf; returning to entrance"); } RouteExit(agent); return; }
            var limit = Math.Min(Math.Max(1, _config.TopKChoices), candidates.Count); var top = candidates.GetRange(0, limit); var best = candidates[0].Total;
            var selected = SimulationMath.WeightedChoice(top, item => Math.Exp((item.Total - best) * _config.WeightedRandomSharpness), _random);
            agent.Path = selected.Path; agent.PathIndex = selected.Path.Count > 1 ? 1 : 0; agent.CurrentShelf = selected.Shelf.Id; agent.Status = "TRANSIT"; agent.RouteTarget = selected.Target; agent.RouteStatus = "TRANSIT"; agent.StuckFor = 0; agent.Replans = 0;
            Emit(agent, "decision", "chose " + selected.Shelf.Label);
        }

        public SimResult BuildResult(string name)
        {
            for (var index = 0; index < Agents.Count; index++) if (Time >= Agents[index].Spawn && !double.IsPositiveInfinity(Agents[index].Spawn)) RecordTrajectory(Agents[index], true);
            return new SimResult
            {
                Id = "sim-" + Guid.NewGuid().ToString("N"), CreatedAt = DateTimeOffset.UtcNow, Name = name ?? string.Empty,
                Summary = new SimulationSummary { DurationSeconds = Time, Revenue = Revenue, Purchases = Purchases.Count, Spawned = Spawned, Converted = Converted, MainBuyers = MainBuyers, ImpulseBuyers = ImpulseBuyers, NotFound = NotFound, Unreachable = Unreachable, StuckRecoveries = StuckRecoveries, Completed = Completed },
                Events = Events.ToArray(), Purchases = Purchases.ToArray(), Replay = new ReplayData { SampleSeconds = SimulationMath.Clamp(_config.TrajectorySampleSeconds, 0.05, 10.0), Agents = Agents.Select(agent => new AgentTrajectory { Id = agent.Profile.Id, Spawn = agent.Spawn, Samples = agent.Trajectory.ToArray() }).ToArray() }
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
                    TargetId = agent.Status == "CHECKOUT" ? "checkout"
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

        private void UpdateAgent(NPCRuntimeState agent, double dt)
        {
            if (agent.Status == "DECIDING") Decide(agent); else if (agent.Status == "TRANSIT" || agent.Status == "CHECKOUT" || agent.Status == "LEAVING") Move(agent, dt); else if (agent.Status == "DWELL") { agent.DwellLeft -= dt; if (agent.DwellLeft <= 0.0) FinishDwell(agent); }
            RecordTrajectory(agent, false);
        }

        private void Move(NPCRuntimeState agent, double dt)
        {
            if (agent.Path == null || agent.PathIndex >= agent.Path.Count)
            {
                if (agent.Status == "TRANSIT") { agent.Status = "DWELL"; agent.DwellLeft = agent.Profile.DwellSeconds * _config.DwellScale * (0.8 + (_random.NextDouble() * 0.4)); Emit(agent, "dwell", "started dwell"); }
                else if (agent.Status == "CHECKOUT") { Emit(agent, "checkout", "completed checkout"); if (!SetPath(agent, _layout.Entrance, "LEAVING", false)) FailRoute(agent, "entrance is unreachable"); }
                else { agent.Finished = true; agent.Status = "LEFT"; Emit(agent, "left", "left the store"); }
                return;
            }
            var target = agent.Path[agent.PathIndex]; var dx = target.X-agent.X; var dy = target.Y-agent.Y; var distance = Math.Sqrt(dx*dx+dy*dy); if (distance < 1e-6) { agent.PathIndex++; return; }
            var pace = 0.94 + (0.06 * Math.Sin((Time * 4.0) + agent.StridePhase)); var step = agent.Profile.WalkingSpeed * pace * dt;
            var next = distance <= step ? new Position2D(target.X,target.Y) : new Position2D(agent.X+(dx/distance*step),agent.Y+(dy/distance*step));
            if (!Grid.LineIsWalkable(agent.Position(), next)) { agent.StuckFor += dt; if (agent.StuckFor >= _config.StuckTimeout) RecoverRoute(agent, "path obstructed"); return; }
            var moved = SimulationMath.Distance(agent.Position(),next); agent.X=next.X;agent.Y=next.Y;agent.StuckFor=moved<0.001?agent.StuckFor+dt:0.0;if(distance<=step)agent.PathIndex++;if(agent.StuckFor>=_config.StuckTimeout)RecoverRoute(agent,"no movement progress");
        }

        private void FinishDwell(NPCRuntimeState agent)
        {
            var shelf = (_layout.Shelves ?? Array.Empty<ShelfDefinition>()).First(item => item.Id == agent.CurrentShelf); var products = _catalog.Where(product => product.ShelfId == agent.CurrentShelf).ToArray(); var matched = products.Where(product => product.Category == agent.Profile.TargetCategory).ToArray(); NeedAffectSystem.ApplyShelfExperience(agent,shelf.Valence);
            if (!agent.BoughtMain && matched.Length>0){var probability=SimulationMath.Sigmoid((_config.PurchaseNeedA*agent.Need)+(_config.PurchaseValenceB*agent.Valence)+_config.PurchaseBiasC);var roll=_random.NextDouble();var bought=roll<probability;Emit(agent,"purchase-roll","main purchase roll",probability,roll,bought);if(bought)Buy(agent,matched[_random.Next(matched.Length)],"main");}
            if(products.Length>0){var probability=_config.ImpulseBase*((agent.Valence+1.0)/2.0);var roll=_random.NextDouble();var bought=roll<probability;Emit(agent,"impulse-roll","impulse purchase roll",probability,roll,bought);if(bought)Buy(agent,products[_random.Next(products.Length)],"impulse_cross_sell");}
            agent.Visited.Add(agent.CurrentShelf);agent.CurrentShelf=string.Empty;if(agent.BoughtMain||agent.BoughtImpulse)RouteExit(agent);else{agent.Status="DECIDING";NeedAffectSystem.Recover(agent);}
        }

        private void Buy(NPCRuntimeState agent, ProductDefinition product, string type)
        {
            Purchases.Add(new PurchaseRecord{Time=Time,NpcId=agent.Profile.Id,ProductId=product.Id,Type=type,Price=product.Price});Revenue+=product.Price;if(type=="main"&&!agent.BoughtMain){agent.BoughtMain=true;MainBuyers++;}if(type!="main"&&!agent.BoughtImpulse){agent.BoughtImpulse=true;ImpulseBuyers++;}if(!agent.Converted){agent.Converted=true;Converted++;}Emit(agent,"purchase","bought "+product.Name,productId:product.Id,purchaseType:type);
        }
        private void RouteExit(NPCRuntimeState agent){if(agent.Converted&&SetPath(agent,_layout.Checkout,"CHECKOUT",false))return;if(SetPath(agent,_layout.Entrance,"LEAVING",false))return;FailRoute(agent,"no route to checkout or entrance");}
        private bool SetPath(NPCRuntimeState agent,Position2D target,string status,bool keepReplans){var path=Grid.FindPath(agent.Position(),target);if(path==null)return false;agent.Path=path;agent.PathIndex=path.Count>1?1:0;agent.Status=status;agent.RouteTarget=new Position2D(target.X,target.Y);agent.RouteStatus=status;agent.StuckFor=0;if(!keepReplans)agent.Replans=0;return true;}
        private void RecoverRoute(NPCRuntimeState agent,string reason){agent.Replans++;StuckRecoveries++;Emit(agent,"replan",reason);if(agent.RouteTarget!=null&&agent.Replans<=_config.MaxReplans&&SetPath(agent,agent.RouteTarget,agent.RouteStatus,true))return;if(agent.Status=="TRANSIT"){if(!string.IsNullOrEmpty(agent.CurrentShelf)&&!agent.Visited.Contains(agent.CurrentShelf))agent.Visited.Add(agent.CurrentShelf);Emit(agent,"abandon","abandoned unreachable shelf");agent.CurrentShelf=string.Empty;RouteExit(agent);return;}FailRoute(agent,"exit route remained blocked after replanning");}
        private void FailRoute(NPCRuntimeState agent,string reason){agent.Path.Clear();agent.Finished=true;agent.Status="BLOCKED";Emit(agent,"blocked",reason);}

        private void Separate(List<NPCRuntimeState> active)
        {
            for(var first=0;first<active.Count;first++)for(var second=first+1;second<active.Count;second++){var a=active[first];var b=active[second];var dx=a.X-b.X;var dy=a.Y-b.Y;var distance=Math.Sqrt(dx*dx+dy*dy);if(distance<=0.0||distance>=_config.CollisionRadius)continue;var push=(_config.CollisionRadius-distance)/_config.CollisionRadius*_config.SeparationStrength*0.5;var pa=new Position2D(a.X+dx/distance*push,a.Y+dy/distance*push);var pb=new Position2D(b.X-dx/distance*push,b.Y-dy/distance*push);if(Grid.LineIsWalkable(a.Position(),pa)){a.X=pa.X;a.Y=pa.Y;}if(Grid.LineIsWalkable(b.Position(),pb)){b.X=pb.X;b.Y=pb.Y;}}
        }

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
        private sealed class UtilityCandidate{public ShelfDefinition Shelf;public List<Position2D> Path;public Position2D Target;public double Total;public double Need;public double Explore;public double Valence;public double Travel;}
    }
}
