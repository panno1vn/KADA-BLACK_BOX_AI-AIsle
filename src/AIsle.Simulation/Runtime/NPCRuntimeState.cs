using System;
using System.Collections.Generic;
using AIsle.Contracts.Population;
using AIsle.Contracts.Simulation;

namespace AIsle.Simulation.Runtime
{
    public sealed class NPCRuntimeState
    {
        public NPCProfile Profile;
        public double X; public double Y; public string Status = "WAITING"; public double Spawn;
        public double VelocityX; public double VelocityY;
        public double Valence; public double Need; public double Explore; public List<Position2D> Path = new List<Position2D>(); public int PathIndex;
        public double DwellLeft; public List<string> Visited = new List<string>(); public bool BoughtMain; public bool BoughtImpulse; public bool Converted;
        public string CurrentShelf = string.Empty; public bool Finished; public double StuckFor; public int Replans;
        public Position2D RouteTarget; public string RouteStatus = string.Empty; public double StridePhase;
        public double LastTrajectoryTime = double.NegativeInfinity; public string LastTrajectoryStatus = string.Empty;
        public readonly List<TrajectorySample> Trajectory = new List<TrajectorySample>();

        public NPCRuntimeState(NPCProfile profile, Position2D entrance, double spawn, Random random)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile)); X = entrance.X; Y = entrance.Y; Spawn = spawn;
            Valence = profile.AffectAttractor; Need = profile.InitialNeed; Explore = profile.InitialExplorationNeed; StridePhase = random.NextDouble() * Math.PI * 2.0;
        }

        public Position2D Position() => new Position2D(X, Y);
        public double Speed() => Math.Sqrt((VelocityX * VelocityX) + (VelocityY * VelocityY));
    }
}
