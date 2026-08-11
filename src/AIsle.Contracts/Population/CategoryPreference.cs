using System;

namespace AIsle.Contracts.Population
{
    [Serializable]
    public sealed class CategoryPreference
    {
        public string CategoryId = string.Empty;
        public double Weight;

        public CategoryPreference()
        {
        }

        public CategoryPreference(string categoryId, double weight)
        {
            CategoryId = categoryId ?? string.Empty;
            Weight = weight;
        }
    }
}
