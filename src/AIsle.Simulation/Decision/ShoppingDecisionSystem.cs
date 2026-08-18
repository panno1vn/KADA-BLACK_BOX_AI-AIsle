using System;
using System.Linq;
using AIsle.Contracts.Population;
using AIsle.Contracts.Simulation;
using AIsle.Simulation.Runtime;

namespace AIsle.Simulation.Decision
{
    public static class ShoppingDecisionSystem
    {
        public static TargetDecisionEvaluation EvaluateTarget(
            NPCRuntimeState agent,
            ShelfDefinition shelf,
            ProductDefinition[] shelfProducts,
            double pathLength,
            SimulationConfig config)
        {
            if (agent == null) throw new ArgumentNullException(nameof(agent));
            if (shelf == null) throw new ArgumentNullException(nameof(shelf));
            if (config == null) throw new ArgumentNullException(nameof(config));

            var products = shelfProducts ?? Array.Empty<ProductDefinition>();
            var matchesMissionCategory = products.Any(product =>
                string.Equals(product.Category, agent.Profile.TargetCategory, StringComparison.Ordinal));
            var needAmount = matchesMissionCategory ? agent.Need : 0.0;
            var needDelta = SimulationMath.Attenuate(agent.Need, config.NeedAttenuationSharpness)
                - SimulationMath.Attenuate(Math.Max(0.0, agent.Need - needAmount), config.NeedAttenuationSharpness);
            var need = config.UtilityNeedWeight * needDelta;
            var preference = products.Length == 0
                ? 0.0
                : products.Max(product => PreferenceFor(agent.Profile, product.Category));
            var mission = config.UtilityExploreWeight * MissionFit(agent, matchesMissionCategory, preference);
            var travel = config.DistancePenalty * Math.Max(pathLength * pathLength, 0.25);

            return new TargetDecisionEvaluation
            {
                Total = need + preference + mission - travel,
                Need = need,
                Preference = preference,
                Mission = mission,
                Travel = travel
            };
        }

        public static PurchaseDecisionEvaluation EvaluateMainPurchase(
            NPCRuntimeState agent,
            ProductDefinition product,
            SimulationConfig config)
        {
            if (agent == null) throw new ArgumentNullException(nameof(agent));
            if (product == null) throw new ArgumentNullException(nameof(product));
            if (config == null) throw new ArgumentNullException(nameof(config));

            var need = config.PurchaseNeedA * SimulationMath.Clamp(agent.Need, 0.0, 1.0);
            var preference = PreferenceFor(agent.Profile, product.Category);
            var price = SimulationMath.Clamp(agent.Profile.PriceSensitivity, 0.0, 1.0) * NormalizePrice(product.Price);
            var impulse = SimulationMath.Clamp(agent.Profile.Impulsiveness, 0.0, 1.0);
            var score = need + preference + impulse - price + config.PurchaseBiasC;
            return new PurchaseDecisionEvaluation
            {
                Probability = SimulationMath.Sigmoid(score),
                Need = need,
                Preference = preference,
                Price = price,
                Impulse = impulse,
                Score = score
            };
        }

        public static PurchaseDecisionEvaluation EvaluateImpulsePurchase(
            NPCRuntimeState agent,
            ProductDefinition product,
            SimulationConfig config)
        {
            if (agent == null) throw new ArgumentNullException(nameof(agent));
            if (product == null) throw new ArgumentNullException(nameof(product));
            if (config == null) throw new ArgumentNullException(nameof(config));

            var preference = PreferenceFor(agent.Profile, product.Category);
            var price = SimulationMath.Clamp(agent.Profile.PriceSensitivity, 0.0, 1.0) * NormalizePrice(product.Price);
            var impulse = SimulationMath.Clamp(agent.Profile.Impulsiveness, 0.0, 1.0);
            var probability = config.ImpulseBase * impulse * (1.0 + preference) * (1.0 - price);
            return new PurchaseDecisionEvaluation
            {
                Probability = SimulationMath.Clamp(probability, 0.0, 1.0),
                Preference = preference,
                Price = price,
                Impulse = impulse,
                Score = probability
            };
        }

        public static double PreferenceFor(NPCProfile profile, string category)
        {
            if (profile == null || string.IsNullOrWhiteSpace(category)) return 0.0;
            var preferences = profile.CategoryPreferences ?? Array.Empty<CategoryPreference>();
            var match = preferences.FirstOrDefault(item => item != null
                && string.Equals(item.CategoryId, category, StringComparison.Ordinal));
            if (match != null) return SimulationMath.Clamp(match.Weight, 0.0, 1.0);
            return string.Equals(profile.TargetCategory, category, StringComparison.Ordinal) ? 1.0 : 0.0;
        }

        private static double MissionFit(NPCRuntimeState agent, bool matchesMissionCategory, double preference)
        {
            switch (agent.Profile.ShoppingMission)
            {
                case ShoppingMission.QuickTopUp:
                    return matchesMissionCategory ? 1.0 : 0.0;
                case ShoppingMission.PlannedBasket:
                    return Math.Max(matchesMissionCategory ? 1.0 : 0.0, preference);
                case ShoppingMission.Exploration:
                    return SimulationMath.Clamp(agent.Explore, 0.0, 1.0) * (1.0 - preference);
                default:
                    return preference;
            }
        }

        private static double NormalizePrice(double price)
        {
            var nonNegative = Math.Max(0.0, price);
            return nonNegative / (1.0 + nonNegative);
        }
    }

    public sealed class TargetDecisionEvaluation
    {
        public double Total;
        public double Need;
        public double Preference;
        public double Mission;
        public double Travel;
    }

    public sealed class PurchaseDecisionEvaluation
    {
        public double Probability;
        public double Score;
        public double Need;
        public double Preference;
        public double Price;
        public double Impulse;
    }
}
