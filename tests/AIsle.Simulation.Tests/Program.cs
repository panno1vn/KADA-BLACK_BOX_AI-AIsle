using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AIsle.Contracts.Population;
using AIsle.Contracts.Simulation;
using AIsle.Simulation.Decision;
using AIsle.Simulation.Runtime;
using AIsle.Simulation.Runtime.Avoidance;

internal static class Program
{
    private static int Main()
    {
        try
        {
            TestPoissonSpawn(); TestNeedAndAffect(); TestConfigValidation(); TestPathRules(); TestUtility(); TestShoppingDecisionSeparation();
            TestUnreachableAndPhantom(); TestBoundedRecoveryAndAbandon(); TestMovementAndArrival(); TestNoPurchaseJourney();
            TestFullJourneyAndResult(); TestStateProjection(); TestShelfInteractionSlots(); TestShelfReservationAndQueue(); TestShelfQueueJourney();
            TestRvoHeadOn(); TestRvoCrossingAndCrowd(); TestRvoFallbackAndNoNeighbor();
            Console.WriteLine("PASS: C# simulation baseline verification completed."); return 0;
        }
        catch(Exception exception) { Console.Error.WriteLine("FAIL: " + exception); return 1; }
    }

    private static void TestPoissonSpawn()
    {
        var gaps = new System.Collections.Generic.List<double>(); var curve = new[] { new SpawnRatePoint { Minute=0,Rate=12 }, new SpawnRatePoint { Minute=10,Rate=12 } };
        for(var run=1;run<=80;run++){var arrivals=PoissonSpawnSampler.Sample(curve,600,int.MaxValue);var previous=0.0;for(var index=0;index<arrivals.Length;index++){gaps.Add(arrivals[index]-previous);previous=arrivals[index];}}
        var mean=gaps.Average();Assert(Math.Abs(mean-5.0)<0.3,"Poisson mean interval outside tolerance: "+mean);
        var layout=OpenLayout(Array.Empty<ShelfDefinition>());layout.SpawnRateCurve=curve;var host=new SimulationHost(layout,Array.Empty<ProductDefinition>(),Population(Profile("immediate","")),new SimulationConfig{DurationMinutes=10,TickSeconds=.2});Assert(host.Agents[0].Spawn==0,"First live NPC must be scheduled at T=0");host.Step(.2);Assert(host.Spawned==1&&host.Agents[0].Status!="WAITING","First live NPC did not start on the first tick");
        Console.WriteLine("PASS RUN2-02 Poisson spawn mean="+mean.ToString("F3")+"; first NPC immediate");
    }

    private static void TestNeedAndAffect()
    {
        var profile=Profile("need","drink");profile.InitialNeed=0.2;profile.NeedGrowthPerMinute=0.03;profile.InitialExplorationNeed=0.3;profile.ExplorationGrowthPerMinute=0.02;profile.AffectAttractor=0.1;profile.AffectDispersion=0.5;profile.AffectStability=0.2;profile.AffectRecovery=0.25;
        var agent=new NPCRuntimeState(profile,new Position2D(),0,new Random(1));var config=new SimulationConfig();NeedAffectSystem.Update(agent,60,config);AssertClose(0.23,agent.Need,1e-12,"Need growth changed");AssertClose(0.32,agent.Explore,1e-12,"Explore growth changed");NeedAffectSystem.ApplyShelfExperience(agent,0.9);AssertClose(0.42,agent.Valence,1e-12,"Affect update changed");NeedAffectSystem.Recover(agent);AssertClose(0.34,agent.Valence,1e-12,"Affect recovery changed");Console.WriteLine("PASS RUN2-03/04 Need and Affect");
    }

    private static void TestPathRules()
    {
        var config=new SimulationConfig{PathCellSize=0.2,ObstacleMargin=0.2};var sealedLayout=new LayoutDefinition{Width=6,Height=4,Entrance=new Position2D(1,2),Checkout=new Position2D(1.5,2),Walls=new[]{new WallDefinition{Id="barrier",X1=3,Y1=0,X2=3,Y2=4}}};
        var sealedGrid=new PathGrid(sealedLayout,config);Assert(sealedGrid.FindPath(new Position2D(1,2),new Position2D(5,2))==null,"Sealed wall was crossed");sealedLayout.Walls[0].Y2=2.8;var gapGrid=new PathGrid(sealedLayout,config);var path=gapGrid.FindPath(new Position2D(1,1),new Position2D(5,1));Assert(path!=null&&path.Count>2,"A* did not route through gap");for(var i=1;i<path.Count;i++)Assert(gapGrid.LineIsWalkable(path[i-1],path[i]),"Smoothed path is blocked");
        var cornerLayout=new LayoutDefinition{Width=5,Height=5,Entrance=new Position2D(1,1),Checkout=new Position2D(4,4),Walls=new[]{new WallDefinition{Id="vertical",X1=2,Y1=0,X2=2,Y2=2},new WallDefinition{Id="horizontal",X1=0,Y1=2,X2=2,Y2=2}}};
        var cornerGrid=new PathGrid(cornerLayout,new SimulationConfig{PathCellSize=0.2,ObstacleMargin=0.12});var cornerPath=cornerGrid.FindPath(new Position2D(1,1),new Position2D(3,3));Assert(cornerPath==null,"A* escaped through a diagonally touching blocked corner");
        Console.WriteLine("PASS S4.2 A* wall, corner and unreachable invariants");
    }

