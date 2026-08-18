using System;
using AIsle.Contracts.Simulation;

namespace AIsle.Simulation.Results
{
    public static class ReplayProjector
    {
        public static ReplayProjection Project(SimResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (!string.Equals(result.SchemaVersion, SimulationSchemas.SimResultV1, StringComparison.Ordinal))
                throw new ArgumentException("Unsupported SimResult schemaVersion.", nameof(result));
            if (result.Replay == null || result.Replay.Agents == null)
                throw new ArgumentException("Stored replay trajectory is missing.", nameof(result));

            var agents = new ReplayAgentProjection[result.Replay.Agents.Length];
            for (var agentIndex = 0; agentIndex < agents.Length; agentIndex++)
            {
                var source = result.Replay.Agents[agentIndex] ?? throw new ArgumentException("Stored replay agent is null.", nameof(result));
                var sourceSamples = source.Samples ?? Array.Empty<TrajectorySample>();
                var samples = new TrajectorySample[sourceSamples.Length];
                var previousTime = double.NegativeInfinity;
                for (var sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
                {
                    var sample = sourceSamples[sampleIndex] ?? throw new ArgumentException("Stored trajectory sample is null.", nameof(result));
                    if (!double.IsFinite(sample.Time) || sample.Time < previousTime)
                        throw new ArgumentException("Stored trajectory times must be finite and ordered.", nameof(result));
                    previousTime = sample.Time;
                    samples[sampleIndex] = new TrajectorySample
                    {
                        Time = sample.Time,
                        X = sample.X,
                        Y = sample.Y,
                        Status = sample.Status,
                        ShelfId = sample.ShelfId
                    };
                }

                agents[agentIndex] = new ReplayAgentProjection { Id = source.Id, Spawn = source.Spawn, Samples = samples };
            }

            return new ReplayProjection
            {
                ResultId = result.Id,
                DurationSeconds = result.Summary?.DurationSeconds ?? 0.0,
                SampleSeconds = result.Replay.SampleSeconds,
                Agents = agents
            };
        }
    }
}
