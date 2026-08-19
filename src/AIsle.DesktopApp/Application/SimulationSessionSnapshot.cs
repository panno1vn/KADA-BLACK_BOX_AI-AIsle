using System;
using AIsle.Contracts.Simulation;

namespace AIsle.DesktopApp.Application
{
    public sealed class SimulationSessionSnapshot
    {
        public string RunId { get; set; } = string.Empty;
        public double SpeedMultiplier { get; set; } = 1.0;
        public SimulationStateProjection State { get; set; } = new SimulationStateProjection();
        public SimulationSummary Summary { get; set; } = new SimulationSummary();
        public SimulationEvent[] Events { get; set; } = Array.Empty<SimulationEvent>();
        public PurchaseRecord[] Purchases { get; set; } = Array.Empty<PurchaseRecord>();
    }
}
