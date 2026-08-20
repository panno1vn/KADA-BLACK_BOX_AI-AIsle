using System;

namespace AIsle.Contracts.Population
{
    [Serializable]
    public sealed class PopulationParameterRanges
    {
        public ParameterRange WalkingSpeed = new ParameterRange(0.8, 1.8);
        public ParameterRange InitialNeed = new ParameterRange(0.0, 1.0);
        public ParameterRange NeedGrowthPerMinute = new ParameterRange(0.0, 0.06);
        public ParameterRange InitialExplorationNeed = new ParameterRange(0.0, 1.0);
        public ParameterRange ExplorationGrowthPerMinute = new ParameterRange(0.0, 0.06);
        public ParameterRange AffectAttractor = new ParameterRange(-1.0, 1.0);
        public ParameterRange AffectStability = new ParameterRange(0.0, 1.0);
        public ParameterRange AffectDispersion = new ParameterRange(0.0, 1.0);
        public ParameterRange AffectRecovery = new ParameterRange(0.0, 1.0);
        public ParameterRange DwellSeconds = new ParameterRange(1.0, 30.0);

        // Compatibility fields. S8 activates Impulsiveness and PriceSensitivity;
        // the remaining legacy fields stay frozen.
        public ParameterRange Patience = new ParameterRange(0.0, 1.0);
        public ParameterRange Exploration = new ParameterRange(0.0, 1.0);
        public ParameterRange Sociability = new ParameterRange(0.0, 1.0);
        public ParameterRange Impulsiveness = new ParameterRange(0.0, 1.0);
        public ParameterRange CrowdTolerance = new ParameterRange(0.0, 1.0);
        public ParameterRange PriceSensitivity = new ParameterRange(0.0, 1.0);
    }
}
