using System;
using System.Collections.Generic;
using AIsle.Contracts.Simulation;

namespace AIsle.Simulation.Results
{
    public static class KpiProjector
    {
        public static KpiProjection Project(SimResult result)
        {
            Validate(result);
            var purchases = result.Purchases ?? Array.Empty<PurchaseRecord>();
            var revenue = 0.0;
            for (var index = 0; index < purchases.Length; index++)
            {
                var price = purchases[index]?.Price ?? 0.0;
                if (!double.IsFinite(price)) throw new ArgumentException("Purchase price must be finite.", nameof(result));
                revenue += price;
            }

            var spawned = Math.Max(0, result.Summary.Spawned);
            var converted = Math.Max(0, result.Summary.Converted);
            var conversionRate = spawned == 0 ? 0.0 : Math.Min(100.0, (double)converted / spawned * 100.0);
            var shelfVisits = 0;
            var dwellSeconds = 0.0;
            var pathLength = 0.0;
            var agents = result.Replay.Agents ?? Array.Empty<AgentTrajectory>();
            for (var agentIndex = 0; agentIndex < agents.Length; agentIndex++)
            {
                var samples = agents[agentIndex]?.Samples ?? Array.Empty<TrajectorySample>();
                for (var sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
                {
                    var current = samples[sampleIndex] ?? throw new ArgumentException("Trajectory sample is null.", nameof(result));
                    if (!IsFinite(current)) throw new ArgumentException("Trajectory sample values must be finite.", nameof(result));
                    var previous = sampleIndex == 0 ? null : samples[sampleIndex - 1];
                    if (string.Equals(current.Status, "DWELL", StringComparison.Ordinal)
                        && !string.IsNullOrWhiteSpace(current.ShelfId)
                        && (previous == null || !string.Equals(previous.Status, "DWELL", StringComparison.Ordinal)
                            || !string.Equals(previous.ShelfId, current.ShelfId, StringComparison.Ordinal)))
                        shelfVisits++;

                    if (sampleIndex + 1 < samples.Length)
                    {
                        var next = samples[sampleIndex + 1] ?? throw new ArgumentException("Trajectory sample is null.", nameof(result));
                        if (!IsFinite(next) || next.Time < current.Time) throw new ArgumentException("Trajectory sample times must be ordered.", nameof(result));
                        if (string.Equals(current.Status, "DWELL", StringComparison.Ordinal)) dwellSeconds += next.Time - current.Time;
                        var dx = next.X - current.X;
                        var dy = next.Y - current.Y;
                        pathLength += Math.Sqrt((dx * dx) + (dy * dy));
                    }
                }
            }

            var checkoutCompletions = 0;
            var events = result.Events ?? Array.Empty<SimulationEvent>();
            for (var index = 0; index < events.Length; index++)
                if (string.Equals(events[index]?.Type, "checkout", StringComparison.Ordinal)) checkoutCompletions++;

            return new KpiProjection
            {
                ResultId = result.Id,
                Metrics = new[]
                {
                    Metric("purchase_count", "Purchase count", "count", purchases.Length),
                    Metric("conversion_rate", "Conversion rate", "percent", conversionRate),
                    Metric("revenue", "Revenue", "currency", revenue),
                    Metric("shelf_visits", "Shelf visits", "count", shelfVisits),
                    Metric("dwell_time_seconds", "Dwell time", "seconds", dwellSeconds),
                    Metric("path_length_meters", "Path length", "meters", pathLength),
                    Metric("checkout_completions", "Checkout completions", "count", checkoutCompletions)
                }
            };
        }

        private static void Validate(SimResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.Summary == null || result.Purchases == null || result.Events == null || result.Replay == null || result.Replay.Agents == null)
                throw new ArgumentException("Stored result is incomplete.", nameof(result));
        }

        private static bool IsFinite(TrajectorySample sample) =>
            double.IsFinite(sample.Time) && double.IsFinite(sample.X) && double.IsFinite(sample.Y);

        private static KpiMetric Metric(string key, string name, string unit, double value) =>
            new KpiMetric { Key = key, Name = name, Unit = unit, Value = value };
    }

    public static class ResultComparer
    {
        public static ResultComparison Compare(SimResult runA, SimResult runB)
        {
            var projectionA = KpiProjector.Project(runA);
            var projectionB = KpiProjector.Project(runB);
            var byKey = new Dictionary<string, KpiMetric>(StringComparer.Ordinal);
            for (var index = 0; index < projectionB.Metrics.Length; index++) byKey.Add(projectionB.Metrics[index].Key, projectionB.Metrics[index]);
            var comparisons = new KpiComparison[projectionA.Metrics.Length];
            for (var index = 0; index < comparisons.Length; index++)
            {
                var left = projectionA.Metrics[index];
                if (!byKey.TryGetValue(left.Key, out var right)) throw new InvalidOperationException("KPI sets do not match.");
                var delta = right.Value - left.Value;
                comparisons[index] = new KpiComparison
                {
                    Key = left.Key,
                    Name = left.Name,
                    Unit = left.Unit,
                    RunA = left.Value,
                    RunB = right.Value,
                    AbsoluteDelta = delta,
                    RelativeDeltaPercent = left.Value == 0.0 ? (double?)null : delta / Math.Abs(left.Value) * 100.0
                };
            }

            return new ResultComparison { RunAId = runA.Id, RunBId = runB.Id, Metrics = comparisons };
        }
    }
}
