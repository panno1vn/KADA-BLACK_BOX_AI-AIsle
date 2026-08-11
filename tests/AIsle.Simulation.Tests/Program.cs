using System;
using System.Linq;
using System.Text.Json;
using AIsle.Contracts.Population;
using AIsle.Contracts.Simulation;
using AIsle.Simulation.Runtime;

internal static class Program
{
    private static int Main()
    {
        try
        {
            TestPoissonSpawn(); TestNeedAndAffect(); TestPathRules(); TestUtility(); TestUnreachableAndPhantom(); TestFullJourneyAndResult();
            Console.WriteLine("PASS: C# simulation baseline verification completed."); return 0;
        }
        catch(Exception exception) { Console.Error.WriteLine("FAIL: " + exception); return 1; }
    }

    private static void TestPoissonSpawn()
    {
        var gaps = new System.Collections.Generic.List<double>(); var curve = new[] { new SpawnRatePoint { Minute=0,Rate=12 }, new SpawnRatePoint { Minute=10,Rate=12 } };
        for(var run=1;run<=80;run++){var arrivals=PoissonSpawnSampler.Sample(curve,600,int.MaxValue);var previous=0.0;for(var index=0;index<arrivals.Length;index++){gaps.Add(arrivals[index]-previous);previous=arrivals[index];}}
        var mean=gaps.Average();Assert(Math.Abs(mean-5.0)<0.3,"Poisson mean interval outside tolerance: "+mean);Console.WriteLine("PASS RUN2-02 Poisson spawn mean="+mean.ToString("F3"));
    }

    private static void TestNeedAndAffect()
    {
        var profile=Profile("need","drink");profile.InitialNeed=0.2;profile.NeedGrowthPerMinute=0.03;profile.InitialExplorationNeed=0.3;profile.ExplorationGrowthPerMinute=0.02;profile.AffectAttractor=0.1;profile.AffectDispersion=0.5;profile.AffectStability=0.2;profile.AffectRecovery=0.25;
        var agent=new NPCRuntimeState(profile,new Position2D(),0,new Random(1));var config=new SimulationConfig();NeedAffectSystem.Update(agent,60,config);AssertClose(0.23,agent.Need,1e-12,"Need growth changed");AssertClose(0.32,agent.Explore,1e-12,"Explore growth changed");NeedAffectSystem.ApplyShelfExperience(agent,0.9);AssertClose(0.42,agent.Valence,1e-12,"Affect update changed");NeedAffectSystem.Recover(agent);AssertClose(0.34,agent.Valence,1e-12,"Affect recovery changed");Console.WriteLine("PASS RUN2-03/04 Need and Affect");
    }

    private static void TestPathRules()
    {
        var config=new SimulationConfig{PathCellSize=0.2,ObstacleMargin=0.2};var sealedLayout=new LayoutDefinition{Width=6,Height=4,Entrance=new Position2D(1,2),Checkout=new Position2D(1.5,2),Walls=new[]{new WallDefinition{Id="barrier",X1=3,Y1=0,X2=3,Y2=4}}};
        var sealedGrid=new PathGrid(sealedLayout,config);Assert(sealedGrid.FindPath(new Position2D(1,2),new Position2D(5,2))==null,"Sealed wall was crossed");sealedLayout.Walls[0].Y2=2.8;var gapGrid=new PathGrid(sealedLayout,config);var path=gapGrid.FindPath(new Position2D(1,1),new Position2D(5,1));Assert(path!=null&&path.Count>2,"A* did not route through gap");for(var i=1;i<path.Count;i++)Assert(gapGrid.LineIsWalkable(path[i-1],path[i]),"Smoothed path is blocked");Console.WriteLine("PASS RUN2-07 A* hard invariants");
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
        Assert(host.Events.Any(item=>item.Type=="decision")&&host.Events.Any(item=>item.Type=="purchase")&&host.Events.Any(item=>item.Type=="checkout")&&host.Events.Any(item=>item.Type=="left"),"Full event journey incomplete");Assert(host.Purchases.Count>=1,"Purchase missing");Assert(host.Agents[0].Trajectory.Count>2,"Trajectory missing");var result=host.BuildResult("baseline");Assert(result.SchemaVersion=="aisle.sim-result.v1"&&result.Summary.Completed&&result.Replay.Columns.Length==5,"SimResult contract invalid");var json=JsonSerializer.Serialize(result,new JsonSerializerOptions{IncludeFields=true});var roundTrip=JsonSerializer.Deserialize<SimResult>(json,new JsonSerializerOptions{IncludeFields=true});Assert(roundTrip!=null&&roundTrip.Purchases.Length==result.Purchases.Length&&roundTrip.Replay.Agents[0].Samples.Length>2,"SimResult serialization failed");Console.WriteLine("PASS RUN2-08..15 full journey, trace, trajectory and SimResult");
    }

    private static LayoutDefinition OpenLayout(ShelfDefinition[] shelves)=>new LayoutDefinition{Width=12,Height=4,Entrance=new Position2D(1,1.7),Checkout=new Position2D(1,2.7),Shelves=shelves,SpawnRateCurve=new[]{new SpawnRatePoint{Minute=0,Rate=600}}};
    private static PopulationDefinition Population(params NPCProfile[] profiles)=>new PopulationDefinition{PopulationId="test",NPCProfiles=profiles,Metadata=new PopulationMetadata{GeneratorName="test",GeneratorVersion="1"}};
    private static NPCProfile Profile(string id,string target)=>new NPCProfile{Id=id,TargetCategory=target,WalkingSpeed=1.2,Patience=0.5,Exploration=0.5,Sociability=0.5,Impulsiveness=0.5,CrowdTolerance=0.5,PriceSensitivity=0.5,CategoryPreferences=new[]{new CategoryPreference(target,1)},InitialNeed=0.8,InitialExplorationNeed=0,DwellSeconds=0.5};
    private static void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}private static void AssertClose(double expected,double actual,double tolerance,string message){Assert(Math.Abs(expected-actual)<=tolerance,message+" expected="+expected+" actual="+actual);}
}