    private static void TestShelfInteractionSlots()
    {
        var config = new SimulationConfig { PathCellSize = .2, ObstacleMargin = .12, CollisionRadius = .32 };
        var shelf = new ShelfDefinition { Id = "slots", Label = "Slots", X = 3, Y = 2, Width = 2, Height = 1 };
        var layout = new LayoutDefinition { Width = 8, Height = 6, Entrance = new Position2D(1, 3), Checkout = new Position2D(1.5, 3), Shelves = new[] { shelf } };
        var grid = new PathGrid(layout, config);
        var slots = grid.ShelfInteractionSlots(shelf);
        Assert(slots.Count(item => item.Side == ShelfSide.North) > 1 && slots.Count(item => item.Side == ShelfSide.South) > 1, "I1 horizontal shelf did not derive multiple North/South slots.");
        Assert(slots.Count(item => item.Side == ShelfSide.East) > 1 && slots.Count(item => item.Side == ShelfSide.West) > 1, "I2 vertical side did not derive multiple slots.");
        Assert(slots.All(item => grid.IsPointWalkable(item.Position)), "I5 generated slot is not walkable.");
        Assert(slots.All(item => item.Position.X < shelf.X || item.Position.X > shelf.X + shelf.Width || item.Position.Y < shelf.Y || item.Position.Y > shelf.Y + shelf.Height), "I5 generated slot lies inside shelf geometry.");
        var minimumCornerDistance = config.CollisionRadius * .5 - 1e-9;
        Assert(slots.Where(item => item.Side is ShelfSide.North or ShelfSide.South).All(item => item.Position.X - shelf.X >= minimumCornerDistance && shelf.X + shelf.Width - item.Position.X >= minimumCornerDistance), "I4 horizontal corner padding is below agent radius.");
        var shortShelf = new ShelfDefinition { Id = "short", X = 6, Y = 4, Width = .25, Height = .25 };
        var shortLayout = new LayoutDefinition { Width = 8, Height = 6, Entrance = new Position2D(1, 3), Checkout = new Position2D(1.5, 3), Shelves = new[] { shortShelf } };
        Assert(new PathGrid(shortLayout, config).ShelfInteractionSlots(shortShelf).Count <= 4, "I3 short shelf generated excessive capacity.");
        var northY = slots.First(item => item.Side == ShelfSide.North).Position.Y;
        layout.Walls = new[] { new WallDefinition { Id = "north-block", X1 = shelf.X - .5, Y1 = northY, X2 = shelf.X + shelf.Width + .5, Y2 = northY } };
        var blockedSlots = new PathGrid(layout, config).ShelfInteractionSlots(shelf);
        Assert(blockedSlots.All(item => item.Side != ShelfSide.North), "I6 blocked North side remained usable.");
        Assert(blockedSlots.Any(item => item.Side != ShelfSide.North), "I6 blocking one side removed every other accessible side.");
        Console.WriteLine("PASS T10 I1-I7 dynamic shelf interaction slot geometry and reachability");
    }

    private static void TestShelfReservationAndQueue()
    {
        var config = new SimulationConfig { PathCellSize = .2, ObstacleMargin = .12, CollisionRadius = .32 };
        var shelf = new ShelfDefinition { Id = "capacity", X = 3, Y = 2, Width = 1, Height = 1 };
        var layout = new LayoutDefinition { Width = 8, Height = 8, Entrance = new Position2D(1, 3), Checkout = new Position2D(1.5, 3), Shelves = new[] { shelf } };
        var grid = new PathGrid(layout, config);
        var runtime = new ShelfInteractionRuntime(layout, grid, config);
        var owners = new List<string>();
        foreach (var slot in runtime.Slots)
        {
            var owner = "owner-" + owners.Count;
            Assert(runtime.TryReserve(slot, owner), "R1 free slot could not be reserved.");
            Assert(!runtime.TryReserve(slot, "duplicate"), "R1 slot accepted a second owner.");
            owners.Add(owner);
        }
        Assert(runtime.Slots.Select(item => item.OwnerNpcId).Distinct().Count() == runtime.Slots.Count, "R2 multiple capacity did not retain distinct owners.");
        var a = runtime.TryJoinQueue(shelf.Id, "A", new Position2D(1, 3));
        var b = runtime.TryJoinQueue(shelf.Id, "B", new Position2D(1, 3));
        var c = runtime.TryJoinQueue(shelf.Id, "C", new Position2D(1, 3));
        Assert(a != null && b != null && c != null && runtime.TotalQueueLength == 3, "R3/Q1 full shelf did not create queue entries.");
        Assert(new[] { a.Position.X + ":" + a.Position.Y, b.Position.X + ":" + b.Position.Y, c.Position.X + ":" + c.Position.Y }.Distinct().Count() == 3, "Q2 queue positions are not unique.");
        var side = a.Side;
        var released = runtime.Slots.First(item => item.Side == side);
        runtime.ReleaseSlot(released.OwnerNpcId);
        var positions = new Dictionary<string, Position2D> { ["A"] = a.Position, ["B"] = b.Position, ["C"] = c.Position };
        var promotedA = runtime.TryPromote(shelf.Id, side, id => positions.TryGetValue(id, out var position) ? position : null);
        Assert(promotedA != null && promotedA.NpcId == "A" && promotedA.Slot.OwnerNpcId == "A", "Q1/Q3 FIFO head was not reserved before promotion.");
        runtime.ReleaseSlot("A");
        var promotedB = runtime.TryPromote(shelf.Id, side, id => positions.TryGetValue(id, out var position) ? position : null);
        Assert(promotedB != null && promotedB.NpcId == "B", "Q5 newer queue member bypassed the FIFO head.");
        runtime.ReleaseSlot("B");
        var promotedC = runtime.TryPromote(shelf.Id, side, id => positions.TryGetValue(id, out var position) ? position : null);
        Assert(promotedC != null && promotedC.NpcId == "C" && runtime.TotalQueueLength == 0, "Q4 queue did not compact/promote in FIFO order.");
        Console.WriteLine("PASS T10 R1-R6/Q1-Q6 unique reservation and FIFO queue lifecycle");
    }

