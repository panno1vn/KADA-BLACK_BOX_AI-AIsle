using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AIsle.Contracts.Population;
using AIsle.Contracts.Project;
using AIsle.Contracts.Simulation;
using AIsle.DesktopApp.Infrastructure;
using AIsle.Simulation.Results;
using AIsle.Simulation.Runtime;

namespace AIsle.DesktopApp.Application
{
    public static class ReleaseSmokeRunner
    {
        public static int Run(string applicationBaseDirectory, string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            var reportPath = Path.Combine(outputDirectory, "qa-smoke-report.json");
            try
            {
                var uiRoot = LocalUiAssets.ResolveRoot(applicationBaseDirectory);
                LocalUiAssets.Verify(uiRoot, Path.Combine(uiRoot, "desktop-bridge.js"));
                var projectPath = Path.Combine(uiRoot, "default-project.json");
                var projects = new ProjectApplicationService(new JsonProjectRepository(), new LayoutValidator());
                var loaded = projects.LoadAsync(projectPath).GetAwaiter().GetResult();
                if (!loaded.Ok || loaded.Project == null) throw new InvalidOperationException("Packaged demo project did not load.");

                var categories = (loaded.Project.Catalog ?? Array.Empty<ProjectProduct>())
                    .Select(item => item.Category ?? string.Empty).Where(item => item.Length > 0).Distinct(StringComparer.Ordinal).ToArray();
                var populationConfig = new PopulationConfig
                {
                    Count = 8,
                    CategoryIds = categories,
                    GeneratorSettings = new GeneratorSettings { EvolutionPopulationSize = 12, Generations = 2, CrossoverProbability = 0.75, MutationProbability = 0.1 }
                };
                var generated = new PopulationApplicationService().Generate(populationConfig);
                if (!generated.Validation.Valid) throw new InvalidOperationException("Smoke population validation failed.");
                var population = new PopulationDefinition
                {
                    PopulationId = "release-smoke-population",
                    NPCProfiles = generated.Profiles,
                    Metadata = new PopulationMetadata { GeneratorName = "ReleaseSmoke", GeneratorVersion = "1" }
                };

                var layout = ProjectSimulationMapper.MapLayout(loaded.Project);
                layout.SpawnRateCurve = new[] { new SpawnRatePoint { Minute = 0, Rate = 600 }, new SpawnRatePoint { Minute = 0.5, Rate = 600 } };
                var catalog = ProjectSimulationMapper.MapCatalog(loaded.Project);
                var config = new SimulationConfig { DurationMinutes = 0.5, TickSeconds = 0.2, TrajectorySampleSeconds = 0.5 };
                var history = new JsonHistoryStore(Path.Combine(outputDirectory, "history"));
                var first = RunSimulation("Release smoke A", layout, catalog, population, config);
                var second = RunSimulation("Release smoke B", layout, catalog, population, config);
                history.Save(first);
                history.Save(second);

                var listed = history.List();
                var storedA = history.Read(first.Id);
                var replay = ReplayProjector.Project(storedA);
                var kpis = KpiProjector.Project(storedA);
                var comparison = ResultComparer.Compare(storedA, history.Read(second.Id));
                if (!first.Summary.Completed || !second.Summary.Completed || listed.Items.Length != 2 || listed.Warnings.Length != 0
                    || replay.ResultId != first.Id || kpis.Metrics.Length != 7 || comparison.Metrics.Length != 7)
                    throw new InvalidOperationException($"Inconsistent output: first.Completed={first.Summary.Completed}, second.Completed={second.Summary.Completed}, listedCount={listed.Items.Length}, warnings={string.Join(";", listed.Warnings)}, replayMatches={replay.ResultId == first.Id}, kpisCount={kpis.Metrics.Length}, compCount={comparison.Metrics.Length}");

                WriteReport(reportPath, new ReleaseSmokeReport
                {
                    Ok = true,
                    Version = Version(),
                    Flow = new[] { "launch", "open-project", "population", "run", "history", "replay", "compare" },
                    HistoryCount = listed.Items.Length,
                    ReplayAgents = replay.Agents.Length,
                    KpiCount = kpis.Metrics.Length
                });
                return 0;
            }
            catch (Exception exception)
            {
                WriteReport(reportPath, new ReleaseSmokeReport { Ok = false, Version = Version(), Error = exception.GetType().Name + ": " + exception.Message });
                return 1;
            }
        }

        private static SimResult RunSimulation(string name, LayoutDefinition layout, ProductDefinition[] catalog, PopulationDefinition population, SimulationConfig config)
        {
            var host = new SimulationHost(layout, catalog, population, config);
            host.RunToCompletion(10000);
            return host.BuildResult(name);
        }

        private static string Version() =>
            typeof(ReleaseSmokeRunner).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

        private static void WriteReport(string path, ReleaseSmokeReport report) =>
            File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }));
    }

    public sealed class ReleaseSmokeReport
    {
        public bool Ok { get; set; }
        public string Version { get; set; } = string.Empty;
        public string[] Flow { get; set; } = Array.Empty<string>();
        public int HistoryCount { get; set; }
        public int ReplayAgents { get; set; }
        public int KpiCount { get; set; }
        public string? Error { get; set; }
    }
}
