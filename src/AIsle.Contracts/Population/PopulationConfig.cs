using System;

namespace AIsle.Contracts.Population
{
    [Serializable]
    public sealed class PopulationConfig
    {
        public int Count = 200;
        public PopulationParameterRanges ParameterRanges = new PopulationParameterRanges();
        public PopulationDistributionTargets DistributionTargets = new PopulationDistributionTargets();
        public GeneratorSettings GeneratorSettings = new GeneratorSettings();
        public string[] CategoryIds = { "drinks", "snacks", "essentials" };
        public ShoppingMissionWeight[] ShoppingMissionWeights =
        {
            new ShoppingMissionWeight { Mission = ShoppingMission.Routine, Weight = 0.35 },
            new ShoppingMissionWeight { Mission = ShoppingMission.QuickTopUp, Weight = 0.25 },
            new ShoppingMissionWeight { Mission = ShoppingMission.PlannedBasket, Weight = 0.25 },
            new ShoppingMissionWeight { Mission = ShoppingMission.Exploration, Weight = 0.15 }
        };
    }
}