    private static void TestShelfQueueJourney()
    {
        var shelf = new ShelfDefinition { Id = "hotspot", Label = "Hotspot", Category = "drink", X = 4, Y = 3, Width = 1, Height = 1, Valence = .5 };
        var layout = new LayoutDefinition { Width = 10, Height = 8, Entrance = new Position2D(1, 4), Checkout = new Position2D(2, 4), Shelves = new[] { shelf }, SpawnRateCurve = new[] { new SpawnRatePoint { Minute = 0, Rate = 100000 } } };
        var profiles = Enumerable.Range(0, 20).Select(index => { var profile = Profile("queue-" + index, "drink"); profile.WalkingSpeed = 1.5; profile.DwellSeconds = .3; profile.InitialNeed = 1; return profile; }).ToArray();
        var config = new SimulationConfig { DurationMinutes = 5, TickSeconds = .1, PathCellSize = .2, ObstacleMargin = .12, CollisionRadius = .32, MaxShelfVisits = 1, TopKChoices = 1, DecisionNoise = 0, PurchaseNeedA = 10, PurchaseBiasC = 10, PurchaseValenceB = 0 };
        var host = new SimulationHost(layout, new[] { new ProductDefinition { Id = "drink", Name = "Drink", Category = "drink", ShelfId = shelf.Id, Price = 10 } }, Population(profiles), config);
        for (var index = 0; index < host.Agents.Count; index++)
        {
            var agent = host.Agents[index]; agent.Spawn = 0; agent.X = .8 + ((index % 5) * .4); agent.Y = 3.2 + ((index / 5) * .4);
        }
        for (var tick = 0; tick < 6000 && !host.Completed; tick++)
        {
            host.Step(config.TickSeconds);
            var owned = host.Interactions.Slots.Where(item => !string.IsNullOrEmpty(item.OwnerNpcId)).Select(item => item.OwnerNpcId).ToArray();
            Assert(owned.Distinct().Count() == owned.Length, "R1 duplicate slot owner appeared during same-tick crowd processing.");
        }
        Assert(host.MaxShelfQueueLength > 0 && host.Events.Any(item => item.Type == "queue-join") && host.Events.Any(item => item.Type == "queue-promote"), "R3/Q3 hotspot did not exercise queue and promotion.");
        Assert(host.Completed && host.Agents.All(item => item.Finished), "C5 full shelf journey did not terminate: time=" + host.Time + ", queue=" + host.Interactions.TotalQueueLength + ", states=" + string.Join(",", host.Agents.GroupBy(item => item.Status).Select(group => group.Key + "=" + group.Count())));
        Assert(host.Interactions.Slots.All(item => item.State == ShelfSlotState.Free) && host.Interactions.TotalQueueLength == 0, "R4-R6 completed journey leaked slot or queue ownership.");
        Console.WriteLine("PASS T10 C1/C3-C5 20-agent shelf queue journey terminates without reservation leaks");
    }

    private static void TestConfigValidation()
    {
        SimulationConfigValidator.ThrowIfInvalid(new SimulationConfig());
        foreach(var invalid in new[]{new SimulationConfig{TickSeconds=0},new SimulationConfig{PathCellSize=0},new SimulationConfig{ImpulseBase=2},new SimulationConfig{MaxReplans=9},new SimulationConfig{RvoNeighborDistance=.1},new SimulationConfig{RvoMaxNeighbors=0},new SimulationConfig{RvoTimeHorizon=0}})
        {
            var rejected=false;try{SimulationConfigValidator.ThrowIfInvalid(invalid);}catch(ArgumentException){rejected=true;}Assert(rejected,"Invalid SimulationConfig was accepted");
        }
        Console.WriteLine("PASS S4.1 SimulationConfig defaults and bounds");
    }

