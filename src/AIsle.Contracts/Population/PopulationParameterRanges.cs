using System;

namespace AIsle.Contracts.Population
{
    [Serializable]
    public sealed class PopulationParameterRanges
    {
        public ParameterRange WalkingSpeed = new ParameterRange(0.8, 1.8);
        public ParameterRange Patience = new ParameterRange(0.0, 1.0);
        public ParameterRange Exploration = new ParameterRange(0.0, 1.0);
        public ParameterRange Sociability = new ParameterRange(0.0, 1.0);
        public ParameterRange Impulsiveness = new ParameterRange(0.0, 1.0);
        public ParameterRange CrowdTolerance = new ParameterRange(0.0, 1.0);
        public ParameterRange PriceSensitivity = new ParameterRange(0.0, 1.0);
    }
}
