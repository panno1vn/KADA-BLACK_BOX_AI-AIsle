using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using AIsle.Contracts.Population;
using AIsle.Contracts.Simulation;
using AIsle.Simulation.Runtime;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var report = new BenchmarkReport
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Machine = Environment.MachineName,
                Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                Scenarios = new[] { Run(200), Run(500), Run(1000) },
                AvoidanceScenarios = new[] { RunAvoidance(50), RunAvoidance(100), RunAvoidance(200) },
                ShelfQueueScenarios = new[] { RunShelfQueue(20), RunShelfQueue(50), RunShelfQueue(100) }
            };
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
            Console.WriteLine(json);
            if (args.Length > 0)
            {
                var path = Path.GetFullPath(args[0]);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, json);
            }
            if (report.Scenarios.Any(item => !item.Correct) || report.AvoidanceScenarios.Any(item => !item.Correct) || report.ShelfQueueScenarios.Any(item => !item.Correct)) return 1;
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static BenchmarkScenario Run(int count)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var allocatedBefore = GC.GetTotalAllocatedBytes(true);
        var process = Process.GetCurrentProcess();
        var stopwatch = Stopwatch.StartNew();
        var config = new SimulationConfig { DurationMinutes = 0.05, TickSeconds = 0.2, TrajectorySampleSeconds = 0.5 };
        var host = new SimulationHost(Layout(), Catalog(), Population(count), config);
        var ticks = 0;
        while (!host.Completed && ticks < 1000) { host.Step(config.TickSeconds); ticks++; }
        var result = host.BuildResult("benchmark-" + count);
        stopwatch.Stop();
        process.Refresh();
        var samples = result.Replay.Agents.Sum(item => item.Samples.Length);
        return new BenchmarkScenario
        {
            NpcCount = count,
            Ticks = ticks,
            RuntimeMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
            TickMilliseconds = ticks == 0 ? 0 : stopwatch.Elapsed.TotalMilliseconds / ticks,
            AllocatedMegabytes = (GC.GetTotalAllocatedBytes(true) - allocatedBefore) / 1024.0 / 1024.0,
            WorkingSetMegabytes = process.WorkingSet64 / 1024.0 / 1024.0,
            Events = result.Events.Length,
            ReplaySamples = samples,
            Correct = result.Summary.Completed && result.Summary.DurationSeconds == config.DurationMinutes * 60.0
                && result.Replay.Agents.Length == count && result.Purchases.Length == result.Summary.Purchases
        };
    }

    private static AvoidanceBenchmarkScenario RunAvoidance(int count)
    {
        GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
        var config=new SimulationConfig{DurationMinutes=.1,TickSeconds=.05,TrajectorySampleSeconds=1,RvoNeighborDistance=3,RvoMaxNeighbors=30,RvoTimeHorizon=4};
        var host=new SimulationHost(new LayoutDefinition{Width=20,Height=8,Entrance=new Position2D(.8,.8),Checkout=new Position2D(.8,7.2),SpawnRateCurve=new[]{new SpawnRatePoint{Minute=0,Rate=100000}}},Array.Empty<ProductDefinition>(),Population(count),config);
        var initialDistance=new double[count];
        for(var index=0;index<count;index++)
        {
            var column=index%20;var row=index/20;var agent=host.Agents[index];agent.X=.8+(column*.5);agent.Y=.8+(row*.6);agent.Spawn=0;agent.Status="TRANSIT";agent.CurrentShelf="benchmark";
            var target=new Position2D(column<10?18:.8,agent.Y);agent.Path=new System.Collections.Generic.List<Position2D>{agent.Position(),target};agent.PathIndex=1;agent.RouteTarget=target;agent.RouteStatus="TRANSIT";agent.Profile.DwellSeconds=1000;
            initialDistance[index]=Distance(agent.Position(),target);
        }
        var stopwatch=Stopwatch.StartNew();var ticks=0;long collisions=0;long overlaps=0;var geometrySafe=true;
        while(!host.Completed&&ticks<1000)
        {
            host.Step(config.TickSeconds);ticks++;
            for(var first=0;first<count;first++)
            {
                geometrySafe&=host.Grid.IsPointWalkable(host.Agents[first].Position());
                for(var second=first+1;second<count;second++){var distance=Distance(host.Agents[first].Position(),host.Agents[second].Position());if(distance<config.CollisionRadius*.5)collisions++;if(distance<config.CollisionRadius)overlaps++;}
            }
        }
        stopwatch.Stop();var progressed=0;
        for(var index=0;index<count;index++)if(Distance(host.Agents[index].Position(),host.Agents[index].RouteTarget)<initialDistance[index]-.2)progressed++;
        return new AvoidanceBenchmarkScenario{NpcCount=count,Ticks=ticks,RuntimeMilliseconds=stopwatch.Elapsed.TotalMilliseconds,TickMilliseconds=ticks==0?0:stopwatch.Elapsed.TotalMilliseconds/ticks,CollisionPairTicks=collisions,OverlapPairTicks=overlaps,ProgressedAgents=progressed,GeometrySafe=geometrySafe,Correct=host.Completed&&collisions==0&&progressed>=Math.Ceiling(count*.8)&&geometrySafe};
    }

    private static ShelfQueueBenchmarkScenario RunShelfQueue(int count)
    {
        GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
        var config=new SimulationConfig{DurationMinutes=1.5,TickSeconds=.1,TrajectorySampleSeconds=1,PathCellSize=.2,ObstacleMargin=.12,CollisionRadius=.32,MaxShelfVisits=1,TopKChoices=1,DecisionNoise=0,PurchaseNeedA=10,PurchaseBiasC=10,PurchaseValenceB=0};
        var shelf=new ShelfDefinition{Id="hotspot",Label="Hotspot",Category="beverage",X=10,Y=5.5,Width=2,Height=1,Valence=.4};
        var layout=new LayoutDefinition{Width=20,Height=12,Entrance=new Position2D(1,1),Checkout=new Position2D(2,10.5),Shelves=new[]{shelf},SpawnRateCurve=new[]{new SpawnRatePoint{Minute=0,Rate=100000}}};
        var host=new SimulationHost(layout,new[]{new ProductDefinition{Id="p",Name="Water",Category="beverage",ShelfId="hotspot",Price=10}},Population(count),config);
        for(var index=0;index<count;index++){var agent=host.Agents[index];agent.Spawn=0;agent.X=.8+((index%10)*.4);agent.Y=2+((index/10)*.4);agent.Profile.DwellSeconds=.3;agent.Profile.InitialNeed=1;}
        var tickSamples=new System.Collections.Generic.List<double>();var total=Stopwatch.StartNew();long severeOverlap=0;var geometrySafe=true;var ticks=0;
        while(!host.Completed&&ticks<2000)
        {
            var tick=Stopwatch.StartNew();host.Step(config.TickSeconds);tick.Stop();tickSamples.Add(tick.Elapsed.TotalMilliseconds);ticks++;
            for(var first=0;first<count;first++)
            {
                if(host.Agents[first].Finished)continue;
                geometrySafe&=host.Grid.IsPointWalkable(host.Agents[first].Position());
                for(var second=first+1;second<count;second++)if(!host.Agents[second].Finished&&host.Agents[first].CurrentShelf=="hotspot"&&host.Agents[second].CurrentShelf=="hotspot"&&Distance(host.Agents[first].Position(),host.Agents[second].Position())<config.CollisionRadius*.5)severeOverlap++;
            }
        }
        total.Stop();tickSamples.Sort();var p95=tickSamples.Count==0?0:tickSamples[Math.Min(tickSamples.Count-1,(int)Math.Floor(tickSamples.Count*.95))];var completedAgents=host.Agents.Count(agent=>agent.Finished);
        return new ShelfQueueBenchmarkScenario{NpcCount=count,Ticks=ticks,RuntimeMilliseconds=total.Elapsed.TotalMilliseconds,TickMilliseconds=ticks==0?0:total.Elapsed.TotalMilliseconds/ticks,P95TickMilliseconds=p95,SevereOverlapPairTicks=severeOverlap,CompletedAgents=completedAgents,MaxQueueLength=host.MaxShelfQueueLength,GeometrySafe=geometrySafe,Correct=host.Completed&&severeOverlap==0&&completedAgents>0&&host.MaxShelfQueueLength>0&&geometrySafe};
    }

    private static double Distance(Position2D first,Position2D second){var dx=first.X-second.X;var dy=first.Y-second.Y;return Math.Sqrt((dx*dx)+(dy*dy));}

    private static LayoutDefinition Layout() => new LayoutDefinition
    {
        Width = 20, Height = 10,
        Entrance = new Position2D(1, 5), Checkout = new Position2D(2, 5),
        Shelves = new[] { new ShelfDefinition { Id = "s1", Label = "Shelf", Category = "beverage", X = 10, Y = 4, Width = 2, Height = 1, Valence = 0.2 } },
        SpawnRateCurve = new[] { new SpawnRatePoint { Minute = 0, Rate = 60000 }, new SpawnRatePoint { Minute = 0.05, Rate = 60000 } }
    };

    private static ProductDefinition[] Catalog() => new[]
    {
        new ProductDefinition { Id = "p1", Name = "Water", Category = "beverage", ShelfId = "s1", Price = 10 }
    };

    private static PopulationDefinition Population(int count)
    {
        var profiles = new NPCProfile[count];
        for (var index = 0; index < count; index++)
            profiles[index] = new NPCProfile { Id = "npc-" + index, TargetCategory = "beverage", WalkingSpeed = 1.2, InitialNeed = 0.8, DwellSeconds = 1 };
        return new PopulationDefinition
        {
            PopulationId = "benchmark-" + count,
            NPCProfiles = profiles,
            Metadata = new PopulationMetadata { GeneratorName = "benchmark-fixture", GeneratorVersion = "1" }
        };
    }

    private sealed class BenchmarkReport
    {
        public DateTimeOffset TimestampUtc { get; set; }
        public string Machine { get; set; } = string.Empty;
        public string Framework { get; set; } = string.Empty;
        public BenchmarkScenario[] Scenarios { get; set; } = Array.Empty<BenchmarkScenario>();
        public AvoidanceBenchmarkScenario[] AvoidanceScenarios { get; set; } = Array.Empty<AvoidanceBenchmarkScenario>();
        public ShelfQueueBenchmarkScenario[] ShelfQueueScenarios { get; set; } = Array.Empty<ShelfQueueBenchmarkScenario>();
    }

    private sealed class BenchmarkScenario
    {
        public int NpcCount { get; set; }
        public int Ticks { get; set; }
        public double RuntimeMilliseconds { get; set; }
        public double TickMilliseconds { get; set; }
        public double AllocatedMegabytes { get; set; }
        public double WorkingSetMegabytes { get; set; }
        public int Events { get; set; }
        public int ReplaySamples { get; set; }
        public bool Correct { get; set; }
    }

    private sealed class AvoidanceBenchmarkScenario
    {
        public int NpcCount { get; set; }
        public int Ticks { get; set; }
        public double RuntimeMilliseconds { get; set; }
        public double TickMilliseconds { get; set; }
        public long CollisionPairTicks { get; set; }
        public long OverlapPairTicks { get; set; }
        public int ProgressedAgents { get; set; }
        public bool GeometrySafe { get; set; }
        public bool Correct { get; set; }
    }

    private sealed class ShelfQueueBenchmarkScenario
    {
        public int NpcCount { get; set; }
        public int Ticks { get; set; }
        public double RuntimeMilliseconds { get; set; }
        public double TickMilliseconds { get; set; }
        public double P95TickMilliseconds { get; set; }
        public long SevereOverlapPairTicks { get; set; }
        public int CompletedAgents { get; set; }
        public int MaxQueueLength { get; set; }
        public bool GeometrySafe { get; set; }
        public bool Correct { get; set; }
    }
}
