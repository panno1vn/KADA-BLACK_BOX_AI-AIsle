using System.Collections.Generic;

namespace AIsle.Simulation.Runtime.Avoidance
{
    public interface IRvoAvoidance
    {
        IReadOnlyList<RvoVelocity> Solve(IReadOnlyList<RvoAgentInput> agents, RvoAvoidanceSettings settings, double deltaSeconds);
    }

    public sealed class RvoAvoidanceSettings
    {
        public double NeighborDistance;
        public int MaxNeighbors;
        public double TimeHorizon;
        public double TimeHorizonObstacles;
    }

    public sealed class RvoAgentInput
    {
        public double X;
        public double Y;
        public double VelocityX;
        public double VelocityY;
        public double PreferredVelocityX;
        public double PreferredVelocityY;
        public double Radius;
        public double MaxSpeed;
    }

    public readonly struct RvoVelocity
    {
        public RvoVelocity(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }
        public double Y { get; }
    }
}
