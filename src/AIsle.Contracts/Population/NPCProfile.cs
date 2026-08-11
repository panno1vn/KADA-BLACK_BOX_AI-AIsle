using System;

namespace AIsle.Contracts.Population
{
    [Serializable]
    public sealed class NPCProfile
    {
        public string Id = string.Empty;
        public double WalkingSpeed;
        public double Patience;
        public double Exploration;
        public double Sociability;
        public double Impulsiveness;
        public double CrowdTolerance;
        public double PriceSensitivity;
        public string TargetCategory = string.Empty;
        public double InitialNeed = 0.6;
        public double NeedGrowthPerMinute = 0.015;
        public double InitialExplorationNeed = 0.4;
        public double ExplorationGrowthPerMinute = 0.01;
        public double AffectAttractor = 0.2;
        public double AffectStability = 0.6;
        public double AffectDispersion = 0.4;
        public double AffectRecovery = 0.15;
        public double DwellSeconds = 10.0;
        public CategoryPreference[] CategoryPreferences = Array.Empty<CategoryPreference>();
        public ShoppingMission ShoppingMission;

        public NPCProfile Copy()
        {
            var preferences = new CategoryPreference[CategoryPreferences == null ? 0 : CategoryPreferences.Length];
            for (var index = 0; index < preferences.Length; index++)
            {
                var source = CategoryPreferences[index];
                preferences[index] = source == null
                    ? new CategoryPreference()
                    : new CategoryPreference(source.CategoryId, source.Weight);
            }

            return new NPCProfile
            {
                Id = Id,
                WalkingSpeed = WalkingSpeed,
                Patience = Patience,
                Exploration = Exploration,
                Sociability = Sociability,
                Impulsiveness = Impulsiveness,
                CrowdTolerance = CrowdTolerance,
                PriceSensitivity = PriceSensitivity,
                TargetCategory = TargetCategory,
                InitialNeed = InitialNeed,
                NeedGrowthPerMinute = NeedGrowthPerMinute,
                InitialExplorationNeed = InitialExplorationNeed,
                ExplorationGrowthPerMinute = ExplorationGrowthPerMinute,
                AffectAttractor = AffectAttractor,
                AffectStability = AffectStability,
                AffectDispersion = AffectDispersion,
                AffectRecovery = AffectRecovery,
                DwellSeconds = DwellSeconds,
                CategoryPreferences = preferences,
                ShoppingMission = ShoppingMission
            };
        }
    }
}
