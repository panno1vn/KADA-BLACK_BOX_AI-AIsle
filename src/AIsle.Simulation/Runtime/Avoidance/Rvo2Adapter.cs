using System;
using System.Collections.Generic;
using RVO;

namespace AIsle.Simulation.Runtime.Avoidance
{
    public sealed class Rvo2Adapter : IRvoAvoidance
    {
        // Upstream exposes one process-wide simulator. Serialize the short Clear/Add/Step/Get cycle
        // so independent SimulationHost instances cannot corrupt each other's avoidance step.
        private static readonly object SimulatorGate = new object();

        public IReadOnlyList<RvoVelocity> Solve(IReadOnlyList<RvoAgentInput> agents, RvoAvoidanceSettings settings, double deltaSeconds)
        {
            if (agents == null) throw new ArgumentNullException(nameof(agents));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var result = new RvoVelocity[agents.Count];
            if (agents.Count < 2)
            {
                for (var index = 0; index < agents.Count; index++)
                    result[index] = Preferred(agents[index]);
                return result;
            }

            lock (SimulatorGate)
            {
                var simulator = Simulator.Instance;
                simulator.Clear();
                simulator.setTimeStep(ToPositiveFloat(deltaSeconds, nameof(deltaSeconds)));

                for (var index = 0; index < agents.Count; index++)
                {
                    var agent = agents[index];
                    simulator.addAgent(
                        new Vector2(ToFloat(agent.X), ToFloat(agent.Y)),
                        ToNonNegativeFloat(settings.NeighborDistance, nameof(settings.NeighborDistance)),
                        settings.MaxNeighbors,
                        ToPositiveFloat(settings.TimeHorizon, nameof(settings.TimeHorizon)),
                        ToPositiveFloat(settings.TimeHorizonObstacles, nameof(settings.TimeHorizonObstacles)),
                        ToNonNegativeFloat(agent.Radius, nameof(agent.Radius)),
                        ToNonNegativeFloat(agent.MaxSpeed, nameof(agent.MaxSpeed)),
                        new Vector2(ToFloat(agent.VelocityX), ToFloat(agent.VelocityY)));
                    simulator.setAgentPrefVelocity(index, new Vector2(ToFloat(agent.PreferredVelocityX), ToFloat(agent.PreferredVelocityY)));
                }

                simulator.doStep();
                for (var index = 0; index < agents.Count; index++)
                {
                    var velocity = simulator.getAgentVelocity(index);
                    result[index] = new RvoVelocity(velocity.x(), velocity.y());
                }
            }

            return result;
        }

        private static RvoVelocity Preferred(RvoAgentInput agent) => new RvoVelocity(agent.PreferredVelocityX, agent.PreferredVelocityY);

        private static float ToFloat(double value)
        {
            if (!double.IsFinite(value) || value < -float.MaxValue || value > float.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value));
            return (float)value;
        }

        private static float ToPositiveFloat(double value, string name)
        {
            if (!double.IsFinite(value) || value <= 0.0 || value > float.MaxValue)
                throw new ArgumentOutOfRangeException(name);
            return (float)value;
        }

        private static float ToNonNegativeFloat(double value, string name)
        {
            if (!double.IsFinite(value) || value < 0.0 || value > float.MaxValue)
                throw new ArgumentOutOfRangeException(name);
            return (float)value;
        }
    }
}
