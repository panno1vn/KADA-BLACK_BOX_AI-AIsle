using System;

namespace AIsle.Contracts.Population
{
    [Serializable]
    public sealed class PopulationMetadata
    {
        public string GeneratorName = string.Empty;
        public string GeneratorVersion = string.Empty;
    }
}
