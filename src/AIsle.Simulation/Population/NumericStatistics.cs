using System;

namespace AIsle.Simulation.Population
{
    [Serializable]
    public sealed class NumericStatistics
    {
        public double Mean;
        public double Median;
        public double Min;
        public double Max;
        public double StandardDeviation;
        public double Percentile10;
        public double Percentile25;
        public double Percentile50;
        public double Percentile75;
        public double Percentile90;
    }
}
