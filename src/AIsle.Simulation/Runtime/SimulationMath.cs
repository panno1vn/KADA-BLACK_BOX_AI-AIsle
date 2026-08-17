using System;
using System.Collections.Generic;
using AIsle.Contracts.Simulation;

namespace AIsle.Simulation.Runtime
{
    internal static class SimulationMath
    {
        public static double Clamp(double value, double low, double high) => Math.Max(low, Math.Min(high, value));
        public static double Distance(Position2D a, Position2D b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));
        public static double Sigmoid(double value) => 1.0 / (1.0 + Math.Exp(-value));
        public static double Attenuate(double value, double sharpness) => 1.0 / Math.Max(0.001, sharpness - Clamp(value, 0.0, 1.0));

        public static T WeightedChoice<T>(IList<T> items, Func<T, double> weightOf, Random random)
        {
            if (items.Count == 0) return default(T);
            var weights = new double[items.Count];
            var total = 0.0;
            for (var index = 0; index < items.Count; index++) { weights[index] = Math.Max(0.0, weightOf(items[index])); total += weights[index]; }
            if (!(total > 0.0)) return items[random.Next(items.Count)];
            var roll = random.NextDouble() * total;
            for (var index = 0; index < items.Count; index++) { roll -= weights[index]; if (roll <= 0.0) return items[index]; }
            return items[items.Count - 1];
        }
    }
}
