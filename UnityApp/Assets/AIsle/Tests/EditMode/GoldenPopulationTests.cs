using System;
using System.IO;
using System.Linq;
using AIsle.Contracts.Population;
using AIsle.Simulation.Population;
using AIsle.Simulation.Population.Genetic;
using NUnit.Framework;
using UnityEngine;

namespace AIsle.Tests.Population
{
    public sealed class GoldenPopulationTests
    {
        private static readonly string[] Scenarios = { "basic_001", "basic_002", "extreme_patience", "high_exploration", "mixed_population" };

        [TestCaseSource(nameof(Scenarios))]
        public void RepeatedRunsPreserveHardInvariants(string scenario)
        {
            var directory = Path.Combine(RepositoryRoot(), "tests", "Golden", "Population", scenario);
            var config = JsonUtility.FromJson<PopulationConfig>(File.ReadAllText(Path.Combine(directory, "config.json")));
            var expected = JsonUtility.FromJson<GoldenExpected>(File.ReadAllText(Path.Combine(directory, "expected.json")));
            var generator = new GeneticPopulationGenerator();
            var first = generator.Generate(config);
            var second = generator.Generate(config);
            Assert.That(first.NPCProfiles.Length, Is.EqualTo(expected.Count));
            Assert.That(second.NPCProfiles.Length, Is.EqualTo(expected.Count));
            Assert.That(new PopulationValidator().Validate(first, config).Valid, Is.True);
            Assert.That(new PopulationValidator().Validate(second, config).Valid, Is.True);
            Assert.That(second.PopulationId, Is.Not.EqualTo(first.PopulationId));

            var roundTrip = JsonUtility.FromJson<PopulationDefinition>(JsonUtility.ToJson(first));
            Assert.That(roundTrip.NPCProfiles.Length, Is.EqualTo(first.NPCProfiles.Length));
            Assert.That(roundTrip.NPCProfiles.All(profile => profile != null && !double.IsNaN(profile.Patience) && !double.IsInfinity(profile.Patience)), Is.True);
        }

        [Test]
        public void ContractsAndSimulationRemainUnityIndependentAndUseVettedLibraries()
        {
            var references = typeof(PopulationValidator).Assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
            Assert.That(references.Any(name => name == "GeneticSharp.Domain"), Is.True);
            Assert.That(references.Any(name => name == "MathNet.Numerics"), Is.True);
            Assert.That(references.Any(name => name.StartsWith("Unity", StringComparison.Ordinal)), Is.False);
        }

        [Serializable]
        private sealed class GoldenExpected { public int Count; }
        private static string RepositoryRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
    }
}
