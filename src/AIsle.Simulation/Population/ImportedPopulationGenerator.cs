using System;
using AIsle.Contracts.Population;

namespace AIsle.Simulation.Population
{
    public sealed class ImportedPopulationGenerator : IPopulationGenerator
    {
        private readonly PopulationDefinition _definition;

        public ImportedPopulationGenerator(PopulationDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public PopulationDefinition Generate(PopulationConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return new PopulationDefinition
            {
                PopulationId = _definition.PopulationId,
                NPCProfiles = ManualPopulationGenerator.CopyProfiles(_definition.NPCProfiles ?? Array.Empty<NPCProfile>()),
                Metadata = new PopulationMetadata
                {
                    GeneratorName = nameof(ImportedPopulationGenerator),
                    GeneratorVersion = "import-v2"
                }
            };
        }
    }
}
