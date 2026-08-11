using System;

namespace AIsle.Simulation.Population
{
    [Serializable]
    public sealed class CategoryFrequency
    {
        public string CategoryId = string.Empty;
        public int PreferredByCount;
        public double MeanWeight;
    }
}
