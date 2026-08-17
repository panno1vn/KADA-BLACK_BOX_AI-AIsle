using System;
using AIsle.Contracts.Population;
using GeneticSharp.Domain.Chromosomes;
using MathNet.Numerics.Distributions;

namespace AIsle.Simulation.Population.Genetic
{
    public sealed class AIsleNpcChromosome : ChromosomeBase
    {
        public const int TraitCount = 7;
        private readonly PopulationConfig _config;

        public AIsleNpcChromosome(PopulationConfig config)
            : base(TraitCount + (config.CategoryIds ?? Array.Empty<string>()).Length + 1)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            CreateGenes();
        }

        public override Gene GenerateGene(int geneIndex)
        {
            if (geneIndex < TraitCount)
            {
                var range = RangeAt(geneIndex);
                var target = TargetAt(geneIndex);
                var value = target != null && target.Enabled
                    ? SampleNormal(target.Mean, target.StandardDeviation)
                    : SampleUniform(range.Min, range.Max);
                return new Gene(range.Clamp(value));
            }

            return new Gene(ContinuousUniform.Sample(0.0, 1.0));
        }

        public override IChromosome CreateNew()
        {
            return new AIsleNpcChromosome(_config);
        }

        public double ValueAt(int index)
        {
            return Convert.ToDouble(GetGene(index).Value);
        }

        public void SetValueAt(int index, double value)
        {
            ReplaceGene(index, new Gene(value));
        }

        public ParameterRange RangeAt(int index)
        {
            var ranges = _config.ParameterRanges;
            switch (index)
            {
                case 0: return ranges.WalkingSpeed;
                case 1: return ranges.Patience;
                case 2: return ranges.Exploration;
                case 3: return ranges.Sociability;
                case 4: return ranges.Impulsiveness;
                case 5: return ranges.CrowdTolerance;
                case 6: return ranges.PriceSensitivity;
                default: return new ParameterRange(0.0, 1.0);
            }
        }

        public DistributionTarget TargetAt(int index)
        {
            var targets = _config.DistributionTargets;
            switch (index)
            {
                case 0: return targets.WalkingSpeed;
                case 1: return targets.Patience;
                case 2: return targets.Exploration;
                case 3: return targets.Sociability;
                case 4: return targets.Impulsiveness;
                case 5: return targets.CrowdTolerance;
                case 6: return targets.PriceSensitivity;
                default: return null;
            }
        }

        private static double SampleNormal(double mean, double standardDeviation)
        {
            return standardDeviation <= 0.0 ? mean : Normal.Sample(mean, standardDeviation);
        }

        private static double SampleUniform(double min, double max)
        {
            return min == max ? min : ContinuousUniform.Sample(min, max);
        }
    }
}
