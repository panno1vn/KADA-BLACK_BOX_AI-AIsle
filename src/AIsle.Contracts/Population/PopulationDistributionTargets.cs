using System;

namespace AIsle.Contracts.Population
{
    [Serializable]
    public sealed class PopulationDistributionTargets
    {
        public DistributionTarget WalkingSpeed = new DistributionTarget();
        public DistributionTarget InitialNeed = new DistributionTarget();
        public DistributionTarget NeedGrowthPerMinute = new DistributionTarget();
        public DistributionTarget InitialExplorationNeed = new DistributionTarget();
        public DistributionTarget ExplorationGrowthPerMinute = new DistributionTarget();
        public DistributionTarget AffectAttractor = new DistributionTarget();
        public DistributionTarget AffectStability = new DistributionTarget();
        public DistributionTarget AffectDispersion = new DistributionTarget();
        public DistributionTarget AffectRecovery = new DistributionTarget();
        public DistributionTarget DwellSeconds = new DistributionTarget();

        // Compatibility fields. S8 activates Impulsiveness and PriceSensitivity;
        // the remaining legacy fields stay frozen.
        public DistributionTarget Patience = new DistributionTarget();
        public DistributionTarget Exploration = new DistributionTarget();
        public DistributionTarget Sociability = new DistributionTarget();
        public DistributionTarget Impulsiveness = new DistributionTarget();
        public DistributionTarget CrowdTolerance = new DistributionTarget();
        public DistributionTarget PriceSensitivity = new DistributionTarget();
    }
}
