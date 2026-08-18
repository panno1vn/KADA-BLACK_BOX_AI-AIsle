using System;

namespace AIsle.Contracts.Simulation
{
    [Serializable]
    public sealed class ReplayProjection
    {
        public string ResultId = string.Empty;
        public double DurationSeconds;
        public double SampleSeconds;
        public ReplayAgentProjection[] Agents = Array.Empty<ReplayAgentProjection>();
    }

    [Serializable]
    public sealed class ReplayAgentProjection
    {
        public string Id = string.Empty;
        public double Spawn;
        public TrajectorySample[] Samples = Array.Empty<TrajectorySample>();
    }
}
