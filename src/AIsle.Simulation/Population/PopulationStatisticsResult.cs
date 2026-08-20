using System;

namespace AIsle.Simulation.Population
{
    [Serializable]
    public sealed class PopulationStatisticsResult
    {
        public int Count;
        public NumericStatistics WalkingSpeed = new NumericStatistics();
        public NumericStatistics InitialNeed = new NumericStatistics();
        public NumericStatistics NeedGrowthPerMinute = new NumericStatistics();
        public NumericStatistics InitialExplorationNeed = new NumericStatistics();
        public NumericStatistics ExplorationGrowthPerMinute = new NumericStatistics();
        public NumericStatistics AffectAttractor = new NumericStatistics();
        public NumericStatistics AffectStability = new NumericStatistics();
        public NumericStatistics AffectDispersion = new NumericStatistics();
        public NumericStatistics AffectRecovery = new NumericStatistics();
        public NumericStatistics DwellSeconds = new NumericStatistics();
        public NumericStatistics Impulsiveness = new NumericStatistics();
        public NumericStatistics PriceSensitivity = new NumericStatistics();
        public CategoryFrequency[] TargetCategoryFrequency = Array.Empty<CategoryFrequency>();
    }
}
