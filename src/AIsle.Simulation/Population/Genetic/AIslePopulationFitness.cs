using System;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;

namespace AIsle.Simulation.Population.Genetic
{
    public sealed class AIslePopulationFitness : IFitness
    {
        public double Evaluate(IChromosome chromosome)
        {
            var aisle = chromosome as AIsleNpcChromosome;
            if (aisle == null)
            {
                throw new ArgumentException("Expected an AIsleNpcChromosome.", nameof(chromosome));
            }

            var penalty = 0.0;
            var totalWeight = 0.0;
            for (var index = 0; index < AIsleNpcChromosome.TraitCount; index++)
            {
                var target = aisle.TargetAt(index);
                if (target == null || !target.Enabled || target.Weight <= 0.0)
                {
                    continue;
                }

                var range = aisle.RangeAt(index);
                var span = Math.Max(range.Max - range.Min, 1e-9);
                penalty += target.Weight * Math.Abs(aisle.ValueAt(index) - target.Mean) / span;
                totalWeight += target.Weight;
            }

            return totalWeight <= 0.0 ? 1.0 : 1.0 / (1.0 + (penalty / totalWeight));
        }
    }
}
