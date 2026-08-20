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
            RunPhantomRateChecks();
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

    private static void RunPhantomRateChecks()
    {
        var config = new PopulationConfig
        {
            Count = 1000,
            PhantomNeedRate = 0.12,
            CategoryIds = new[] { "drinks", "snacks" }
        };
        var population = new GeneticPopulationGenerator().Generate(config);
        var phantomCount = population.NPCProfiles.Count(p => string.Equals(p.TargetCategory, PopulationConfig.PhantomCategory, StringComparison.Ordinal));
        var rate = (double)phantomCount / population.NPCProfiles.Length;
        // Expected ~0.12, sample size 1000 -> tolerance ±0.05
        Assert(Math.Abs(rate - 0.12) <= 0.05, $"Phantom need rate {rate:F3} is outside expected tolerance (target 0.12 ± 0.05).");
        Assert(population.NPCProfiles.Where(p => string.Equals(p.TargetCategory, PopulationConfig.PhantomCategory, StringComparison.Ordinal)).All(p => p.CategoryPreferences.Length == 0), "Phantom profile should have empty category preferences.");
        Console.WriteLine($"PASS phantom rate statistical checks (rate={rate:F3}, count={phantomCount}/1000)");
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
        Assert(roundTrip.NPCProfiles.All(profile => profile != null && double.IsFinite(profile.InitialNeed) && !string.IsNullOrWhiteSpace(profile.TargetCategory)), scenario + ": round-trip contains invalid active values.");
        Console.WriteLine("PASS invariant scenario " + scenario + " count=" + expected.Count);
    }

    private static void AssertValid(PopulationDefinition definition, PopulationConfig config, string label)
    {
        var validation = new PopulationValidator().Validate(definition, config);
        Assert(validation.Valid, label + " invalid: " + string.Join("; ", validation.Errors));
        Assert(definition.NPCProfiles.All(profile => profile != null && !string.IsNullOrWhiteSpace(profile.Id)), label + ": invalid profile.");
        Assert(definition.NPCProfiles.All(profile => (profile.CategoryPreferences.Length > 0 || string.Equals(profile.TargetCategory, PopulationConfig.PhantomCategory, StringComparison.Ordinal))
            && profile.Impulsiveness >= config.ParameterRanges.Impulsiveness.Min
            && profile.Impulsiveness <= config.ParameterRanges.Impulsiveness.Max
            && profile.PriceSensitivity >= config.ParameterRanges.PriceSensitivity.Min
            && profile.PriceSensitivity <= config.ParameterRanges.PriceSensitivity.Max),
            label + ": S8 shopping fields were not generated within bounds.");
    }

    private static void RunValidatorFailureChecks()
    {
        var config = new PopulationConfig { Count = 2, CategoryIds = new[] { "drinks" } };
        var population = new GeneticPopulationGenerator().Generate(config);
        population.NPCProfiles[1].Id = population.NPCProfiles[0].Id;
        population.NPCProfiles[0].InitialNeed = double.NaN;
        var result = new PopulationValidator().Validate(population, config);
        Assert(!result.Valid && result.Errors.Length >= 2, "Validator accepted invalid population.");
        try { new GeneticPopulationGenerator().Generate(new PopulationConfig { Count = 0, CategoryIds = new[] { "drinks" } }); }
        catch (ArgumentException) { }

        try
        {
            new GeneticPopulationGenerator().Generate(new PopulationConfig { Count = 10, CategoryIds = Array.Empty<string>() });
            throw new InvalidOperationException("Generator accepted empty CategoryIds.");
        }
        catch (ArgumentException)
        {
            Console.WriteLine("PASS validator rejection checks");
            return;
        }
    }

    private static void RunStatisticsChecks()
    {
        var profiles = new[] { CreateProfile("a", 1.0), CreateProfile("b", 2.0), CreateProfile("c", 3.0) };
        var stats = PopulationStatistics.Calculate(profiles).WalkingSpeed;
        AssertClose(2.0, stats.Mean, 1e-12, "Mean incorrect.");
        AssertClose(2.0, stats.Median, 1e-12, "Median incorrect.");
        AssertClose(Math.Sqrt(2.0 / 3.0), stats.StandardDeviation, 1e-12, "Population std incorrect.");
        Assert(stats.Percentile10 <= stats.Percentile25 && stats.Percentile25 <= stats.Percentile50
            && stats.Percentile50 <= stats.Percentile75 && stats.Percentile75 <= stats.Percentile90,
            "Percentiles are not ordered.");

        var config = new PopulationConfig { Count = 120, CategoryIds = new[] { "drinks" } };
        config.DistributionTargets.InitialNeed = new DistributionTarget
        {
            Enabled = true, Mean = 0.65, StandardDeviation = 0.08, Weight = 1.0, Tolerance = 0.18
        };
        var generated = new GeneticPopulationGenerator().Generate(config);
        var generatedStats = PopulationStatistics.Calculate(generated).InitialNeed;
        Assert(Math.Abs(generatedStats.Mean - 0.65) <= 0.18, "Generated distribution mean is outside configured sanity tolerance.");
        Assert(generatedStats.Min >= config.ParameterRanges.InitialNeed.Min && generatedStats.Max <= config.ParameterRanges.InitialNeed.Max,
            "Generated distribution escaped configured bounds.");
        Console.WriteLine("PASS Math.NET statistics checks");
    }

    private static void RunFitnessChecks()
    {
        var config = new PopulationConfig { CategoryIds = new[] { "drinks" } };
        config.DistributionTargets.InitialNeed = new DistributionTarget { Enabled = true, Mean = 0.8, StandardDeviation = 0.1, Weight = 1.0, Tolerance = 0.2 };
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
        Id = id, WalkingSpeed = speed, InitialNeed = 0.5, NeedGrowthPerMinute = 0.01,
        InitialExplorationNeed = 0.4, ExplorationGrowthPerMinute = 0.01, AffectAttractor = 0.2,
        AffectStability = 0.6, AffectDispersion = 0.4, AffectRecovery = 0.15,
        DwellSeconds = 10, TargetCategory = "essentials", Impulsiveness = 0.5, PriceSensitivity = 0.5,
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
