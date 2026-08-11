using System;
using System.Collections.Generic;
using AIsle.Contracts.Population;

namespace AIsle.Simulation.Population
{
    public sealed class PopulationValidator
    {
        public ValidationResult Validate(PopulationDefinition definition, PopulationConfig config)
        {
            var warnings = new List<string>();
            var errors = new List<string>();
            if (definition == null)
            {
                errors.Add("Population definition is null.");
                return CreateResult(warnings, errors);
            }

            if (config == null)
            {
                errors.Add("Population config is null.");
                return CreateResult(warnings, errors);
            }

            var profiles = definition.NPCProfiles ?? Array.Empty<NPCProfile>();
            if (profiles.Length != config.Count)
            {
                errors.Add("NPC count does not match config.Count.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var categories = new HashSet<string>(config.CategoryIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            for (var index = 0; index < profiles.Length; index++)
            {
                var profile = profiles[index];
                if (profile == null)
                {
                    errors.Add("NPC profile at index " + index + " is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(profile.Id))
                {
                    errors.Add("NPC profile at index " + index + " has an empty ID.");
                }
                else if (!ids.Add(profile.Id))
                {
                    errors.Add("Duplicate NPC ID: " + profile.Id + ".");
                }

                ValidateValue(errors, profile.Id, "WalkingSpeed", profile.WalkingSpeed, config.ParameterRanges.WalkingSpeed);
                ValidateValue(errors, profile.Id, "Patience", profile.Patience, config.ParameterRanges.Patience);
                ValidateValue(errors, profile.Id, "Exploration", profile.Exploration, config.ParameterRanges.Exploration);
                ValidateValue(errors, profile.Id, "Sociability", profile.Sociability, config.ParameterRanges.Sociability);
                ValidateValue(errors, profile.Id, "Impulsiveness", profile.Impulsiveness, config.ParameterRanges.Impulsiveness);
                ValidateValue(errors, profile.Id, "CrowdTolerance", profile.CrowdTolerance, config.ParameterRanges.CrowdTolerance);
                ValidateValue(errors, profile.Id, "PriceSensitivity", profile.PriceSensitivity, config.ParameterRanges.PriceSensitivity);
                ValidatePreferences(errors, warnings, profile, categories);
            }

            if (errors.Count == 0)
            {
                ValidateDistributions(errors, warnings, profiles, config.DistributionTargets);
            }

            if (definition.Metadata == null || string.IsNullOrWhiteSpace(definition.Metadata.GeneratorName))
            {
                errors.Add("Generator metadata is missing.");
            }

            return CreateResult(warnings, errors);
        }

        public static void ThrowIfConfigInvalid(PopulationConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var errors = new List<string>();
            if (config.Count <= 0)
            {
                errors.Add("Count must be greater than zero.");
            }

            if (config.ParameterRanges == null || config.DistributionTargets == null || config.GeneratorSettings == null)
            {
                errors.Add("Ranges, targets and generator settings are required.");
            }
            else
            {
                ValidateRange(errors, "WalkingSpeed", config.ParameterRanges.WalkingSpeed);
                ValidateRange(errors, "Patience", config.ParameterRanges.Patience);
                ValidateRange(errors, "Exploration", config.ParameterRanges.Exploration);
                ValidateRange(errors, "Sociability", config.ParameterRanges.Sociability);
                ValidateRange(errors, "Impulsiveness", config.ParameterRanges.Impulsiveness);
                ValidateRange(errors, "CrowdTolerance", config.ParameterRanges.CrowdTolerance);
                ValidateRange(errors, "PriceSensitivity", config.ParameterRanges.PriceSensitivity);
                if (config.GeneratorSettings.EvolutionPopulationSize <= 0 || config.GeneratorSettings.Generations <= 0)
                {
                    errors.Add("Generator population size and generation count are invalid.");
                }

                if (!IsFinite(config.GeneratorSettings.MutationProbability)
                    || config.GeneratorSettings.MutationProbability < 0.0
                    || config.GeneratorSettings.MutationProbability > 1.0
                    || !IsFinite(config.GeneratorSettings.CrossoverProbability)
                    || config.GeneratorSettings.CrossoverProbability < 0.0
                    || config.GeneratorSettings.CrossoverProbability > 1.0)
                {
                    errors.Add("Genetic operator probabilities are invalid.");
                }
            }

            var categories = config.CategoryIds ?? Array.Empty<string>();
            var categorySet = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < categories.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(categories[index]) || !categorySet.Add(categories[index]))
                {
                    errors.Add("Category IDs must be non-empty and unique.");
                    break;
                }
            }

            if (categories.Length == 0)
            {
                errors.Add("At least one category is required.");
            }

            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join(" ", errors), nameof(config));
            }
        }

