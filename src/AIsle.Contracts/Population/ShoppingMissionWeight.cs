using System;

namespace AIsle.Contracts.Population
{
    [Serializable]
    public sealed class ShoppingMissionWeight
    {
        public ShoppingMission Mission;
        public double Weight = 1.0;
    }
}
