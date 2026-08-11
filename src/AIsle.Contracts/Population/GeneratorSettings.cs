using System;

namespace AIsle.Contracts.Population
{
    [Serializable]
    public sealed class GeneratorSettings
    {
        public int EvolutionPopulationSize = 256;
        public int Generations = 24;
        public double CrossoverProbability = 0.75;
        public double MutationProbability = 0.12;
    }
}
