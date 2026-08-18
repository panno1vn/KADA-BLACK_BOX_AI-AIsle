using System;
using System.Collections.Generic;
using AIsle.Contracts.Simulation;

namespace AIsle.Simulation.Runtime
{
    public static class SimulationConfigValidator
    {
        public static void ThrowIfInvalid(SimulationConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            var errors = new List<string>();

            InRange(errors, nameof(config.TickSeconds), config.TickSeconds, 0.02, 2.0);
            Positive(errors, nameof(config.DurationMinutes), config.DurationMinutes);
            NonNegative(errors, nameof(config.UtilityNeedWeight), config.UtilityNeedWeight);
            NonNegative(errors, nameof(config.UtilityExploreWeight), config.UtilityExploreWeight);
            NonNegative(errors, nameof(config.UtilityValenceWeight), config.UtilityValenceWeight);
            NonNegative(errors, nameof(config.DistancePenalty), config.DistancePenalty);
            Positive(errors, nameof(config.NeedAttenuationSharpness), config.NeedAttenuationSharpness);
            IntegerInRange(errors, nameof(config.TopKChoices), config.TopKChoices, 1, int.MaxValue);
            Positive(errors, nameof(config.WeightedRandomSharpness), config.WeightedRandomSharpness);
            NonNegative(errors, nameof(config.DecisionNoise), config.DecisionNoise);
            Finite(errors, nameof(config.PurchaseNeedA), config.PurchaseNeedA);
            Finite(errors, nameof(config.PurchaseValenceB), config.PurchaseValenceB);
            Finite(errors, nameof(config.PurchaseBiasC), config.PurchaseBiasC);
            InRange(errors, nameof(config.ImpulseBase), config.ImpulseBase, 0.0, 1.0);
            IntegerInRange(errors, nameof(config.MaxShelfVisits), config.MaxShelfVisits, 1, 10);
            NonNegative(errors, nameof(config.DwellScale), config.DwellScale);
            NonNegative(errors, nameof(config.NeedTimeScale), config.NeedTimeScale);
            Positive(errors, nameof(config.CollisionRadius), config.CollisionRadius);
            NonNegative(errors, nameof(config.SeparationStrength), config.SeparationStrength);
            InRange(errors, nameof(config.PathCellSize), config.PathCellSize, 0.1, 0.75);
            NonNegative(errors, nameof(config.ObstacleMargin), config.ObstacleMargin);
            InRange(errors, nameof(config.StuckTimeout), config.StuckTimeout, 0.2, 10.0);
            IntegerInRange(errors, nameof(config.MaxReplans), config.MaxReplans, 0, 8);
            InRange(errors, nameof(config.TrajectorySampleSeconds), config.TrajectorySampleSeconds, 0.05, 10.0);

            if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors), nameof(config));
        }

        private static void Finite(List<string> errors, string name, double value)
        {
            if (!double.IsFinite(value)) errors.Add(name + " must be finite.");
        }

        private static void Positive(List<string> errors, string name, double value)
        {
            if (!double.IsFinite(value) || value <= 0.0) errors.Add(name + " must be finite and greater than zero.");
        }

        private static void NonNegative(List<string> errors, string name, double value)
        {
            if (!double.IsFinite(value) || value < 0.0) errors.Add(name + " must be finite and non-negative.");
        }

        private static void InRange(List<string> errors, string name, double value, double minimum, double maximum)
        {
            if (!double.IsFinite(value) || value < minimum || value > maximum)
                errors.Add(name + " must be between " + minimum + " and " + maximum + ".");
        }

        private static void IntegerInRange(List<string> errors, string name, int value, int minimum, int maximum)
        {
            if (value < minimum || value > maximum) errors.Add(name + " is outside its supported range.");
        }
    }
}
