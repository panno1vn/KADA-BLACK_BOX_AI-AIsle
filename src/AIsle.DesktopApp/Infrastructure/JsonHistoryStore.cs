using System;
using System.Collections.Generic;
using System.IO;
using AIsle.Contracts.Simulation;
using AIsle.DesktopApp.Application;

namespace AIsle.DesktopApp.Infrastructure
{
    public sealed class JsonHistoryStore : IHistoryStore
    {
        private const string Extension = ".sim-result.json";
        private readonly string _directory;

        public JsonHistoryStore(string? directory = null)
        {
            _directory = directory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AIsle",
                "history-v1");
        }

        public HistoryEntry Save(SimResult result)
        {
            var json = SimResultJsonSerializer.Serialize(result);
            Directory.CreateDirectory(_directory);
            var path = PathFor(result.Id);
            if (File.Exists(path)) throw new DuplicateHistoryIdException(result.Id);

            var temporaryPath = Path.Combine(_directory, "." + result.Id + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(temporaryPath, json);
                try
                {
                    File.Move(temporaryPath, path, false);
                }
                catch (IOException) when (File.Exists(path))
                {
                    throw new DuplicateHistoryIdException(result.Id);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }

            return ToEntry(result);
        }

        public HistoryListResult List()
        {
            if (!Directory.Exists(_directory)) return new HistoryListResult();
            var entries = new List<HistoryEntry>();
            var warnings = new List<HistoryWarning>();
            var files = Directory.GetFiles(_directory, "*" + Extension, SearchOption.TopDirectoryOnly);
            for (var index = 0; index < files.Length; index++)
            {
                try
                {
                    entries.Add(ToEntry(ReadFile(files[index])));
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is System.Text.Json.JsonException)
                {
                    warnings.Add(new HistoryWarning
                    {
                        FileName = Path.GetFileName(files[index]),
                        Code = "corrupted_history",
                        Message = "The history item could not be read."
                    });
                }
            }

            entries.Sort((left, right) => right.CreatedAt.CompareTo(left.CreatedAt));
            warnings.Sort((left, right) => string.CompareOrdinal(left.FileName, right.FileName));
            return new HistoryListResult { Items = entries.ToArray(), Warnings = warnings.ToArray() };
        }

        public SimResult Read(string id)
        {
            if (!SimResultJsonSerializer.IsSafeId(id)) throw new ArgumentException("History ID is invalid.", nameof(id));
            var path = PathFor(id);
            if (!File.Exists(path)) throw new HistoryResultNotFoundException(id);
            try
            {
                return ReadFile(path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is System.Text.Json.JsonException)
            {
                throw new CorruptedHistoryException(Path.GetFileName(path), exception);
            }
        }

        private SimResult ReadFile(string path) => SimResultJsonSerializer.Deserialize(File.ReadAllText(path));
        private string PathFor(string id) => Path.Combine(_directory, id + Extension);

        private static HistoryEntry ToEntry(SimResult result) => new HistoryEntry
        {
            Id = result.Id,
            CreatedAt = result.CreatedAt,
            Name = result.Name,
            Summary = result.Summary
        };
    }
}
