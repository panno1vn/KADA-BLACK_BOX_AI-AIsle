using System;

namespace AIsle.Contracts.Population
{
    [Serializable]
    public sealed class DistributionTarget
    {
        public bool Enabled;
        public double Mean = 0.5;
        public double StandardDeviation = 0.2;
        public double Weight = 1.0;
        public double Tolerance = 0.15;
    }
}