    private static void TestUtility()
    {
        var layout=OpenLayout(new[]{new ShelfDefinition{Id="near",Label="Near",X=2.4,Y=1.2,Width=1,Height=1,Valence=0.1},new ShelfDefinition{Id="far",Label="Far",X=7.6,Y=1.2,Width=1,Height=1,Valence=0.9}});
        var catalog=new[]{new ProductDefinition{Id="near-p",Name="Near",Category="near-cat",ShelfId="near",Price=10},new ProductDefinition{Id="far-p",Name="Far",Category="far-cat",ShelfId="far",Price=10}};
        var config=new SimulationConfig{TopKChoices=1,DecisionNoise=0,UtilityExploreWeight=0};var farHost=new SimulationHost(layout,catalog,Population(Profile("far-npc","far-cat")),config);farHost.Decide(farHost.Agents[0]);Assert(farHost.Agents[0].CurrentShelf=="far","Strong far need lost");var nearHost=new SimulationHost(layout,catalog,Population(Profile("near-npc","near-cat")),config);nearHost.Decide(nearHost.Agents[0]);Assert(nearHost.Agents[0].CurrentShelf=="near","Near choice lost quadratic travel bias");Console.WriteLine("PASS RUN2-05/06 Smart Object and Utility AI");
    }

    private static void TestUnreachableAndPhantom()
    {
        var layout=new LayoutDefinition{Width=6,Height=4,Entrance=new Position2D(1,2),Checkout=new Position2D(1.5,2),Walls=new[]{new WallDefinition{Id="barrier",X1=3,Y1=0,X2=3,Y2=4}},Shelves=new[]{new ShelfDefinition{Id="isolated",Label="Isolated",X=4.4,Y=1.4,Width=1,Height=1,Valence=1}},SpawnRateCurve=new[]{new SpawnRatePoint{Minute=0,Rate=600}}};
        var catalog=new[]{new ProductDefinition{Id="p",Name="Drink",Category="drink",ShelfId="isolated",Price=10}};var profile=Profile("blocked","missing-category");var host=new SimulationHost(layout,catalog,Population(profile),new SimulationConfig{DurationMinutes=1,PathCellSize=0.2,ObstacleMargin=0.2});host.Agents[0].Spawn=0;for(var i=0;i<80&&!host.Completed;i++)host.Step(0.2);Assert(host.Events.Any(item=>item.Type=="phantom-need"),"Phantom need not traced");Assert(host.Events.Any(item=>item.Type=="unreachable"),"Unreachable shelf not traced");Assert(host.Events.Any(item=>item.Type=="left"),"Unreachable NPC did not exit");Assert(host.Agents[0].X<3,"NPC crossed sealed wall");Console.WriteLine("PASS RUN2-12/13 phantom, unreachable and exit flow");
    }

    private static void TestFullJourneyAndResult()
    {
        var shelf=new ShelfDefinition{Id="s1",Label="Drink",X=3,Y=1.2,Width=1,Height=1,Valence=0.5};var layout=OpenLayout(new[]{shelf});layout.SpawnRateCurve=new[]{new SpawnRatePoint{Minute=0,Rate=600},new SpawnRatePoint{Minute=1,Rate=600}};
        var catalog=new[]{new ProductDefinition{Id="drink",Name="Drink",Category="drink",ShelfId="s1",Price=12.5}};var profile=Profile("buyer","drink");profile.InitialNeed=1;profile.DwellSeconds=0.2;profile.WalkingSpeed=1.5;
        var config=new SimulationConfig{DurationMinutes=1,TickSeconds=0.1,TopKChoices=1,DecisionNoise=0,PurchaseNeedA=10,PurchaseValenceB=0,PurchaseBiasC=10,TrajectorySampleSeconds=0.2};var host=new SimulationHost(layout,catalog,Population(profile),config);host.Agents[0].Spawn=0;host.RunToCompletion(5000);
        Assert(host.Events.Any(item=>item.Type=="decision")&&host.Events.Any(item=>item.Type=="purchase")&&host.Events.Any(item=>item.Type=="checkout")&&host.Events.Any(item=>item.Type=="left"),"Full event journey incomplete");Assert(host.Purchases.Count>=1,"Purchase missing");Assert(host.Agents[0].Trajectory.Count>2,"Trajectory missing");var result=host.BuildResult("baseline");Assert(result.SchemaVersion=="aisle.sim-result.v1"&&result.Summary.Completed&&result.Replay.Columns.Length==5,"SimResult contract invalid");AssertClose(result.Purchases.Sum(item=>item.Price),result.Summary.Revenue,1e-12,"Revenue and purchase records disagree");Assert(result.Summary.Purchases==result.Purchases.Length&&result.Summary.Converted==host.Agents.Count(item=>item.Converted),"Result counters disagree with runtime state");Assert(result.Replay.Agents.SelectMany(item=>item.Samples).All(item=>host.Grid.IsPointWalkable(new Position2D(item.X,item.Y))),"Journey trajectory penetrated geometry");var json=JsonSerializer.Serialize(result,new JsonSerializerOptions{IncludeFields=true});var roundTrip=JsonSerializer.Deserialize<SimResult>(json,new JsonSerializerOptions{IncludeFields=true});Assert(roundTrip!=null&&roundTrip.Purchases.Length==result.Purchases.Length&&roundTrip.Replay.Agents[0].Samples.Length>2,"SimResult serialization failed");Console.WriteLine("PASS RUN2-08..15/R5-R6 shelf arrival, full journey, trace, trajectory and SimResult");
    }

