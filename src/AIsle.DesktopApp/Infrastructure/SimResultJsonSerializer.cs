using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIsle.Contracts.Simulation;

namespace AIsle.DesktopApp.Infrastructure
{
    public static class SimResultJsonSerializer
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true
        };

        public static string Serialize(SimResult result)
        {
            Validate(result);
            return JsonSerializer.Serialize(result, Options);
        }

        public static SimResult Deserialize(string json)
        {
            var result = JsonSerializer.Deserialize<SimResult>(json, Options)
                ?? throw new JsonException("SimResult payload is null.");
            Validate(result);
            return result;
        }

        public static void Validate(SimResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (!string.Equals(result.SchemaVersion, SimulationSchemas.SimResultV1, StringComparison.Ordinal))
                throw new JsonException("Unsupported SimResult schemaVersion.");
            if (!IsSafeId(result.Id)) throw new JsonException("SimResult ID may contain only letters, numbers, underscore and hyphen.");
            if (result.CreatedAt == default) throw new JsonException("SimResult createdAt is required.");
            if (result.Summary == null || result.Events == null || result.Purchases == null || result.Replay == null || result.Replay.Agents == null)
                throw new JsonException("SimResult summary, events, purchases and replay are required.");
        }

        public static bool IsSafeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            for (var index = 0; index < id.Length; index++)
            {
                var value = id[index];
                if (!char.IsAsciiLetterOrDigit(value) && value != '_' && value != '-') return false;
            }
            return true;
        }
    }
}
