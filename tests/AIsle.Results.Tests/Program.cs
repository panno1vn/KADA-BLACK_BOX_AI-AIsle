using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using AIsle.Contracts.Simulation;
using AIsle.DesktopApp.Application;
using AIsle.DesktopApp.Infrastructure;
using AIsle.Simulation.Results;

internal static class Program
{
    private static int Main()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aisle-s5-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            TestFrozenSchema();
            TestHistoryStore(directory);
            TestReplayFromStoredResult(directory);
            TestKpiProjection();
            TestStoredResultComparison(directory);
            Console.WriteLine("PASS: S5/S6 result, history, replay, KPI and compare verification completed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FAIL: " + exception);
            return 1;
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static void TestFrozenSchema()
    {
        var source = Result("schema-1");
        var json = SimResultJsonSerializer.Serialize(source);
        using var document = JsonDocument.Parse(json);
        var names = document.RootElement.EnumerateObject().Select(item => item.Name).OrderBy(item => item).ToArray();
        Assert(names.SequenceEqual(new[] { "createdAt", "events", "id", "name", "purchases", "replay", "schemaVersion", "summary" }),
            "SimResult contains fields outside the frozen S5 schema.");
        var roundTrip = SimResultJsonSerializer.Deserialize(json);
        Assert(roundTrip.SchemaVersion == SimulationSchemas.SimResultV1 && roundTrip.Replay.Agents[0].Samples.Length == 3,
            "SimResult JSON round-trip failed.");

        var extra = json.Replace("\"name\"", "\"futureOnly\":true,\"name\"", StringComparison.Ordinal);
        var rejected = false;
        try { SimResultJsonSerializer.Deserialize(extra); }
        catch (JsonException) { rejected = true; }
        Assert(rejected, "Future-only SimResult field was accepted.");
    }

    private static void TestHistoryStore(string directory)
    {
        var store = new JsonHistoryStore(directory);
        var first = Result("run-a");
        var second = Result("run-b");
        second.CreatedAt = first.CreatedAt.AddMinutes(1);
        second.Purchases[0].Price = 24;
        second.Summary.Revenue = 24;
        store.Save(first);
        store.Save(second);
        var listed = store.List();
        Assert(listed.Items.Select(item => item.Id).SequenceEqual(new[] { "run-b", "run-a" }) && listed.Warnings.Length == 0,
            "History list ordering or metadata changed.");
        Assert(store.Read("run-a").Purchases.Length == first.Purchases.Length, "History read changed stored data.");

        var duplicateRejected = false;
        try { store.Save(first); }
        catch (DuplicateHistoryIdException) { duplicateRejected = true; }
        Assert(duplicateRejected, "Duplicate history ID was not rejected.");

        File.WriteAllText(Path.Combine(directory, "broken.sim-result.json"), "{broken");
        listed = store.List();
        Assert(listed.Items.Length == 2 && listed.Warnings.Length == 1 && listed.Warnings[0].Code == "corrupted_history",
            "Corrupted history was not isolated and reported.");
        var corruptRejected = false;
        try { store.Read("broken"); }
        catch (CorruptedHistoryException) { corruptRejected = true; }
        Assert(corruptRejected, "Direct read did not report corrupted history.");
    }