    private static void TestShoppingDecisionSeparation()
    {
        var shelf=new ShelfDefinition{Id="s",Label="Shelf",Category="drink",X=3,Y=1,Width=1,Height=1};
        var product=new ProductDefinition{Id="p",Name="Drink",Category="drink",ShelfId="s",Price=5};
        var config=new SimulationConfig{DecisionNoise=0,UtilityExploreWeight=0,DistancePenalty=.05,PurchaseNeedA=3,PurchaseBiasC=-2,ImpulseBase=.2};
        var lowProfile=Profile("low","drink");lowProfile.InitialNeed=.2;lowProfile.PriceSensitivity=1;lowProfile.Impulsiveness=.2;
        var highProfile=lowProfile.Copy();highProfile.Id="high";highProfile.InitialNeed=.9;
        var lowAgent=new NPCRuntimeState(lowProfile,new Position2D(),0,new Random(1));var highAgent=new NPCRuntimeState(highProfile,new Position2D(),0,new Random(1));
        var lowNeed=ShoppingDecisionSystem.EvaluateTarget(lowAgent,shelf,new[]{product},2,config);var highNeed=ShoppingDecisionSystem.EvaluateTarget(highAgent,shelf,new[]{product},2,config);
        Assert(highNeed.Total>=lowNeed.Total,"D2 higher matching need reduced target utility");
        var near=ShoppingDecisionSystem.EvaluateTarget(highAgent,shelf,new[]{product},1,config);var far=ShoppingDecisionSystem.EvaluateTarget(highAgent,shelf,new[]{product},4,config);
        Assert(far.Total<=near.Total,"D3 longer travel improved target utility");
        var cheap=ShoppingDecisionSystem.EvaluateMainPurchase(highAgent,new ProductDefinition{Category="drink",Price=1},config);var expensive=ShoppingDecisionSystem.EvaluateMainPurchase(highAgent,new ProductDefinition{Category="drink",Price=100},config);
        Assert(expensive.Probability<=cheap.Probability,"D4 higher price increased purchase tendency for a price-sensitive NPC");
        var impulsiveProfile=highProfile.Copy();impulsiveProfile.Impulsiveness=.9;var impulsiveAgent=new NPCRuntimeState(impulsiveProfile,new Position2D(),0,new Random(1));
        var lowImpulse=ShoppingDecisionSystem.EvaluateImpulsePurchase(highAgent,product,config);var highImpulse=ShoppingDecisionSystem.EvaluateImpulsePurchase(impulsiveAgent,product,config);
        Assert(highImpulse.Probability>=lowImpulse.Probability,"D5 higher impulsiveness reduced impulse tendency");

        var blockedLayout=new LayoutDefinition{Width=6,Height=4,Entrance=new Position2D(1,2),Checkout=new Position2D(1,1),Walls=new[]{new WallDefinition{Id="barrier",X1=3,Y1=0,X2=3,Y2=4}},Shelves=new[]{new ShelfDefinition{Id="blocked",Label="Blocked",Category="drink",X=4.2,Y=1,Width=1,Height=1}}};
        var blockedProduct=new ProductDefinition{Id="blocked-p",Name="Blocked",Category="drink",ShelfId="blocked",Price=1};var blockedHost=new SimulationHost(blockedLayout,new[]{blockedProduct},Population(Profile("blocked-choice","drink")),config);blockedHost.Decide(blockedHost.Agents[0]);
        Assert(blockedHost.Agents[0].CurrentShelf!="blocked","D1 unreachable shelf was selected");
        Console.WriteLine("PASS S8.1 D1-D5 separated target and purchase decisions");
    }

    private static void TestNoPurchaseJourney()
    {
        var shelf=new ShelfDefinition{Id="s1",Label="Drink",Category="drink",X=3,Y=1.2,Width=1,Height=1,Valence=0};var layout=OpenLayout(new[]{shelf});
        var catalog=new[]{new ProductDefinition{Id="drink",Name="Drink",Category="drink",ShelfId="s1",Price=12.5}};var profile=Profile("browser","drink");profile.DwellSeconds=0.1;
        var config=new SimulationConfig{DurationMinutes=1,TickSeconds=0.1,TopKChoices=1,DecisionNoise=0,PurchaseNeedA=0,PurchaseValenceB=0,PurchaseBiasC=-100,ImpulseBase=0,MaxShelfVisits=1};
        var host=new SimulationHost(layout,catalog,Population(profile),config);host.Agents[0].Spawn=0;host.RunToCompletion(5000);
        Assert(host.Completed&&host.Purchases.Count==0,"No-purchase journey did not terminate cleanly");Assert(host.Events.Any(item=>item.Type=="purchase-roll"&&!item.Bought)&&host.Events.Any(item=>item.Type=="left"),"No-purchase journey trace incomplete");
        Console.WriteLine("PASS S4.3 no-purchase and exit journey");
    }

