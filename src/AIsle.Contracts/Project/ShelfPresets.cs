#nullable enable
using System;
using System.Collections.Generic;

namespace AIsle.Contracts.Project
{
    public sealed class ShelfPresetDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public static class ShelfPresets
    {
        public const string Standard = "standard";
        public const string Cooler = "cooler";
        public const string Endcap = "endcap";

        private static readonly Dictionary<string, ShelfPresetDefinition> Presets = new(StringComparer.OrdinalIgnoreCase)
        {
            [Standard] = new ShelfPresetDefinition { Id = Standard, Name = "Standard (2.0 x 1.0m)", Width = 2.0, Height = 1.0 },
            [Cooler] = new ShelfPresetDefinition { Id = Cooler, Name = "Cooler / Square (1.0 x 1.0m)", Width = 1.0, Height = 1.0 },
            [Endcap] = new ShelfPresetDefinition { Id = Endcap, Name = "Endcap / Slim (1.0 x 2.0m)", Width = 1.0, Height = 2.0 }
        };

        public static IReadOnlyCollection<ShelfPresetDefinition> All => Presets.Values;

        public static ShelfPresetDefinition GetPreset(string? presetId)
        {
            if (!string.IsNullOrEmpty(presetId) && Presets.TryGetValue(presetId, out var preset))
            {
                return preset;
            }
            return Presets[Standard];
        }

        public static bool TryGetPreset(string? presetId, out ShelfPresetDefinition preset)
        {
            if (!string.IsNullOrEmpty(presetId) && Presets.TryGetValue(presetId, out preset!))
            {
                return true;
            }
            preset = Presets[Standard];
            return false;
        }
    }
}
