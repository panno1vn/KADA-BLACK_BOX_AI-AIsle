using System;

namespace AIsle.Contracts.Population
{
    [Serializable]
    public sealed class ParameterRange
    {
        public double Min;
        public double Max = 1.0;

        public ParameterRange()
        {
        }

        public ParameterRange(double min, double max)
        {
            Min = min;
            Max = max;
        }

        public bool IsValid()
        {
            return !double.IsNaN(Min) && !double.IsInfinity(Min)
                && !double.IsNaN(Max) && !double.IsInfinity(Max)
                && Min <= Max;
        }

        public double Clamp(double value)
        {
            return Math.Max(Min, Math.Min(Max, value));
        }
    }
}
