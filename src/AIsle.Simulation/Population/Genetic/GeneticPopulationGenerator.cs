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
        public const string Version = "population-geneticsharp-v3";

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
            var selector = Math.Max(0.0, Math.Min(0.999999999999, chromosome.ValueAt(AIsleNpcChromosome.TraitCount)));
            var categoryIndex = categories.Length > 0 ? Math.Min(categories.Length - 1, (int)(selector * categories.Length)) : 0;
            var normalCategory = categories.Length > 0 ? categories[categoryIndex] : PopulationConfig.PhantomCategory;

            var missionSelector = Math.Max(0.0, Math.Min(0.999999999999, chromosome.ValueAt(AIsleNpcChromosome.TraitCount + 1)));
            var phantomRoll = Math.Max(0.0, Math.Min(1.0, chromosome.ValueAt(AIsleNpcChromosome.TraitCount + 2)));
            var isPhantom = config.PhantomNeedRate > 0 && phantomRoll < config.PhantomNeedRate;
            var targetCategory = isPhantom ? PopulationConfig.PhantomCategory : normalCategory;

            var preferences = isPhantom
                ? Array.Empty<CategoryPreference>()
                : new[] { new CategoryPreference(normalCategory, 1.0) };

            return new NPCProfile
            {
                Id = "npc-" + Guid.NewGuid().ToString("N"),
                WalkingSpeed = chromosome.ValueAt(0),
                InitialNeed = chromosome.ValueAt(1),
                NeedGrowthPerMinute = chromosome.ValueAt(2),
                InitialExplorationNeed = chromosome.ValueAt(3),
                ExplorationGrowthPerMinute = chromosome.ValueAt(4),
                AffectAttractor = chromosome.ValueAt(5),
                AffectStability = chromosome.ValueAt(6),
                AffectDispersion = chromosome.ValueAt(7),
                AffectRecovery = chromosome.ValueAt(8),
                DwellSeconds = chromosome.ValueAt(9),
                Impulsiveness = chromosome.ValueAt(10),
                PriceSensitivity = chromosome.ValueAt(11),
                TargetCategory = targetCategory,
                CategoryPreferences = preferences,
                ShoppingMission = SelectMission(config.ShoppingMissionWeights, missionSelector)
            };
        }

        private static ShoppingMission SelectMission(ShoppingMissionWeight[] weights, double selector)
        {
            var source = weights ?? Array.Empty<ShoppingMissionWeight>();
            var total = 0.0;
            for (var index = 0; index < source.Length; index++) total += Math.Max(0.0, source[index].Weight);
            var roll = selector * total;
            for (var index = 0; index < source.Length; index++)
            {
                roll -= Math.Max(0.0, source[index].Weight);
                if (roll <= 0.0) return source[index].Mission;
            }

            return source[source.Length - 1].Mission;
        }
    }
}
