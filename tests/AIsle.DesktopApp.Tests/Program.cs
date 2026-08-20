using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AIsle.Contracts.Project;
using AIsle.Contracts.Population;
using AIsle.Contracts.Simulation;
using AIsle.DesktopApp.Application;
using AIsle.DesktopApp.Bridge;
using AIsle.DesktopApp.Infrastructure;

internal static class Program
{
    private static async Task<int> Main()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "aisle-s2-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);

        try
        {
            ContractSerializationRoundTrip();
            await LoadUseCaseScenarios(testDirectory);
            await SaveRoundTrip(testDirectory);
            LayoutValidationScenarios();
            await BridgeProjectRoundTrip(testDirectory);
            BridgePopulationGeneration();
            BridgeSimulationCommands();
            BridgeHistoryAndReplay(testDirectory);
            SqliteHistoryStoreScenarios(testDirectory);
            StartupErrorHandling(testDirectory);
            PixelNpcAssetPackaging();
            ReleaseSmokeFlow(testDirectory);
            BridgeEnvelopeScenarios();
            Console.WriteLine("PASS: Desktop S1-S7 bridge, persistence, QA and application verification completed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FAIL: " + exception);
            return 1;
        }
        finally
        {
            if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
        }
    }

    private static void ContractSerializationRoundTrip()
    {
        var json = ProjectJsonSerializer.Serialize(ValidProject());
        using var document = JsonDocument.Parse(json);
        var rootNames = document.RootElement.EnumerateObject().Select(item => item.Name).OrderBy(item => item).ToArray();
        var layoutNames = document.RootElement.GetProperty("layout").EnumerateObject().Select(item => item.Name).OrderBy(item => item).ToArray();

        Assert(rootNames.SequenceEqual(new[] { "catalog", "layout", "schemaVersion" }), "Project schema contains unexpected root fields.");
        Assert(layoutNames.SequenceEqual(new[] { "checkout", "entrance", "height", "shelves", "spawnRateCurve", "walls", "width" }), "Layout schema contains unexpected fields.");
        var roundTrip = ProjectJsonSerializer.Deserialize(json);
        Assert(roundTrip.SchemaVersion == ProjectSchema.Version, "Schema version did not round-trip.");
        Assert(roundTrip.Layout?.Walls?.Length == 1 && roundTrip.Layout.Shelves?.Length == 1, "Layout arrays did not round-trip.");
        Assert(roundTrip.Catalog?.Single().Shelf == "s1", "Catalog did not round-trip.");
    }

    private static async Task LoadUseCaseScenarios(string directory)
    {
        var repository = new JsonProjectRepository();
        var service = new ProjectApplicationService(repository, new LayoutValidator());
        var validPath = Path.Combine(directory, "valid.json");
        await File.WriteAllTextAsync(validPath, ProjectJsonSerializer.Serialize(ValidProject()));

        var valid = await service.LoadAsync(validPath);
        Assert(valid.Ok && valid.Project?.Layout?.Entrance != null, "Valid project did not load.");

        var missing = await service.LoadAsync(Path.Combine(directory, "missing.json"));
        Assert(!missing.Ok && missing.Error?.Code == "project_not_found", "Missing file error mapping changed.");

        var malformedPath = Path.Combine(directory, "malformed.json");
        await File.WriteAllTextAsync(malformedPath, "{ this is not json");
        var malformed = await service.LoadAsync(malformedPath);
        Assert(!malformed.Ok && malformed.Error?.Code == "malformed_json", "Malformed JSON error mapping changed.");

        var schemaPath = Path.Combine(directory, "invalid-schema.json");
        await File.WriteAllTextAsync(schemaPath, "{\"schemaVersion\":\"future.v9\",\"layout\":{},\"catalog\":[]}");
        var invalidSchema = await service.LoadAsync(schemaPath);
        Assert(!invalidSchema.Ok && invalidSchema.Error?.Code == "invalid_schema", "Invalid schema error mapping changed.");

        var extraFieldPath = Path.Combine(directory, "extra-field.json");
        await File.WriteAllTextAsync(extraFieldPath, ProjectJsonSerializer.Serialize(ValidProject()).Replace("\"schemaVersion\"", "\"futureOnly\":true,\"schemaVersion\"", StringComparison.Ordinal));
        var extraField = await service.LoadAsync(extraFieldPath);
        Assert(!extraField.Ok && extraField.Error?.Code == "invalid_schema", "Unmapped future field was not rejected.");
    }

    private static async Task SaveRoundTrip(string directory)
    {
        var path = Path.Combine(directory, "nested", "saved.json");
        var service = new ProjectApplicationService(new JsonProjectRepository(), new LayoutValidator());
        var saved = await service.SaveAsync(path, ValidProject());
        Assert(saved.Ok && File.Exists(path), "Project save did not create the target file.");
        Assert(saved.Project?.Layout?.Shelves?.Single().Id == "s1", "Save/reload round-trip changed the project.");
        Assert(!Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.tmp", SearchOption.TopDirectoryOnly).Any(), "Atomic-save temp file leaked.");

        var invalid = ValidProject();
        invalid.Layout!.Entrance = null;
        var invalidSavePath = Path.Combine(directory, "invalid-save.json");
        var rejected = await service.SaveAsync(invalidSavePath, invalid);
        Assert(!rejected.Ok && rejected.Error?.Code == "invalid_layout" && !File.Exists(invalidSavePath), "Invalid layout was persisted.");
    }

    private static void LayoutValidationScenarios()
    {
        var validator = new LayoutValidator();
        var missingMarkers = ValidProject();
        missingMarkers.Layout!.Entrance = null;
        missingMarkers.Layout.Checkout = null;
        var missingResult = validator.ValidateProject(missingMarkers);
        Assert(!missingResult.IsValid && missingResult.Errors.Any(item => item.Contains("entrance", StringComparison.OrdinalIgnoreCase)) && missingResult.Errors.Any(item => item.Contains("checkout", StringComparison.OrdinalIgnoreCase)), "Required marker validation failed.");

        var invalidGeometry = ValidProject();
        invalidGeometry.Layout!.Walls![0].X2 = invalidGeometry.Layout.Walls[0].X1;
        invalidGeometry.Layout.Walls[0].Y2 = invalidGeometry.Layout.Walls[0].Y1;
        invalidGeometry.Layout.Shelves![0].W = 0;
        Assert(!validator.ValidateProject(invalidGeometry).IsValid, "Invalid wall/shelf geometry was accepted.");

        var unreachable = ValidProject();
        unreachable.Layout = new ProjectLayout
        {
            Width = 6, Height = 4,
            Entrance = new ProjectPoint { X = 1, Y = 2 },
            Checkout = new ProjectPoint { X = 1.5, Y = 2 },
            Walls = new[] { new ProjectWall { Id = "barrier", X1 = 3, Y1 = 0, X2 = 3, Y2 = 4 } },
            Shelves = new[] { new ProjectShelf { Id = "isolated", Label = "Isolated", Category = "test", X = 4.4, Y = 1.4, W = 1, H = 1, Valence = 0 } },
            SpawnRateCurve = Array.Empty<ProjectSpawnRatePoint>()
        };
        var unreachableResult = validator.ValidateProject(unreachable);
        Assert(unreachableResult.IsValid, "Unreachable shelf should be a warning, not a blocking error.");
        Assert(unreachableResult.UnreachableShelfIds.SequenceEqual(new[] { "isolated" }) && unreachableResult.Warnings.Length == 1, "Shelf reachability warning changed.");
    }

    private static async Task BridgeProjectRoundTrip(string directory)
    {
        var path = Path.Combine(directory, "bridge.json");
        var service = new ProjectApplicationService(new JsonProjectRepository(), new LayoutValidator());
        var bridge = new BridgeMessageProcessor(service, path);
        var projectJson = ProjectJsonSerializer.Serialize(ValidProject());
        var saveRequest = "{\"requestId\":\"save-001\",\"type\":\"project.save\",\"payload\":{\"project\":" + projectJson + "}}";
        using var saveResponse = JsonDocument.Parse(await bridge.ProcessAsync(saveRequest));
        Assert(saveResponse.RootElement.GetProperty("ok").GetBoolean() && saveResponse.RootElement.GetProperty("requestId").GetString() == "save-001", "Bridge project.save failed.");

        using var loadResponse = JsonDocument.Parse(await bridge.ProcessAsync("{\"requestId\":\"load-001\",\"type\":\"project.load\",\"payload\":{}}"));
        Assert(loadResponse.RootElement.GetProperty("ok").GetBoolean(), "Bridge project.load failed.");
        Assert(loadResponse.RootElement.GetProperty("payload").GetProperty("project").GetProperty("schemaVersion").GetString() == ProjectSchema.Version, "Bridge load payload schema changed.");
    }

    private static void BridgeEnvelopeScenarios()
    {
        var processor = new BridgeMessageProcessor();
        using var ping = JsonDocument.Parse(processor.Process("{\"requestId\":\"ping-001\",\"type\":\"app.ping\",\"payload\":{}}"));
        Assert(ping.RootElement.GetProperty("requestId").GetString() == "ping-001" && ping.RootElement.GetProperty("ok").GetBoolean(), "app.ping envelope changed.");

        var invalidInputs = new[] { string.Empty, "not-json", "[]", "{}", "{\"requestId\":\"missing-type\",\"payload\":{}}", "{\"requestId\":\"missing-payload\",\"type\":\"app.ping\"}" };
        foreach (var input in invalidInputs)
        {
            using var response = JsonDocument.Parse(processor.Process(input));
            Assert(!response.RootElement.GetProperty("ok").GetBoolean(), "Invalid bridge message unexpectedly succeeded.");
        }
    }

    private static void BridgePopulationGeneration()
    {
        var request = JsonSerializer.Serialize(new
        {
            requestId = "population-001",
            type = "population.generate",
            payload = new { config = new PopulationConfig { Count = 12, CategoryIds = new[] { "drinks", "snacks" } } }
        }, new JsonSerializerOptions { IncludeFields = true });
        using var response = JsonDocument.Parse(new BridgeMessageProcessor().Process(request));
        var root = response.RootElement;
        Assert(root.GetProperty("ok").GetBoolean(), "population.generate bridge command failed.");
        var payload = root.GetProperty("payload");
        Assert(payload.GetProperty("profiles").GetArrayLength() == 12, "population.generate profile count changed.");
        Assert(payload.GetProperty("summary").GetProperty("count").GetInt32() == 12, "population.generate summary missing.");
        Assert(payload.GetProperty("validation").GetProperty("valid").GetBoolean(), "population.generate returned invalid data.");
    }

    private static void BridgeSimulationCommands()
    {
        using var simulations = new SimulationApplicationService(backgroundLoop: false);
        var bridge = new BridgeMessageProcessor(simulations: simulations);
        var input = new SimulationStartInput
        {
            Name = "bridge-test",
            Layout = new LayoutDefinition
            {
                Width = 6, Height = 4, Entrance = new Position2D(1, 1), Checkout = new Position2D(1, 2),
                SpawnRateCurve = new[] { new SpawnRatePoint { Minute = 0, Rate = 600 } }
            },
            Population = new PopulationDefinition
            {
                PopulationId = "bridge-population",
                NPCProfiles = new[] { new NPCProfile { Id = "npc-1", TargetCategory = "missing", WalkingSpeed = 1.2 } },
                Metadata = new PopulationMetadata { GeneratorName = "test", GeneratorVersion = "1" }
            },
            Config = new SimulationConfig { DurationMinutes = 1, TickSeconds = 0.2 }
        };
        var options = new JsonSerializerOptions { IncludeFields = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var startRequest = JsonSerializer.Serialize(new { requestId = "start", type = "simulation.start", payload = new { input } }, options);
        var startResponse=bridge.Process(startRequest);using var start = JsonDocument.Parse(startResponse);
        Assert(start.RootElement.GetProperty("ok").GetBoolean() && start.RootElement.GetProperty("payload").GetProperty("running").GetBoolean(), "simulation.start failed: "+startResponse);
        Assert(start.RootElement.GetProperty("payload").GetProperty("time").GetDouble() > 0, "simulation.start must advance the first live tick immediately.");
        Assert(start.RootElement.GetProperty("payload").GetProperty("counters").GetProperty("spawned").GetInt32() == 1, "simulation.start must admit the first NPC immediately.");

        using var pause = JsonDocument.Parse(bridge.Process("{\"requestId\":\"pause\",\"type\":\"simulation.pause\",\"payload\":{}}"));
        Assert(!pause.RootElement.GetProperty("payload").GetProperty("running").GetBoolean(), "simulation.pause failed.");
        using var step = JsonDocument.Parse(bridge.Process("{\"requestId\":\"step\",\"type\":\"simulation.step\",\"payload\":{}}"));
        Assert(step.RootElement.GetProperty("payload").GetProperty("time").GetDouble() > 0, "simulation.step did not advance core time.");
        using var reset = JsonDocument.Parse(bridge.Process("{\"requestId\":\"reset\",\"type\":\"simulation.reset\",\"payload\":{}}"));
        Assert(reset.RootElement.GetProperty("payload").GetProperty("time").GetDouble() == 0, "simulation.reset did not rebuild the session.");
        using var speed = JsonDocument.Parse(bridge.Process("{\"requestId\":\"speed\",\"type\":\"simulation.speed\",\"payload\":{\"multiplier\":3}}"));
        Assert(speed.RootElement.GetProperty("ok").GetBoolean(), "simulation.speed failed.");
        Assert(speed.RootElement.GetProperty("payload").GetProperty("speedMultiplier").GetDouble() == 3, "simulation.speed did not preserve the selected multiplier.");
        using var invalidSpeed = JsonDocument.Parse(bridge.Process("{\"requestId\":\"speed-invalid\",\"type\":\"simulation.speed\",\"payload\":{\"multiplier\":4}}"));
        Assert(!invalidSpeed.RootElement.GetProperty("ok").GetBoolean(), "simulation.speed accepted a non-preset multiplier.");
        using var snapshot = JsonDocument.Parse(bridge.Process("{\"requestId\":\"snapshot\",\"type\":\"simulation.snapshot\",\"payload\":{}}"));
        var runId = snapshot.RootElement.GetProperty("payload").GetProperty("runId").GetString();
        Assert(!string.IsNullOrWhiteSpace(runId), "simulation.snapshot run identity is missing.");
        using var result = JsonDocument.Parse(bridge.Process("{\"requestId\":\"result\",\"type\":\"simulation.result\",\"payload\":{\"name\":\"UI result\"}}"));
        Assert(result.RootElement.GetProperty("payload").GetProperty("id").GetString() == runId, "simulation.result identity must stay stable for the session.");
    }

    private static void BridgeHistoryAndReplay(string directory)
    {
        var store = new JsonHistoryStore(Path.Combine(directory, "history"));
        using var bridge = new BridgeMessageProcessor(history: store);
        var result = new SimResult
        {
            Id = "bridge-result", CreatedAt = DateTimeOffset.UtcNow, Name = "Bridge result",
            Summary = new SimulationSummary { DurationSeconds = 2, Spawned = 1, Completed = true },
            Replay = new ReplayData
            {
                SampleSeconds = 1,
                Agents = new[]
                {
                    new AgentTrajectory
                    {
                        Id = "npc-1",
                        Samples = new[] { new TrajectorySample { Time = 0, Status = "WAITING" }, new TrajectorySample { Time = 1, X = 1, Status = "LEFT" } }
                    }
                }
            }
        };
        var options = new JsonSerializerOptions { IncludeFields = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var saveRequest = JsonSerializer.Serialize(new { requestId = "history-save", type = "history.save", payload = new { result } }, options);
        using var saved = JsonDocument.Parse(bridge.Process(saveRequest));
        Assert(saved.RootElement.GetProperty("ok").GetBoolean(), "history.save bridge command failed.");
        var resultB = new SimResult
        {
            Id = "bridge-result-b", CreatedAt = result.CreatedAt.AddMinutes(1), Name = "Bridge result B",
            Summary = new SimulationSummary { DurationSeconds = 2, Spawned = 1, Converted = 1, Purchases = 1, Revenue = 20, Completed = true },
            Purchases = new[] { new PurchaseRecord { NpcId = "npc-1", ProductId = "p1", Price = 20, Type = "main" } },
            Replay = new ReplayData()
        };
        var saveRequestB = JsonSerializer.Serialize(new { requestId = "history-save-b", type = "history.save", payload = new { result = resultB } }, options);
        using var savedB = JsonDocument.Parse(bridge.Process(saveRequestB));
        Assert(savedB.RootElement.GetProperty("ok").GetBoolean(), "Second history.save bridge command failed.");
        using var listed = JsonDocument.Parse(bridge.Process("{\"requestId\":\"history-list\",\"type\":\"history.list\",\"payload\":{}}"));
        Assert(listed.RootElement.GetProperty("payload").GetProperty("items").GetArrayLength() == 2, "history.list bridge command failed.");
        using var read = JsonDocument.Parse(bridge.Process("{\"requestId\":\"history-read\",\"type\":\"history.read\",\"payload\":{\"id\":\"bridge-result\"}}"));
        Assert(read.RootElement.GetProperty("payload").GetProperty("id").GetString() == "bridge-result", "history.read bridge command failed.");
        using var replay = JsonDocument.Parse(bridge.Process("{\"requestId\":\"replay\",\"type\":\"replay.project\",\"payload\":{\"id\":\"bridge-result\"}}"));
        Assert(replay.RootElement.GetProperty("payload").GetProperty("agents")[0].GetProperty("samples").GetArrayLength() == 2, "replay.project bridge command failed.");
        using var kpis = JsonDocument.Parse(bridge.Process("{\"requestId\":\"kpi\",\"type\":\"kpi.project\",\"payload\":{\"id\":\"bridge-result\"}}"));
        Assert(kpis.RootElement.GetProperty("payload").GetProperty("metrics").GetArrayLength() == 7, "kpi.project bridge command failed.");
        using var comparison = JsonDocument.Parse(bridge.Process("{\"requestId\":\"compare\",\"type\":\"compare.results\",\"payload\":{\"runAId\":\"bridge-result\",\"runBId\":\"bridge-result-b\"}}"));
        Assert(comparison.RootElement.GetProperty("payload").GetProperty("runAId").GetString() == "bridge-result"
            && comparison.RootElement.GetProperty("payload").GetProperty("runBId").GetString() == "bridge-result-b", "compare.results bridge command failed.");

        using var liveSimulations = new SimulationApplicationService(backgroundLoop: false);
        using var liveBridge = new BridgeMessageProcessor(simulations: liveSimulations, history: store);
        var liveInput = new SimulationStartInput
        {
            Name = "live-save-test",
            Layout = new LayoutDefinition { Width = 6, Height = 4, Entrance = new Position2D(1, 1), Checkout = new Position2D(1, 2) },
            Population = new PopulationDefinition { NPCProfiles = new[] { new NPCProfile { Id = "npc-1", TargetCategory = "missing", WalkingSpeed = 1.2 } } },
            Config = new SimulationConfig { DurationMinutes = 1, TickSeconds = 0.2 }
        };
        liveBridge.Process(JsonSerializer.Serialize(new { requestId = "start", type = "simulation.start", payload = new { input = liveInput } }, options));
        liveBridge.Process("{\"requestId\":\"step\",\"type\":\"simulation.step\",\"payload\":{}}");
        var resJson = liveBridge.Process("{\"requestId\":\"res\",\"type\":\"simulation.result\",\"payload\":{\"name\":\"Live Run 1\"}}");
        using var resDoc = JsonDocument.Parse(resJson);
        var resPayload = resDoc.RootElement.GetProperty("payload");
        var savePayload = JsonSerializer.Serialize(new { requestId = "save-live", type = "history.save", payload = new { result = resPayload } }, options);
        var saveResp = liveBridge.Process(savePayload);
        using var saveDoc = JsonDocument.Parse(saveResp);
        Assert(saveDoc.RootElement.GetProperty("ok").GetBoolean(), "Saving simulation.result output to history.save failed: " + saveResp);

        File.WriteAllText(Path.Combine(directory, "history", "corrupt.sim-result.json"), "{broken");
        using var corrupt = JsonDocument.Parse(bridge.Process("{\"requestId\":\"corrupt\",\"type\":\"history.read\",\"payload\":{\"id\":\"corrupt\"}}"));
        Assert(!corrupt.RootElement.GetProperty("ok").GetBoolean()
            && corrupt.RootElement.GetProperty("error").GetProperty("code").GetString() == "corrupted_history", "Corrupted history bridge error mapping changed.");
    }

    private static void SqliteHistoryStoreScenarios(string baseDirectory)
    {
        var directory = Path.Combine(baseDirectory, "sqlite-history");

        static SimResult MakeResult(string id, DateTimeOffset createdAt, double revenue) => new SimResult
        {
            Id = id,
            CreatedAt = createdAt,
            Name = "Run " + id,
            Summary = new SimulationSummary
            {
                DurationSeconds = 60, Spawned = 10, Converted = 4, Revenue = revenue, Purchases = 4,
                MainBuyers = 3, ImpulseBuyers = 1, NotFound = 1, Unreachable = 0, StuckRecoveries = 0, Completed = true
            },
            Replay = new ReplayData()
        };

        using (var store = new SqliteHistoryStore(directory))
        {
            var a = MakeResult("sqlite-a", DateTimeOffset.UtcNow.AddMinutes(-1), 100);
            var b = MakeResult("sqlite-b", DateTimeOffset.UtcNow, 200);
            store.Save(a);
            store.Save(b);

            var list = store.List();
            Assert(list.Items.Length == 2 && list.Items[0].Id == "sqlite-b" && list.Items[1].Id == "sqlite-a", "SqliteHistoryStore.List must return newest-first.");
            Assert(list.Items[0].Summary.Revenue == 200, "SqliteHistoryStore.List summary fields did not round-trip.");

            var read = store.Read("sqlite-a");
            Assert(read.Summary.Revenue == 100 && read.Name == "Run sqlite-a", "SqliteHistoryStore.Read did not return the full stored result.");

            ExpectThrows<DuplicateHistoryIdException>(() => store.Save(MakeResult("sqlite-a", DateTimeOffset.UtcNow, 0)), "Duplicate history id was not rejected.");
            ExpectThrows<HistoryResultNotFoundException>(() => store.Read("missing-id"), "Reading a missing history id did not throw.");

            Assert(store.Delete("sqlite-a"), "Delete did not report success for an existing id.");
            Assert(store.List().Items.Length == 1, "Deleted item must disappear from the active list.");
            Assert(store.ListTrash().Items.Single().Id == "sqlite-a", "Deleted item must appear in the trash list.");
            Assert(!store.Delete("sqlite-a"), "Deleting an already-trashed id must return false.");

            Assert(store.Restore("sqlite-a"), "Restore did not report success for a trashed id.");
            Assert(store.List().Items.Length == 2, "Restored item must reappear in the active list.");
            Assert(store.ListTrash().Items.Length == 0, "Trash list must be empty after restore.");

            Assert(store.Delete("sqlite-a") && store.Delete("sqlite-b"), "Setup for Clear/RestoreAll failed.");
            Assert(store.Clear() == 0, "Clear must only count active (non-trashed) items.");
            Assert(store.RestoreAll() == 2, "RestoreAll did not restore every trashed item.");
            Assert(store.Clear() == 2, "Clear did not trash every active item.");
        }

        using (var reopened = new SqliteHistoryStore(directory))
        {
            Assert(reopened.ListTrash().Items.Length == 2, "History must persist across SqliteHistoryStore instances (app restart).");
        }

        var migrationDirectory = Path.Combine(baseDirectory, "sqlite-migration");
        var legacy = new JsonHistoryStore(migrationDirectory);
        legacy.Save(MakeResult("legacy-active", DateTimeOffset.UtcNow.AddMinutes(-2), 50));
        legacy.Save(MakeResult("legacy-trashed", DateTimeOffset.UtcNow.AddMinutes(-3), 75));
        legacy.Delete("legacy-trashed");

        using var migrated = new SqliteHistoryStore(migrationDirectory);
        Assert(migrated.List().Items.Any(item => item.Id == "legacy-active"), "Migration did not import active legacy JSON history.");
        Assert(migrated.ListTrash().Items.Any(item => item.Id == "legacy-trashed"), "Migration did not import trashed legacy JSON history.");
        Assert(File.Exists(Path.Combine(migrationDirectory, "legacy-active.sim-result.json")), "Migration must not delete the original legacy JSON file.");

        // Repo-committed seed data (e.g. UI/history-seed) must be imported automatically so a fresh
        // clone/pull shows the same demo history without the user having to run anything first.
        var seedSourceDirectory = Path.Combine(baseDirectory, "seed-source");
        Directory.CreateDirectory(seedSourceDirectory);
        File.WriteAllText(Path.Combine(seedSourceDirectory, "seeded-run.sim-result.json"), SimResultJsonSerializer.Serialize(MakeResult("seeded-run", DateTimeOffset.UtcNow.AddDays(-1), 999)));
        var seededDbDirectory = Path.Combine(baseDirectory, "sqlite-seeded");
        using var seeded = new SqliteHistoryStore(seededDbDirectory, seedDirectory: seedSourceDirectory);
        Assert(seeded.List().Items.Any(item => item.Id == "seeded-run" && item.Summary.Revenue == 999), "Seed directory was not imported on first construction.");
    }

    private static void ExpectThrows<TException>(Action action, string message) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException(message);
    }

    private static void StartupErrorHandling(string directory)
    {
        var missingUi = Path.Combine(directory, "missing-ui");
        var failed = false;
        try { LocalUiAssets.Verify(missingUi, Path.Combine(missingUi, "desktop-bridge.js")); }
        catch (FileNotFoundException exception)
        {
            var message = DesktopStartupErrors.Message(exception);
            failed = message.Contains("WebView2 Runtime", StringComparison.Ordinal) && message.Contains("Required local UI asset", StringComparison.Ordinal);
        }
        Assert(failed, "WebView2/local-asset startup failure did not produce an actionable message.");
    }

    private static void PixelNpcAssetPackaging()
    {
        var uiRoot = LocalUiAssets.ResolveRoot(AppContext.BaseDirectory);
        var renderer = Path.Combine(uiRoot, "npc-renderer.mjs");
        var spriteRoot = Path.Combine(uiRoot, "assets", "npc");
        Assert(File.Exists(renderer), "Pixel NPC renderer was not packaged.");
        var expected = Enumerable.Range(1, 4)
            .Select(index => Path.Combine(spriteRoot, $"npc_{index}.png"))
            .ToArray();
        Assert(expected.All(File.Exists), "One or more pixel NPC sheets were not packaged.");
        foreach (var path in expected)
        {
            var bytes = File.ReadAllBytes(path);
            Assert(bytes.Length > 29 && bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71, $"NPC asset is not a PNG: {path}");
            var width = ReadBigEndianInt32(bytes, 16);
            var height = ReadBigEndianInt32(bytes, 20);
            var colorType = bytes[25];
            Assert(width == 128 && height == 384 && width % 4 == 0 && height % 8 == 0, $"NPC sheet does not satisfy the normalized 8x4 contract: {path}");
            Assert(colorType is 4 or 6, $"NPC sheet does not contain an alpha channel: {path}");
        }

        var app = File.ReadAllText(Path.Combine(uiRoot, "app.js"));
        Assert(app.Contains("npcRenderer.draw", StringComparison.Ordinal) && !app.Contains("ctx.arc(agent.x", StringComparison.Ordinal), "Desktop app did not replace the legacy NPC dot renderer.");
    }

    private static int ReadBigEndianInt32(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

    private static void ReleaseSmokeFlow(string directory)
    {
        var output = Path.Combine(directory, "release-smoke");
        var exitCode = ReleaseSmokeRunner.Run(AppContext.BaseDirectory, output);
        Assert(exitCode == 0, "Release smoke runner failed: " + File.ReadAllText(Path.Combine(output, "qa-smoke-report.json")));
        using var report = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "qa-smoke-report.json")));
        Assert(report.RootElement.GetProperty("ok").GetBoolean()
            && report.RootElement.GetProperty("flow").GetArrayLength() == 7, "Release smoke flow report changed.");
    }

    private static ProjectDocument ValidProject() => new ProjectDocument
    {
        SchemaVersion = ProjectSchema.Version,
        Layout = new ProjectLayout
        {
            Width = 12,
            Height = 8,
            Walls = new[] { new ProjectWall { Id = "w1", X1 = 0.2, Y1 = 0.2, X2 = 11.8, Y2 = 0.2 } },
            Shelves = new[] { new ProjectShelf { Id = "s1", Label = "Drink", Category = "beverage", X = 2, Y = 2, W = 2.5, H = 0.7, Valence = 0.4 } },
            Entrance = new ProjectPoint { X = 6, Y = 7.5 },
            Checkout = new ProjectPoint { X = 9.5, Y = 6.8 },
            SpawnRateCurve = new[] { new ProjectSpawnRatePoint { Minute = 0, Rate = 3 } }
        },
        Catalog = new[] { new ProjectProduct { Id = "p1", Name = "Water", Category = "beverage", Shelf = "s1", Price = 10000 } }
    };

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