        private static void ValidatePreferences(
            List<string> errors,
            List<string> warnings,
            NPCProfile profile,
            HashSet<string> categories)
        {
            var preferences = profile.CategoryPreferences ?? Array.Empty<CategoryPreference>();
            if (preferences.Length == 0)
            {
                errors.Add("NPC " + profile.Id + " has no category preferences.");
                return;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var total = 0.0;
            for (var index = 0; index < preferences.Length; index++)
            {
                var preference = preferences[index];
                if (preference == null || !categories.Contains(preference.CategoryId) || !seen.Add(preference.CategoryId))
                {
                    errors.Add("NPC " + profile.Id + " has an invalid category preference.");
                    continue;
                }

                if (!IsFinite(preference.Weight) || preference.Weight < 0.0)
                {
                    errors.Add("NPC " + profile.Id + " has an invalid category weight.");
                    continue;
                }

                total += preference.Weight;
            }

            if (Math.Abs(total - 1.0) > 1e-8)
            {
                warnings.Add("NPC " + profile.Id + " category weights do not sum to 1.");
            }
        }

        private static void ValidateDistributions(
            List<string> errors,
            List<string> warnings,
            NPCProfile[] profiles,
            PopulationDistributionTargets targets)
        {
            var statistics = PopulationStatistics.Calculate(profiles);
            ValidateDistribution(errors, warnings, "WalkingSpeed", statistics.WalkingSpeed, targets.WalkingSpeed);
            ValidateDistribution(errors, warnings, "Patience", statistics.Patience, targets.Patience);
            ValidateDistribution(errors, warnings, "Exploration", statistics.Exploration, targets.Exploration);
            ValidateDistribution(errors, warnings, "Sociability", statistics.Sociability, targets.Sociability);
            ValidateDistribution(errors, warnings, "Impulsiveness", statistics.Impulsiveness, targets.Impulsiveness);
            ValidateDistribution(errors, warnings, "CrowdTolerance", statistics.CrowdTolerance, targets.CrowdTolerance);
            ValidateDistribution(errors, warnings, "PriceSensitivity", statistics.PriceSensitivity, targets.PriceSensitivity);
        }

        private static void ValidateDistribution(
            List<string> errors,
            List<string> warnings,
            string name,
            NumericStatistics statistics,
            DistributionTarget target)
        {
            if (target == null || !target.Enabled)
            {
                return;
            }

            if (!IsFinite(target.Mean) || !IsFinite(target.StandardDeviation) || target.StandardDeviation < 0.0 || target.Tolerance < 0.0)
            {
                errors.Add(name + " distribution target is invalid.");
                return;
            }

            if (Math.Abs(statistics.Mean - target.Mean) > target.Tolerance)
            {
                errors.Add(name + " mean is outside target tolerance.");
            }
            else if (Math.Abs(statistics.StandardDeviation - target.StandardDeviation) > Math.Max(target.Tolerance, 0.05))
            {
                warnings.Add(name + " standard deviation differs from target.");
            }
        }

        private static void ValidateValue(List<string> errors, string id, string name, double value, ParameterRange range)
        {
            if (!IsFinite(value) || value < range.Min || value > range.Max)
            {
                errors.Add("NPC " + id + " has invalid " + name + ".");
            }
        }

        private static void ValidateRange(List<string> errors, string name, ParameterRange range)
        {
            if (range == null || !range.IsValid())
            {
                errors.Add(name + " range is invalid.");
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static ValidationResult CreateResult(List<string> warnings, List<string> errors)
        {
            return new ValidationResult
            {
                Valid = errors.Count == 0,
                Warnings = warnings.ToArray(),
                Errors = errors.ToArray()
            };
        }
    }
}
