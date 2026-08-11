using System;
using System.Collections.Generic;
using AIsle.Contracts.Simulation;

namespace AIsle.Simulation.Runtime
{
    public static class PoissonSpawnSampler
    {
        public static double[] Sample(SpawnRatePoint[] curve, double durationSeconds, int maxCount)
        {
            var random = new Random();
            var points = Normalize(curve);
            if (points.Count == 0 || durationSeconds <= 0.0 || maxCount <= 0) return Array.Empty<double>();
            var maxRate = 0.0;
            for (var index = 0; index < points.Count; index++) maxRate = Math.Max(maxRate, points[index].Rate / 60.0);
            if (maxRate <= 0.0) return Array.Empty<double>();
            var result = new List<double>();
            var time = 0.0;
            while (result.Count < maxCount)
            {
                time += -Math.Log(Math.Max(double.Epsilon, 1.0 - random.NextDouble())) / maxRate;
                if (time >= durationSeconds) break;
                var rate = Interpolate(points, time / 60.0) / 60.0;
                if (random.NextDouble() * maxRate <= rate) result.Add(time);
            }
            return result.ToArray();
        }

        private static List<SpawnRatePoint> Normalize(SpawnRatePoint[] curve)
        {
            var result = new List<SpawnRatePoint>();
            var source = curve ?? Array.Empty<SpawnRatePoint>();
            for (var index = 0; index < source.Length; index++)
                if (source[index] != null && IsFinite(source[index].Minute) && source[index].Minute >= 0.0 && IsFinite(source[index].Rate) && source[index].Rate >= 0.0)
                    result.Add(new SpawnRatePoint { Minute = source[index].Minute, Rate = source[index].Rate });
            result.Sort((left, right) => left.Minute.CompareTo(right.Minute));
            for (var index = result.Count - 1; index > 0; index--) if (result[index].Minute == result[index - 1].Minute) result.RemoveAt(index - 1);
            return result;
        }

        private static double Interpolate(List<SpawnRatePoint> curve, double minute)
        {
            if (minute <= curve[0].Minute) return curve[0].Rate;
            for (var index = 1; index < curve.Count; index++)
            {
                var right = curve[index]; if (minute > right.Minute) continue;
                var left = curve[index - 1]; var span = right.Minute - left.Minute;
                return span == 0.0 ? right.Rate : left.Rate + ((right.Rate - left.Rate) * (minute - left.Minute) / span);
            }
            return curve[curve.Count - 1].Rate;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
