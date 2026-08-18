using System;
using AIsle.Contracts.Population;
using GeneticSharp.Domain.Chromosomes;
using MathNet.Numerics.Distributions;

namespace AIsle.Simulation.Population.Genetic
{
    public sealed class AIsleNpcChromosome : ChromosomeBase
    {
        public const int TraitCount = 12;
        private readonly PopulationConfig _config;

        public AIsleNpcChromosome(PopulationConfig config)
            : base(TraitCount + 2)
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
                case 1: return ranges.InitialNeed;
                case 2: return ranges.NeedGrowthPerMinute;
                case 3: return ranges.InitialExplorationNeed;
                case 4: return ranges.ExplorationGrowthPerMinute;
                case 5: return ranges.AffectAttractor;
                case 6: return ranges.AffectStability;
                case 7: return ranges.AffectDispersion;
                case 8: return ranges.AffectRecovery;
                case 9: return ranges.DwellSeconds;
                case 10: return ranges.Impulsiveness;
                case 11: return ranges.PriceSensitivity;
                default: return new ParameterRange(0.0, 1.0);
            }
        }

        public DistributionTarget TargetAt(int index)
        {
            var targets = _config.DistributionTargets;
            switch (index)
            {
                case 0: return targets.WalkingSpeed;
                case 1: return targets.InitialNeed;
                case 2: return targets.NeedGrowthPerMinute;
                case 3: return targets.InitialExplorationNeed;
                case 4: return targets.ExplorationGrowthPerMinute;
                case 5: return targets.AffectAttractor;
                case 6: return targets.AffectStability;
                case 7: return targets.AffectDispersion;
                case 8: return targets.AffectRecovery;
                case 9: return targets.DwellSeconds;
                case 10: return targets.Impulsiveness;
                case 11: return targets.PriceSensitivity;
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
