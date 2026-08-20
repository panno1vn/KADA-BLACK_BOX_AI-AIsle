using System;

namespace AIsle.Contracts.Simulation
{
    [Serializable]
    public sealed class SimulationStartInput
    {
        public string Name = string.Empty;
        public LayoutDefinition Layout = new LayoutDefinition();
        public ProductDefinition[] Catalog = Array.Empty<ProductDefinition>();
        public Population.PopulationDefinition Population = new Population.PopulationDefinition();
        public SimulationConfig Config = new SimulationConfig();
    }

    [Serializable]
    public sealed class SimulationStateProjection
    {
        public double Time;
        public bool Running;
        public bool Completed;
        public SimulationAgentProjection[] Agents = Array.Empty<SimulationAgentProjection>();
        public SimulationCountersProjection Counters = new SimulationCountersProjection();
    }

    [Serializable]
    public sealed class SimulationAgentProjection
    {
        public string Id = string.Empty;
        public double X;
        public double Y;
        public string Status = string.Empty;
        public string TargetId = string.Empty;
    }

    [Serializable]
    public sealed class SimulationCountersProjection
    {
        public int Active;
        public int Spawned;
        public int CompletedAgents;
        public int Converted;
        public int Purchases;
        public double Revenue;
        public int Unreachable;
        public int StuckRecoveries;
    }
}
