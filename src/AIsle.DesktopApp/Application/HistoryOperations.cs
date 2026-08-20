using System;
using AIsle.Contracts.Simulation;

namespace AIsle.DesktopApp.Application
{
    public interface IHistoryStore
    {
        HistoryEntry Save(SimResult result);
        HistoryListResult List();
        HistoryListResult ListTrash();
        SimResult Read(string id);
        bool Delete(string id);
        int Clear();
        bool Restore(string id);
        int RestoreAll();
    }

    public sealed class HistoryEntry
    {
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public string Name { get; set; } = string.Empty;
        public SimulationSummary Summary { get; set; } = new SimulationSummary();
    }

    public sealed class HistoryListResult
    {
        public HistoryEntry[] Items { get; set; } = Array.Empty<HistoryEntry>();
        public HistoryWarning[] Warnings { get; set; } = Array.Empty<HistoryWarning>();
    }

    public sealed class HistoryWarning
    {
        public string FileName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class DuplicateHistoryIdException : Exception
    {
        public DuplicateHistoryIdException(string id) : base("A history result with ID '" + id + "' already exists.") { }
    }

    public sealed class HistoryResultNotFoundException : Exception
    {
        public HistoryResultNotFoundException(string id) : base("History result '" + id + "' was not found.") { }
    }

    public sealed class CorruptedHistoryException : Exception
    {
        public CorruptedHistoryException(string fileName, Exception innerException)
            : base("History file '" + fileName + "' is corrupted or has an unsupported schema.", innerException) { }
    }
}