    private static void TestKpiProjection()
    {
        var empty = new SimResult { Id = "empty", CreatedAt = DateTimeOffset.UtcNow };
        var emptyKpis = KpiProjector.Project(empty);
        Assert(emptyKpis.Metrics.All(item => item.Value == 0.0), "Empty-run KPIs are not zero-safe.");

        var zeroPurchase = Result("zero-purchase");
        zeroPurchase.Purchases = Array.Empty<PurchaseRecord>();
        zeroPurchase.Summary.Converted = 0;
        var zeroKpis = KpiProjector.Project(zeroPurchase);
        Assert(Value(zeroKpis, "purchase_count") == 0 && Value(zeroKpis, "revenue") == 0 && Value(zeroKpis, "conversion_rate") == 0,
            "Zero-purchase KPI projection changed.");

        var normal = KpiProjector.Project(Result("normal"));
        Assert(Value(normal, "purchase_count") == 1 && Value(normal, "conversion_rate") == 100 && Value(normal, "revenue") == 12,
            "Normal-run purchase KPIs changed.");
        Assert(Value(normal, "shelf_visits") == 1 && Value(normal, "dwell_time_seconds") == 1
            && Value(normal, "path_length_meters") == 2 && Value(normal, "checkout_completions") == 1,
            "Normal-run trajectory/event KPIs changed.");

        var multiple = Result("multiple-shelves");
        multiple.Replay.Agents[0].Samples = new[]
        {
            new TrajectorySample { Time = 0, Status = "TRANSIT" },
            new TrajectorySample { Time = 1, X = 1, Status = "DWELL", ShelfId = "s1" },
            new TrajectorySample { Time = 2, X = 1, Y = 1, Status = "TRANSIT" },
            new TrajectorySample { Time = 3, X = 2, Y = 1, Status = "DWELL", ShelfId = "s2" },
            new TrajectorySample { Time = 4, X = 2, Y = 2, Status = "LEFT" }
        };
        Assert(Value(KpiProjector.Project(multiple), "shelf_visits") == 2, "Multiple-shelf visits were not counted by transitions.");
    }

    private static void TestStoredResultComparison(string directory)
    {
        var store = new JsonHistoryStore(directory);
        var comparison = ResultComparer.Compare(store.Read("run-a"), store.Read("run-b"));
        var revenue = comparison.Metrics.Single(item => item.Key == "revenue");
        Assert(revenue.RunA == 12 && revenue.RunB == 24 && revenue.AbsoluteDelta == 12 && revenue.RelativeDeltaPercent == 100,
            "Stored-result comparison delta changed.");

        var zero = new SimResult { Id = "zero", CreatedAt = DateTimeOffset.UtcNow };
        var relative = ResultComparer.Compare(zero, Result("nonzero")).Metrics.Single(item => item.Key == "revenue").RelativeDeltaPercent;
        Assert(relative == null, "Relative delta must be null when Run A is zero.");
    }

    private static double Value(KpiProjection projection, string key) => projection.Metrics.Single(item => item.Key == key).Value;

    private static void TestReplayFromStoredResult(string directory)
    {
        var restartedStore = new JsonHistoryStore(directory);
        var stored = restartedStore.Read("run-a");
        var first = ReplayProjector.Project(stored);
        var second = ReplayProjector.Project(new JsonHistoryStore(directory).Read("run-a"));
        var options = new JsonSerializerOptions { IncludeFields = true };
        Assert(JsonSerializer.Serialize(first, options) == JsonSerializer.Serialize(second, options),
            "Replay projection changed after application-store restart.");
        Assert(first.ResultId == "run-a" && first.Agents[0].Samples[1].Status == "DWELL",
            "Replay projection did not use stored trajectory.");
        first.Agents[0].Samples[0].X = 999;
        Assert(stored.Replay.Agents[0].Samples[0].X != 999, "Replay projection leaked mutable stored objects.");
    }

    private static SimResult Result(string id) => new SimResult
    {
        Id = id,
        CreatedAt = new DateTimeOffset(2026, 8, 18, 1, 0, 0, TimeSpan.Zero),
        Name = "S5 test",
        Summary = new SimulationSummary { DurationSeconds = 3, Spawned = 1, Converted = 1, Purchases = 1, Revenue = 12, Completed = true },
        Events = new[]
        {
            new SimulationEvent { Time = 1, NpcId = "npc-1", Type = "dwell" },
            new SimulationEvent { Time = 2, NpcId = "npc-1", Type = "checkout" }
        },
        Purchases = new[] { new PurchaseRecord { Time = 1.5, NpcId = "npc-1", ProductId = "p1", Price = 12, Type = "main" } },
        Replay = new ReplayData
        {
            SampleSeconds = 1,
            Agents = new[]
            {
                new AgentTrajectory
                {
                    Id = "npc-1", Spawn = 0,
                    Samples = new[]
                    {
                        new TrajectorySample { Time = 0, X = 0, Y = 0, Status = "TRANSIT" },
                        new TrajectorySample { Time = 1, X = 1, Y = 0, Status = "DWELL", ShelfId = "s1" },
                        new TrajectorySample { Time = 2, X = 1, Y = 1, Status = "LEFT" }
                    }
                }
            }
        }
    };

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
