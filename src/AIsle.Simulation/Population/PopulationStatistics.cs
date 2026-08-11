using System;
using System.Collections.Generic;
using AIsle.Contracts.Population;
using MathNet.Numerics.Statistics;

namespace AIsle.Simulation.Population
{
    public static class PopulationStatistics
    {
        public static PopulationStatisticsResult Calculate(PopulationDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return Calculate(definition.NPCProfiles ?? Array.Empty<NPCProfile>());
        }

        public static PopulationStatisticsResult Calculate(NPCProfile[] profiles)
        {
            if (profiles == null)
            {
                throw new ArgumentNullException(nameof(profiles));
            }

            return new PopulationStatisticsResult
            {
                Count = profiles.Length,
                WalkingSpeed = CalculateMetric(profiles, profile => profile.WalkingSpeed),
                Patience = CalculateMetric(profiles, profile => profile.Patience),
                Exploration = CalculateMetric(profiles, profile => profile.Exploration),
                Sociability = CalculateMetric(profiles, profile => profile.Sociability),
                Impulsiveness = CalculateMetric(profiles, profile => profile.Impulsiveness),
                CrowdTolerance = CalculateMetric(profiles, profile => profile.CrowdTolerance),
                PriceSensitivity = CalculateMetric(profiles, profile => profile.PriceSensitivity),
                CategoryPreferenceFrequency = CalculateCategoryFrequency(profiles)
            };
        }

        private static NumericStatistics CalculateMetric(NPCProfile[] profiles, Func<NPCProfile, double> selector)
        {
            if (profiles.Length == 0)
            {
                return new NumericStatistics();
            }

            var values = new double[profiles.Length];
            for (var index = 0; index < profiles.Length; index++)
            {
                values[index] = selector(profiles[index]);
            }

            Array.Sort(values);
            return new NumericStatistics
            {
                Mean = ArrayStatistics.Mean(values),
                Median = SortedArrayStatistics.Median(values),
                Min = SortedArrayStatistics.Minimum(values),
                Max = SortedArrayStatistics.Maximum(values),
                StandardDeviation = ArrayStatistics.PopulationStandardDeviation(values),
                Percentile10 = SortedArrayStatistics.Percentile(values, 10),
                Percentile25 = SortedArrayStatistics.Percentile(values, 25),
                Percentile50 = SortedArrayStatistics.Percentile(values, 50),
                Percentile75 = SortedArrayStatistics.Percentile(values, 75),
                Percentile90 = SortedArrayStatistics.Percentile(values, 90)
            };
        }

        private static CategoryFrequency[] CalculateCategoryFrequency(NPCProfile[] profiles)
        {
            var totals = new Dictionary<string, double>(StringComparer.Ordinal);
            var preferredCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var profileIndex = 0; profileIndex < profiles.Length; profileIndex++)
            {
                var preferences = profiles[profileIndex].CategoryPreferences ?? Array.Empty<CategoryPreference>();
                for (var preferenceIndex = 0; preferenceIndex < preferences.Length; preferenceIndex++)
                {
                    var preference = preferences[preferenceIndex];
                    if (preference == null || string.IsNullOrWhiteSpace(preference.CategoryId))
                    {
                        continue;
                    }

                    totals[preference.CategoryId] = totals.TryGetValue(preference.CategoryId, out var total)
                        ? total + preference.Weight
                        : preference.Weight;
                    if (preference.Weight > 0.0)
                    {
                        preferredCounts[preference.CategoryId] = preferredCounts.TryGetValue(preference.CategoryId, out var count)
                            ? count + 1
                            : 1;
                    }
                }
            }

            var categoryIds = new List<string>(totals.Keys);
            categoryIds.Sort(StringComparer.Ordinal);
            var frequencies = new CategoryFrequency[categoryIds.Count];
            for (var index = 0; index < categoryIds.Count; index++)
            {
                var categoryId = categoryIds[index];
                frequencies[index] = new CategoryFrequency
                {
                    CategoryId = categoryId,
                    PreferredByCount = preferredCounts.TryGetValue(categoryId, out var count) ? count : 0,
                    MeanWeight = profiles.Length == 0 ? 0.0 : totals[categoryId] / profiles.Length
                };
            }

            return frequencies;
        }
    }
}