    private static void TestBoundedRecoveryAndAbandon()
    {
        var shelf=new ShelfDefinition{Id="s1",Label="Shelf",Category="drink",X=1.8,Y=0.8,Width=.6,Height=.6,Valence=0};
        var layout=new LayoutDefinition{Width=6,Height=4,Entrance=new Position2D(1,2),Checkout=new Position2D(1.4,2),Shelves=new[]{shelf},Walls=new[]{new WallDefinition{Id="barrier",X1=3,Y1=0,X2=3,Y2=4}},SpawnRateCurve=new[]{new SpawnRatePoint{Minute=0,Rate=600}}};
        var config=new SimulationConfig{DurationMinutes=1,TickSeconds=.2,PathCellSize=.2,ObstacleMargin=.2,StuckTimeout=.2,MaxReplans=2,TopKChoices=1,DecisionNoise=0};
        var host=new SimulationHost(layout,new[]{new ProductDefinition{Id="p",Name="P",Category="drink",ShelfId="s1",Price=1}},Population(Profile("recover","drink")),config);var agent=host.Agents[0];agent.Spawn=0;host.Step(.2);
        agent.Status="TRANSIT";agent.CurrentShelf="s1";agent.Path=new System.Collections.Generic.List<Position2D>{agent.Position(),new Position2D(3,2)};agent.PathIndex=1;agent.RouteTarget=new Position2D(5,2);agent.RouteStatus="TRANSIT";
        for(var tick=0;tick<30&&!host.Events.Any(item=>item.Type=="abandon");tick++)host.Step(.2);
        Assert(host.Events.Count(item=>item.Type=="replan")<=config.MaxReplans+1,"Recovery exceeded its configured bound");Assert(host.Events.Any(item=>item.Type=="abandon"),"Failed route was not abandoned; events="+string.Join(",",host.Events.Select(item=>item.Type)));Assert(agent.X<3,"Recovery crossed sealed geometry");host.RunToCompletion(5000);Assert(host.Completed&&agent.Finished,"Blocked-target journey did not terminate");
        Console.WriteLine("PASS S4.2 bounded replan and abandon");
    }

    private static void TestMovementAndArrival()
    {
        var config=new SimulationConfig{DurationMinutes=1,TickSeconds=.1,PathCellSize=.2,ObstacleMargin=.12,StuckTimeout=2};var profile=Profile("mover","drink");profile.WalkingSpeed=1.4;profile.DwellSeconds=5;
        var straightHost=new SimulationHost(OpenLayout(Array.Empty<ShelfDefinition>()),Array.Empty<ProductDefinition>(),Population(profile),config);var straight=straightHost.Agents[0];var target=new Position2D(3.03,1.7);PrepareRoute(straight,new[]{straight.Position(),target});
        var previousDistance=SimulationMathForTest(straight,target);var peakSpeed=0.0;var nearMovingSpeed=double.PositiveInfinity;
        for(var tick=0;tick<400&&straight.Status!="DWELL";tick++)
        {
            straightHost.Step(.1);var speed=straight.Speed();var distance=SimulationMathForTest(straight,target);peakSpeed=Math.Max(peakSpeed,speed);if(distance<.4&&speed>0)nearMovingSpeed=Math.Min(nearMovingSpeed,speed);
            Assert(speed<=profile.WalkingSpeed+1e-9,"M1 actual speed exceeded WalkingSpeed");Assert(straight.X<=target.X+1e-9,"M3 straight movement overshot its target");Assert(distance<=previousDistance+1e-9,"M4 distance increased near a straight target");Assert(straightHost.Grid.IsPointWalkable(straight.Position()),"M5 straight movement entered blocked geometry");previousDistance=distance;
        }
        Assert(straight.Status=="DWELL"&&SimulationMathForTest(straight,target)<1e-9&&straight.Speed()==0,"M2 agent did not arrive and stop at the access point");Assert(nearMovingSpeed<peakSpeed,"M2 agent did not slow down before arrival");

        var turnHost=new SimulationHost(OpenLayout(Array.Empty<ShelfDefinition>()),Array.Empty<ProductDefinition>(),Population(Profile("turn","drink")),config);var turn=turnHost.Agents[0];var corner=new Position2D(2,1.7);var turnTarget=new Position2D(2,2.8);PrepareRoute(turn,new[]{turn.Position(),corner,turnTarget});var reachedCorner=false;
        for(var tick=0;tick<400&&turn.Status!="DWELL";tick++){turnHost.Step(.1);reachedCorner|=Math.Abs(turn.X-corner.X)<1e-9&&Math.Abs(turn.Y-corner.Y)<1e-9;Assert(turn.Speed()<=turn.Profile.WalkingSpeed+1e-9,"M1 speed bound failed at 90-degree turn");Assert(turnHost.Grid.IsPointWalkable(turn.Position()),"M5 90-degree turn entered blocked geometry");}
        Assert(reachedCorner&&turn.Status=="DWELL"&&SimulationMathForTest(turn,turnTarget)<1e-9,"M2/M4 90-degree route missed a waypoint or oscillated");

        var corridorLayout=new LayoutDefinition{Width=6,Height=4,Entrance=new Position2D(1,2),Checkout=new Position2D(1,2.5),Walls=new[]{new WallDefinition{Id="top",X1=.2,Y1=1.25,X2=5.5,Y2=1.25},new WallDefinition{Id="bottom",X1=.2,Y1=2.75,X2=5.5,Y2=2.75}}};var corridorHost=new SimulationHost(corridorLayout,Array.Empty<ProductDefinition>(),Population(Profile("corridor","drink")),config);var corridor=corridorHost.Agents[0];var corridorTarget=new Position2D(5,2);var corridorPath=corridorHost.Grid.FindPath(corridor.Position(),corridorTarget);Assert(corridorPath!=null,"M5 narrow corridor path was not found");PrepareRoute(corridor,corridorPath.ToArray());
        for(var tick=0;tick<600&&corridor.Status!="DWELL";tick++){corridorHost.Step(.1);Assert(corridorHost.Grid.IsPointWalkable(corridor.Position()),"M5 narrow path penetrated a wall");Assert(corridor.Speed()<=corridor.Profile.WalkingSpeed+1e-9,"M1 narrow path exceeded speed bound");}
        Assert(corridor.Status=="DWELL"&&SimulationMathForTest(corridor,corridorTarget)<1e-9,"M2 narrow path did not arrive and stop");
        Console.WriteLine("PASS S8.2/R4 M1-M5 smooth speed, arrival, overshoot, oscillation and static geometry");
    }

