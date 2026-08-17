using AIsle.Contracts.Simulation;

namespace AIsle.Simulation.Runtime
{
    public static class NeedAffectSystem
    {
        public static void Update(NPCRuntimeState agent, double deltaSeconds, SimulationConfig config)
        {
            agent.Need = SimulationMath.Clamp(agent.Need + (agent.Profile.NeedGrowthPerMinute * deltaSeconds / 60.0 * config.NeedTimeScale), 0.0, 1.0);
            agent.Explore = SimulationMath.Clamp(agent.Explore + (agent.Profile.ExplorationGrowthPerMinute * deltaSeconds / 60.0 * config.NeedTimeScale), 0.0, 1.0);
        }
        public static void ApplyShelfExperience(NPCRuntimeState agent, double shelfValence)
        {
            agent.Valence = SimulationMath.Clamp(agent.Valence + ((shelfValence - agent.Valence) * agent.Profile.AffectDispersion * (1.0 - agent.Profile.AffectStability)), -1.0, 1.0);
        }
        public static void Recover(NPCRuntimeState agent) { agent.Valence += (agent.Profile.AffectAttractor - agent.Valence) * agent.Profile.AffectRecovery; }
    }
}
