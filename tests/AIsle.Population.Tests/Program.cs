using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using AIsle.Contracts.Population;
using AIsle.Simulation.Population;
using AIsle.Simulation.Population.Genetic;

internal static class Program
{
    private static readonly string[] Scenarios = { "basic_001", "basic_002", "extreme_patience", "high_exploration", "mixed_population" };
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true };

    private static int Main()
    {
        try
        {
            var root = FindRepositoryRoot();
            foreach (var scenario in Scenarios) RunScenario(root, scenario);
            RunValidatorFailureChecks();
            RunStatisticsChecks();
            RunFitnessChecks();
            RunGeneratorAbstractionChecks();
            RunDependencyBoundaryChecks(root);
            Console.WriteLine("PASS: Population source-first verification completed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FAIL: " + exception);
            return 1;
        }
    }

    private static void RunScenario(string root, string scenario)
    {
        var directory = Path.Combine(root, "tests", "Golden", "Population", scenario);
        var config = JsonSerializer.Deserialize<PopulationConfig>(File.ReadAllText(Path.Combine(directory, "config.json")), JsonOptions);
        var expected = JsonSerializer.Deserialize<GoldenExpected>(File.ReadAllText(Path.Combine(directory, "expected.json")), JsonOptions);
        Assert(config != null && expected != null, scenario + ": fixtures did not deserialize.");
        var generator = new GeneticPopulationGenerator();
        var first = generator.Generate(config);
        var second = generator.Generate(config);
        Assert(first.NPCProfiles.Length == expected.Count && second.NPCProfiles.Length == expected.Count, scenario + ": count mismatch.");
        AssertValid(first, config, scenario + " run A");
        AssertValid(second, config, scenario + " run B");
        Assert(!string.Equals(first.PopulationId, second.PopulationId, StringComparison.Ordinal), scenario + ": stochastic runs reused an output identity.");

        var serialized = JsonSerializer.Serialize(first, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<PopulationDefinition>(serialized, JsonOptions);
        Assert(roundTrip != null && roundTrip.NPCProfiles.Length == first.NPCProfiles.Length, scenario + ": serialization round-trip failed.");
        Assert(roundTrip.NPCProfiles.All(profile => profile != null && double.IsFinite(profile.Patience)), scenario + ": round-trip contains invalid values.");
        Console.WriteLine("PASS invariant scenario " + scenario + " count=" + expected.Count);
    }

    private static void AssertValid(PopulationDefinition definition, PopulationConfig config, string label)
    {
        var validation = new PopulationValidator().Validate(definition, config);
        Assert(validation.Valid, label + " invalid: " + string.Join("; ", validation.Errors));
        Assert(definition.NPCProfiles.All(profile => profile != null && !string.IsNullOrWhiteSpace(profile.Id)), label + ": invalid profile.");
    }

    private static void RunValidatorFailureChecks()
    {
        var config = new PopulationConfig { Count = 2 };
        var population = new GeneticPopulationGenerator().Generate(config);
        population.NPCProfiles[1].Id = population.NPCProfiles[0].Id;
        population.NPCProfiles[0].Patience = double.NaN;
        var result = new PopulationValidator().Validate(population, config);
        Assert(!result.Valid && result.Errors.Length >= 2, "Validator accepted invalid population.");
        try { new GeneticPopulationGenerator().Generate(new PopulationConfig { Count = 0 }); }
        catch (ArgumentException) { Console.WriteLine("PASS validator rejection checks"); return; }
        throw new InvalidOperationException("Generator accepted invalid config.");
    }

    private static void RunStatisticsChecks()
    {
        var profiles = new[] { CreateProfile("a", 1.0), CreateProfile("b", 2.0), CreateProfile("c", 3.0) };
        var stats = PopulationStatistics.Calculate(profiles).WalkingSpeed;
        AssertClose(2.0, stats.Mean, 1e-12, "Mean incorrect.");
        AssertClose(2.0, stats.Median, 1e-12, "Median incorrect.");
        AssertClose(Math.Sqrt(2.0 / 3.0), stats.StandardDeviation, 1e-12, "Population std incorrect.");
        Console.WriteLine("PASS Math.NET statistics checks");
    }

    private static void RunFitnessChecks()
    {
        var config = new PopulationConfig();
        config.DistributionTargets.Patience = new DistributionTarget { Enabled = true, Mean = 0.8, StandardDeviation = 0.1, Weight = 1.0, Tolerance = 0.2 };
        var near = new AIsleNpcChromosome(config);
        var far = new AIsleNpcChromosome(config);
        near.SetValueAt(1, 0.8);
        far.SetValueAt(1, 0.0);
        var fitness = new AIslePopulationFitness();
        Assert(fitness.Evaluate(near) > fitness.Evaluate(far), "AIsle fitness does not prefer target-aligned traits.");
        Console.WriteLine("PASS AIsle domain fitness behavior");
    }

    private static void RunGeneratorAbstractionChecks()
    {
        var profiles = new[] { CreateProfile("manual-1", 1.2) };
        var config = new PopulationConfig { Count = 1 };
        var generated = new PopulationGenerator(new ManualPopulationGenerator(profiles)).Generate(config);
        profiles[0].Id = "changed";
        Assert(generated.NPCProfiles[0].Id == "manual-1", "Manual generator leaked source mutation.");
        var imported = new ImportedPopulationGenerator(generated).Generate(config);
        Assert(imported.NPCProfiles[0].Id == generated.NPCProfiles[0].Id, "Imported generator changed domain data.");
        Console.WriteLine("PASS generator abstraction checks");
    }

    private static void RunDependencyBoundaryChecks(string root)
    {
        var references = typeof(PopulationGenerator).Assembly.GetReferencedAssemblies().Select(item => item.Name).ToArray();
        Assert(references.Contains("GeneticSharp.Domain") && references.Contains("MathNet.Numerics"), "Vetted dependencies are not referenced.");
        Assert(!references.Any(name => name.StartsWith("Unity", StringComparison.Ordinal)), "Simulation references Unity.");
        var geneticDirectory = Path.Combine(root, "src", "AIsle.Simulation", "Population", "Genetic");
        foreach (var forbidden in new[] { "Genome.cs", "Selection.cs", "Crossover.cs", "Mutation.cs", "GeneDefinition.cs" })
            Assert(!File.Exists(Path.Combine(geneticDirectory, forbidden)), "Custom generic GA file remains: " + forbidden);
        Console.WriteLine("PASS source-first dependency boundary");
    }

    private static NPCProfile CreateProfile(string id, double speed) => new NPCProfile
    {
        Id = id, WalkingSpeed = speed, Patience = 0.5, Exploration = 0.5, Sociability = 0.5,
        Impulsiveness = 0.5, CrowdTolerance = 0.5, PriceSensitivity = 0.5,
        CategoryPreferences = new[] { new CategoryPreference("essentials", 1.0) }, ShoppingMission = ShoppingMission.Routine
    };

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null) { if (File.Exists(Path.Combine(current.FullName, "docs", "rule.md"))) return current.FullName; current = current.Parent; }
        throw new DirectoryNotFoundException("Repository root not found.");
    }

    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void AssertClose(double expected, double actual, double tolerance, string message) { Assert(Math.Abs(expected - actual) <= tolerance, message); }
    private sealed class GoldenExpected { public int Count { get; set; } }
}
