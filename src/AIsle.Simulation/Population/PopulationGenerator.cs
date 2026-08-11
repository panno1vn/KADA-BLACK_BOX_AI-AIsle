using System;
using AIsle.Contracts.Population;

namespace AIsle.Simulation.Population
{
    public sealed class PopulationGenerator
    {
        private readonly IPopulationGenerator _generator;

        public PopulationGenerator(IPopulationGenerator generator)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        }

        public PopulationDefinition Generate(PopulationConfig config)
        {
            return _generator.Generate(config);
        }
    }
}
