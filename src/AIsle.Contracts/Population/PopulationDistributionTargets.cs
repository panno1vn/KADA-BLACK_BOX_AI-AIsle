using System;

namespace AIsle.Contracts.Population
{
    [Serializable]
    public sealed class PopulationDistributionTargets
    {
        public DistributionTarget WalkingSpeed = new DistributionTarget();
        public DistributionTarget Patience = new DistributionTarget();
        public DistributionTarget Exploration = new DistributionTarget();
        public DistributionTarget Sociability = new DistributionTarget();
        public DistributionTarget Impulsiveness = new DistributionTarget();
        public DistributionTarget CrowdTolerance = new DistributionTarget();
        public DistributionTarget PriceSensitivity = new DistributionTarget();
    }
}