    private static void PrepareRoute(NPCRuntimeState agent,Position2D[] path){agent.Spawn=0;agent.Status="TRANSIT";agent.CurrentShelf="synthetic";agent.Path=new System.Collections.Generic.List<Position2D>(path);agent.PathIndex=path.Length>1?1:0;agent.RouteTarget=path[path.Length-1];agent.RouteStatus="TRANSIT";agent.VelocityX=0;agent.VelocityY=0;}
    private static double SimulationMathForTest(NPCRuntimeState agent,Position2D target)=>Math.Sqrt(((agent.X-target.X)*(agent.X-target.X))+((agent.Y-target.Y)*(agent.Y-target.Y)));

    private static void TestStateProjection()
    {
        var host=new SimulationHost(OpenLayout(Array.Empty<ShelfDefinition>()),Array.Empty<ProductDefinition>(),Population(Profile("projection","missing")),new SimulationConfig{DurationMinutes=1});host.Agents[0].Spawn=0;host.Step(.2);
        var projection=host.ProjectState(false);Assert(projection.Agents.Length==1&&projection.Agents[0].Id=="projection"&&projection.Counters.Spawned==1,"State projection changed");
        var json=JsonSerializer.Serialize(projection,new JsonSerializerOptions{IncludeFields=true});using var document=JsonDocument.Parse(json);var agent=document.RootElement.GetProperty("Agents")[0];
        Assert(agent.TryGetProperty("X",out _)&&agent.TryGetProperty("Y",out _)&&agent.TryGetProperty("Status",out _)&&agent.TryGetProperty("TargetId",out _),"Projection is missing required fields");Assert(!agent.TryGetProperty("Path",out _)&&!agent.TryGetProperty("Profile",out _),"Projection leaked internal simulation objects");
        Console.WriteLine("PASS S4.5 serializable minimal projection");
    }

    private static void TestRvoHeadOn()
    {
        var config=new SimulationConfig{DurationMinutes=1,TickSeconds=.1,RvoNeighborDistance=3,RvoTimeHorizon=2};
        var host=new SimulationHost(OpenLayout(Array.Empty<ShelfDefinition>()),Array.Empty<ProductDefinition>(),Population(Profile("left",""),Profile("right","")),config);
        var left=host.Agents[0];var right=host.Agents[1];left.X=2;left.Y=2;right.X=7;right.Y=2;
        PrepareRoute(left,new[]{left.Position(),new Position2D(7,2)});PrepareRoute(right,new[]{right.Position(),new Position2D(2,2)});
        var minimum=double.PositiveInfinity;
        for(var tick=0;tick<800&&(left.Status!="DWELL"||right.Status!="DWELL");tick++){host.Step(.1);minimum=Math.Min(minimum,SimulationMathForTest(left,right.Position()));}
        Assert(minimum>=config.CollisionRadius*.8,"R1 head-on agents collided; minimum="+minimum);
        Assert(left.Status=="DWELL"&&right.Status=="DWELL","R1 head-on agents did not both reach their goals");
        Console.WriteLine("PASS R1 ORCA head-on avoidance minimum="+minimum.ToString("F3"));
    }

