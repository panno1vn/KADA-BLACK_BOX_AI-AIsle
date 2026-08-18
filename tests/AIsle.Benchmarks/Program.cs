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
                Scenarios = new[] { Run(200), Run(500), Run(1000) }
            };
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
            Console.WriteLine(json);
            if (args.Length > 0)
            {
                var path = Path.GetFullPath(args[0]);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, json);
            }
            if (report.Scenarios.Any(item => !item.Correct)) return 1;
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
}
