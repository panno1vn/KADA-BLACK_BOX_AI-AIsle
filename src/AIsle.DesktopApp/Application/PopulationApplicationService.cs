using System;
using AIsle.Contracts.Population;
using AIsle.Simulation.Population;
using AIsle.Simulation.Population.Genetic;

namespace AIsle.DesktopApp.Application
{
    public sealed class PopulationApplicationService
    {
        private readonly IPopulationGenerator _generator;
        private readonly PopulationValidator _validator;

        public PopulationApplicationService(
            IPopulationGenerator? generator = null,
            PopulationValidator? validator = null)
        {
            _generator = generator ?? new GeneticPopulationGenerator();
            _validator = validator ?? new PopulationValidator();
        }

        public PopulationGenerationResult Generate(PopulationConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            var definition = _generator.Generate(config);
            return new PopulationGenerationResult
            {
                Profiles = definition.NPCProfiles ?? Array.Empty<NPCProfile>(),
                Summary = PopulationStatistics.Calculate(definition),
                Validation = _validator.Validate(definition, config)
            };
        }
    }

    public sealed class PopulationGenerationResult
    {
        public NPCProfile[] Profiles { get; set; } = Array.Empty<NPCProfile>();
        public PopulationStatisticsResult Summary { get; set; } = new PopulationStatisticsResult();
        public ValidationResult Validation { get; set; } = new ValidationResult();
    }
}
