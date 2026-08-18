using System;

namespace AIsle.Contracts.Simulation
{
    [Serializable] public sealed class Position2D { public double X; public double Y; public Position2D() { } public Position2D(double x, double y) { X = x; Y = y; } }
    [Serializable] public sealed class WallDefinition { public string Id = string.Empty; public double X1; public double Y1; public double X2; public double Y2; }
    [Serializable] public sealed class ShelfDefinition { public string Id = string.Empty; public string Label = string.Empty; public string Category = string.Empty; public double X; public double Y; public double Width; public double Height; public double Valence; }
    [Serializable] public sealed class ProductDefinition { public string Id = string.Empty; public string Name = string.Empty; public string Category = string.Empty; public string ShelfId = string.Empty; public double Price; }
    [Serializable] public sealed class SpawnRatePoint { public double Minute; public double Rate; }
    [Serializable] public sealed class LayoutDefinition
    {
        public double Width = 12; public double Height = 8;
        public WallDefinition[] Walls = Array.Empty<WallDefinition>();
        public ShelfDefinition[] Shelves = Array.Empty<ShelfDefinition>();
        public Position2D Entrance = new Position2D(); public Position2D Checkout = new Position2D();
        public SpawnRatePoint[] SpawnRateCurve = Array.Empty<SpawnRatePoint>();
    }
    [Serializable] public sealed class SimulationConfig
    {
        public double TickSeconds = 0.2; public double DurationMinutes = 30;
        public double UtilityNeedWeight = 1.0; public double UtilityExploreWeight = 0.72; public double UtilityValenceWeight = 0.16;
        public double DistancePenalty = 0.05; public double NeedAttenuationSharpness = 1.05; public int TopKChoices = 3;
        public double WeightedRandomSharpness = 2.5; public double DecisionNoise = 0.08;
        public double PurchaseNeedA = 3.0; public double PurchaseValenceB = 1.5; public double PurchaseBiasC = -2.0;
        public double ImpulseBase = 0.08; public int MaxShelfVisits = 3; public double DwellScale = 1.0; public double NeedTimeScale = 1.0;
        public double CollisionRadius = 0.32; public double SeparationStrength = 0.22;
        public double RvoNeighborDistance = 2.0; public int RvoMaxNeighbors = 10;
        public double RvoTimeHorizon = 2.0; public double RvoTimeHorizonObstacles = 2.0;
        public double PathCellSize = 0.25; public double ObstacleMargin = 0.28; public double StuckTimeout = 1.5; public int MaxReplans = 2;
        public double TrajectorySampleSeconds = 0.5;
    }
    [Serializable] public sealed class SimulationEvent
    {
        public double Time; public string NpcId = string.Empty; public string Type = string.Empty; public string Message = string.Empty;
        public string TargetCategory = string.Empty; public string ProductId = string.Empty; public string PurchaseType = string.Empty;
        public double Probability; public double Roll; public bool Bought;
    }
    [Serializable] public sealed class PurchaseRecord { public double Time; public string NpcId = string.Empty; public string ProductId = string.Empty; public string Type = string.Empty; public double Price; }
    [Serializable] public sealed class TrajectorySample { public double Time; public double X; public double Y; public string Status = string.Empty; public string ShelfId = string.Empty; }
    [Serializable] public sealed class AgentTrajectory { public string Id = string.Empty; public double Spawn; public TrajectorySample[] Samples = Array.Empty<TrajectorySample>(); }
    [Serializable] public sealed class ReplayData { public double SampleSeconds; public string[] Columns = { "time", "x", "y", "status", "shelfId" }; public AgentTrajectory[] Agents = Array.Empty<AgentTrajectory>(); }
    [Serializable] public sealed class SimulationSummary
    {
        public double DurationSeconds; public double Revenue; public int Purchases; public int Spawned; public int Converted;
        public int MainBuyers; public int ImpulseBuyers; public int NotFound; public int Unreachable; public int StuckRecoveries; public bool Completed;
    }
    [Serializable] public sealed class SimResult
    {
        public string SchemaVersion = SimulationSchemas.SimResultV1; public string Id = string.Empty; public DateTimeOffset CreatedAt = DateTimeOffset.UtcNow; public string Name = string.Empty;
        public SimulationSummary Summary = new SimulationSummary(); public SimulationEvent[] Events = Array.Empty<SimulationEvent>();
        public PurchaseRecord[] Purchases = Array.Empty<PurchaseRecord>(); public ReplayData Replay = new ReplayData();
    }

    public static class SimulationSchemas
    {
        public const string SimResultV1 = "aisle.sim-result.v1";
    }
}
