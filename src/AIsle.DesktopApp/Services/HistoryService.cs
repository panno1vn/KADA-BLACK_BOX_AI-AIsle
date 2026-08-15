using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AIsle.DesktopApp.Services
{
    public class SimRunSummary
    {
        public string Id { get; set; } = "";
        public string? SchemaVersion { get; set; }
        public string? CreatedAt { get; set; }
        public string? Name { get; set; }
        public int? Seed { get; set; }
        public double? DurationMinutes { get; set; }
        public SimSummaryData? Summary { get; set; }
    }

    public class SimSummaryData
    {
        public double Time { get; set; }
        public double Revenue { get; set; }
        public int Purchases { get; set; }
        public double ConversionRate { get; set; }
        public double NotFoundRate { get; set; }
        public int Spawned { get; set; }
        public int Active { get; set; }
    }

    public class HistoryService
    {
        private readonly string _historyDir;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public HistoryService()
        {
            _historyDir = FindHistoryDirectory();
        }

        public List<SimRunSummary> ListAll()
        {
            if (!Directory.Exists(_historyDir)) return new List<SimRunSummary>();
            var files = Directory.GetFiles(_historyDir, "*.json");
            var results = new List<SimRunSummary>();
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    var summary = new SimRunSummary
                    {
                        Id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : Path.GetFileNameWithoutExtension(file),
                        SchemaVersion = root.TryGetProperty("schemaVersion", out var sv) ? sv.GetString() : null,
                        CreatedAt = root.TryGetProperty("createdAt", out var ca) ? ca.GetString() : null,
                        Name = root.TryGetProperty("name", out var nm) ? nm.GetString() : null,
                    };
                    if (root.TryGetProperty("input", out var input))
                    {
                        if (input.TryGetProperty("seed", out var seedProp) && seedProp.TryGetInt32(out var seedVal))
                            summary.Seed = seedVal;
                        if (input.TryGetProperty("durationMinutes", out var durProp) && durProp.TryGetDouble(out var durVal))
                            summary.DurationMinutes = durVal;
                    }
                    if (root.TryGetProperty("summary", out var sumEl))
                    {
                        summary.Summary = JsonSerializer.Deserialize<SimSummaryData>(sumEl.GetRawText(), JsonOptions);
                    }
                    results.Add(summary);
                }
                catch { /* skip corrupt files */ }
            }
            return results.OrderByDescending(r => r.CreatedAt ?? "").ToList();
        }

        public string? GetRunJson(string id)
        {
            var path = Path.Combine(_historyDir, id + ".json");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public void SaveRun(string id, string json)
        {
            Directory.CreateDirectory(_historyDir);
            File.WriteAllText(Path.Combine(_historyDir, id + ".json"), json);
        }

        private static string FindHistoryDirectory()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            while (dir != null)
            {
                var candidate = Path.Combine(dir, "runtime", "history");
                if (Directory.Exists(candidate)) return candidate;
                if (File.Exists(Path.Combine(dir, "AGENTS.md")))
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return Path.Combine(Directory.GetCurrentDirectory(), "runtime", "history");
        }
    }
}