    private static void TestRvoCrossingAndCrowd()
    {
        var config=new SimulationConfig{DurationMinutes=1,TickSeconds=.1,RvoNeighborDistance=2.5,RvoMaxNeighbors=12};
        var eastProfile=Profile("east","");var northProfile=Profile("north","");eastProfile.DwellSeconds=1000;northProfile.DwellSeconds=1000;
        var crossing=new SimulationHost(OpenLayout(Array.Empty<ShelfDefinition>()),Array.Empty<ProductDefinition>(),Population(eastProfile,northProfile),config);
        var east=crossing.Agents[0];var north=crossing.Agents[1];east.X=2;east.Y=2;north.X=4.5;north.Y=.5;
        PrepareRoute(east,new[]{east.Position(),new Position2D(7,2)});PrepareRoute(north,new[]{north.Position(),new Position2D(4.5,3.5)});
        var crossingMinimum=double.PositiveInfinity;
        for(var tick=0;tick<800&&(east.Status!="DWELL"||north.Status!="DWELL");tick++){crossing.Step(.1);crossingMinimum=Math.Min(crossingMinimum,SimulationMathForTest(east,north.Position()));}
        Assert(crossingMinimum>=config.CollisionRadius*.75,"R2 crossing agents severely overlapped");Assert(east.Status=="DWELL"&&north.Status=="DWELL","R2 crossing paths did not terminate");

        var profiles=Enumerable.Range(0,12).Select(index=>{var profile=Profile("crowd-"+index,"");profile.DwellSeconds=1000;return profile;}).ToArray();var crowd=new SimulationHost(OpenLayout(Array.Empty<ShelfDefinition>()),Array.Empty<ProductDefinition>(),Population(profiles),config);var starts=new double[profiles.Length];var minimum=double.PositiveInfinity;
        for(var index=0;index<crowd.Agents.Count;index++){var agent=crowd.Agents[index];agent.X=1+(index*.38);agent.Y=1.7;starts[index]=agent.X;PrepareRoute(agent,new[]{agent.Position(),new Position2D(10,1.7)});}
        for(var tick=0;tick<80;tick++){crowd.Step(.1);for(var a=0;a<crowd.Agents.Count;a++)for(var b=a+1;b<crowd.Agents.Count;b++)minimum=Math.Min(minimum,SimulationMathForTest(crowd.Agents[a],crowd.Agents[b].Position()));}
        Assert(minimum>=config.CollisionRadius*.65,"R3 aisle crowd developed severe overlap; minimum="+minimum);Assert(crowd.Agents.Count(agent=>agent.X>starts[Array.IndexOf(crowd.Agents.ToArray(),agent)]+.2)>=9,"R3 aisle crowd made insufficient progress");
        Assert(crowd.Agents.All(agent=>crowd.Grid.IsPointWalkable(agent.Position())),"R4 crowd movement penetrated static geometry");
        Console.WriteLine("PASS R2-R4 crossing/crowd/wall invariants minimum="+minimum.ToString("F3"));
    }

    private static void TestRvoFallbackAndNoNeighbor()
    {
        var adapter=new Rvo2Adapter();var input=new RvoAgentInput{PreferredVelocityX=.7,PreferredVelocityY=-.2,MaxSpeed=1,Radius=.16};
        var output=adapter.Solve(new[]{input},new RvoAvoidanceSettings{NeighborDistance=2,MaxNeighbors=10,TimeHorizon=2,TimeHorizonObstacles=2},.1);
        AssertClose(input.PreferredVelocityX,output[0].X,1e-12,"R7 no-neighbor X velocity changed");AssertClose(input.PreferredVelocityY,output[0].Y,1e-12,"R7 no-neighbor Y velocity changed");
        var host=new SimulationHost(OpenLayout(Array.Empty<ShelfDefinition>()),Array.Empty<ProductDefinition>(),Population(Profile("fallback-a",""),Profile("fallback-b","")),new SimulationConfig{DurationMinutes=1},new ThrowingAvoidance());
        var first=host.Agents[0];var second=host.Agents[1];first.X=1;first.Y=1;second.X=1;second.Y=2.5;PrepareRoute(first,new[]{first.Position(),new Position2D(4,1)});PrepareRoute(second,new[]{second.Position(),new Position2D(4,2.5)});host.Step(.2);
        Assert(host.Events.Count(item=>item.Type=="avoidance-fallback")==1&&first.X>1&&second.X>1,"Safe RVO fallback did not use preferred velocities");
        Console.WriteLine("PASS R7 adapter no-neighbor equivalence and safe failure fallback");
    }

    private sealed class ThrowingAvoidance : IRvoAvoidance
    {
        public IReadOnlyList<RvoVelocity> Solve(IReadOnlyList<RvoAgentInput> agents,RvoAvoidanceSettings settings,double deltaSeconds)=>throw new InvalidOperationException("synthetic adapter failure");
    }

    private static LayoutDefinition OpenLayout(ShelfDefinition[] shelves)=>new LayoutDefinition{Width=12,Height=4,Entrance=new Position2D(1,1.7),Checkout=new Position2D(1,2.7),Shelves=shelves,SpawnRateCurve=new[]{new SpawnRatePoint{Minute=0,Rate=600}}};
    private static PopulationDefinition Population(params NPCProfile[] profiles)=>new PopulationDefinition{PopulationId="test",NPCProfiles=profiles,Metadata=new PopulationMetadata{GeneratorName="test",GeneratorVersion="1"}};
    private static NPCProfile Profile(string id,string target)=>new NPCProfile{Id=id,TargetCategory=target,WalkingSpeed=1.2,Patience=0.5,Exploration=0.5,Sociability=0.5,Impulsiveness=0.5,CrowdTolerance=0.5,PriceSensitivity=0.5,CategoryPreferences=new[]{new CategoryPreference(target,1)},InitialNeed=0.8,InitialExplorationNeed=0,DwellSeconds=0.5};
    private static void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}private static void AssertClose(double expected,double actual,double tolerance,string message){Assert(Math.Abs(expected-actual)<=tolerance,message+" expected="+expected+" actual="+actual);}
}
