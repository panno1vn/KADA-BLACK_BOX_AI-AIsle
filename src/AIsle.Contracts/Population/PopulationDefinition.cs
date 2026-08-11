using System;

namespace AIsle.Contracts.Population
{
    [Serializable]
    public sealed class PopulationDefinition
    {
        public string PopulationId = string.Empty;
        public NPCProfile[] NPCProfiles = Array.Empty<NPCProfile>();
        public PopulationMetadata Metadata = new PopulationMetadata();
    }
}
