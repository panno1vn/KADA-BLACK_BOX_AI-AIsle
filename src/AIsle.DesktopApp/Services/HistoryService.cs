using System;
using System.Collections.Generic;
using System.Linq;
using AIsle.Contracts.Simulation;
using AIsle.DesktopApp.Application;
using AIsle.DesktopApp.Infrastructure;

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
        private readonly IHistoryStore _store;

        public HistoryService(IHistoryStore? store = null)
        {
            _store = store ?? new SqliteHistoryStore();
        }

        public List<SimRunSummary> ListAll()
        {
            return _store.List().Items.Select(item => new SimRunSummary
            {
                Id = item.Id,
                SchemaVersion = SimulationSchemas.SimResultV1,
                CreatedAt = item.CreatedAt.ToString("O"),
                Name = item.Name,
                DurationMinutes = item.Summary.DurationSeconds / 60.0,
                Summary = new SimSummaryData
                {
                    Time = item.Summary.DurationSeconds,
                    Revenue = item.Summary.Revenue,
                    Purchases = item.Summary.Purchases,
                    ConversionRate = item.Summary.Spawned == 0 ? 0.0 : (double)item.Summary.Converted / item.Summary.Spawned,
                    NotFoundRate = item.Summary.Spawned == 0 ? 0.0 : (double)item.Summary.NotFound / item.Summary.Spawned,
                    Spawned = item.Summary.Spawned,
                    Active = 0
                }
            }).ToList();
        }

        public string? GetRunJson(string id)
        {
            try { return SimResultJsonSerializer.Serialize(_store.Read(id)); }
            catch (HistoryResultNotFoundException) { return null; }
            catch (CorruptedHistoryException) { return null; }
        }

        public void SaveRun(string id, string json)
        {
            var result = SimResultJsonSerializer.Deserialize(json);
            if (!string.Equals(result.Id, id, StringComparison.Ordinal)) throw new ArgumentException("History ID does not match result payload.", nameof(id));
            _store.Save(result);
        }
    }
}
