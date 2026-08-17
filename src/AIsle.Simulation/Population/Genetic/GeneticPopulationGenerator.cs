using System;
using System.Collections.Generic;
using AIsle.Contracts.Population;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;

namespace AIsle.Simulation.Population.Genetic
{
    public sealed class GeneticPopulationGenerator : IPopulationGenerator
    {
        public const string Version = "population-geneticsharp-v2";

        public PopulationDefinition Generate(PopulationConfig config)
        {
            PopulationValidator.ThrowIfConfigInvalid(config);
            var settings = config.GeneratorSettings;
            var poolSize = Math.Max(config.Count, settings.EvolutionPopulationSize);
            var population = new GeneticSharp.Domain.Populations.Population(poolSize, poolSize, new AIsleNpcChromosome(config));
            var algorithm = new GeneticAlgorithm(
                population,
                new AIslePopulationFitness(),
                new EliteSelection(),
                new UniformCrossover(),
                new UniformMutation(true))
            {
                CrossoverProbability = (float)settings.CrossoverProbability,
                MutationProbability = (float)settings.MutationProbability,
                Termination = new GenerationNumberTermination(settings.Generations)
            };
            algorithm.Start();

            var chromosomes = new List<IChromosome>(population.CurrentGeneration.Chromosomes);
            chromosomes.Sort((left, right) => Nullable.Compare(right.Fitness, left.Fitness));
            var profiles = new NPCProfile[config.Count];
            for (var index = 0; index < profiles.Length; index++)
            {
                profiles[index] = ToProfile((AIsleNpcChromosome)chromosomes[index], config);
            }

            return new PopulationDefinition
            {
                PopulationId = "population-" + Guid.NewGuid().ToString("N"),
                NPCProfiles = profiles,
                Metadata = new PopulationMetadata
                {
                    GeneratorName = nameof(GeneticPopulationGenerator),
                    GeneratorVersion = Version
                }
            };
        }

        private static NPCProfile ToProfile(AIsleNpcChromosome chromosome, PopulationConfig config)
        {
            var categories = config.CategoryIds ?? Array.Empty<string>();
            var preferences = new CategoryPreference[categories.Length];
            var total = 0.0;
            for (var index = 0; index < categories.Length; index++)
            {
                total += Math.Max(0.0, chromosome.ValueAt(AIsleNpcChromosome.TraitCount + index));
            }

            for (var index = 0; index < categories.Length; index++)
            {
                var raw = Math.Max(0.0, chromosome.ValueAt(AIsleNpcChromosome.TraitCount + index));
                preferences[index] = new CategoryPreference(categories[index], total <= 1e-12 ? 1.0 / categories.Length : raw / total);
            }

            return new NPCProfile
            {
                Id = "npc-" + Guid.NewGuid().ToString("N"),
                WalkingSpeed = chromosome.ValueAt(0),
                Patience = chromosome.ValueAt(1),
                Exploration = chromosome.ValueAt(2),
                Sociability = chromosome.ValueAt(3),
                Impulsiveness = chromosome.ValueAt(4),
                CrowdTolerance = chromosome.ValueAt(5),
                PriceSensitivity = chromosome.ValueAt(6),
                CategoryPreferences = preferences,
                ShoppingMission = SelectMission(chromosome.ValueAt(AIsleNpcChromosome.TraitCount + categories.Length), config.ShoppingMissionWeights)
            };
        }

        private static ShoppingMission SelectMission(double value, ShoppingMissionWeight[] missionWeights)
        {
            var weights = missionWeights ?? Array.Empty<ShoppingMissionWeight>();
            var total = 0.0;
            for (var index = 0; index < weights.Length; index++) total += Math.Max(0.0, weights[index].Weight);
            if (weights.Length == 0 || total <= 0.0) return ShoppingMission.Routine;
            var cursor = Math.Max(0.0, Math.Min(0.999999999999, value)) * total;
            var cumulative = 0.0;
            for (var index = 0; index < weights.Length; index++)
            {
                cumulative += Math.Max(0.0, weights[index].Weight);
                if (cursor < cumulative) return weights[index].Mission;
            }
            return weights[weights.Length - 1].Mission;
        }
    }
}
