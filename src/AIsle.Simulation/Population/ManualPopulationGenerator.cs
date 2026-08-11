using System;
using AIsle.Contracts.Population;

namespace AIsle.Simulation.Population
{
    public sealed class ManualPopulationGenerator : IPopulationGenerator
    {
        private readonly NPCProfile[] _profiles;

        public ManualPopulationGenerator(NPCProfile[] profiles)
        {
            _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        }

        public PopulationDefinition Generate(PopulationConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var profiles = CopyProfiles(_profiles);
            var definition = new PopulationDefinition
            {
                PopulationId = "manual-" + Guid.NewGuid().ToString("N"),
                NPCProfiles = profiles,
                Metadata = new PopulationMetadata
                {
                    GeneratorName = nameof(ManualPopulationGenerator),
                    GeneratorVersion = "manual-v2"
                }
            };
            return definition;
        }

        internal static NPCProfile[] CopyProfiles(NPCProfile[] source)
        {
            var copy = new NPCProfile[source.Length];
            for (var index = 0; index < source.Length; index++)
            {
                copy[index] = source[index] == null ? null : source[index].Copy();
            }

            return copy;
        }
    }
}
