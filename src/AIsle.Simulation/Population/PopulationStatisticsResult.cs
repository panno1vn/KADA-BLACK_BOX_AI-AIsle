using System;

namespace AIsle.Simulation.Population
{
    [Serializable]
    public sealed class PopulationStatisticsResult
    {
        public int Count;
        public NumericStatistics WalkingSpeed = new NumericStatistics();
        public NumericStatistics Patience = new NumericStatistics();
        public NumericStatistics Exploration = new NumericStatistics();
        public NumericStatistics Sociability = new NumericStatistics();
        public NumericStatistics Impulsiveness = new NumericStatistics();
        public NumericStatistics CrowdTolerance = new NumericStatistics();
        public NumericStatistics PriceSensitivity = new NumericStatistics();
        public CategoryFrequency[] CategoryPreferenceFrequency = Array.Empty<CategoryFrequency>();
    }
}
