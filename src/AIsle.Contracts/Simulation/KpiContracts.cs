using System;

namespace AIsle.Contracts.Simulation
{
    [Serializable]
    public sealed class KpiProjection
    {
        public string ResultId = string.Empty;
        public KpiMetric[] Metrics = Array.Empty<KpiMetric>();
    }

    [Serializable]
    public sealed class KpiMetric
    {
        public string Key = string.Empty;
        public string Name = string.Empty;
        public string Unit = string.Empty;
        public double Value;
    }

    [Serializable]
    public sealed class ResultComparison
    {
        public string RunAId = string.Empty;
        public string RunBId = string.Empty;
        public KpiComparison[] Metrics = Array.Empty<KpiComparison>();
    }

    [Serializable]
    public sealed class KpiComparison
    {
        public string Key = string.Empty;
        public string Name = string.Empty;
        public string Unit = string.Empty;
        public double RunA;
        public double RunB;
        public double AbsoluteDelta;
        public double? RelativeDeltaPercent;
    }
}
