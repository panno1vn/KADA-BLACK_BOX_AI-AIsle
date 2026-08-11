using System;

namespace AIsle.Simulation.Population
{
    [Serializable]
    public sealed class ValidationResult
    {
        public bool Valid;
        public string[] Warnings = Array.Empty<string>();
        public string[] Errors = Array.Empty<string>();
    }
}
