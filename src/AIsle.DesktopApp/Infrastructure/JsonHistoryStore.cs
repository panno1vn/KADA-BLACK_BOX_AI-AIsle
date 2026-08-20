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
        private readonly string _trashDirectory;

        public JsonHistoryStore(string? directory = null)
        {
            _directory = directory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AIsle",
                "history-v1");
            _trashDirectory = Path.Combine(_directory, ".trash");
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

        public HistoryListResult List() => ListFromDirectory(_directory);

        public HistoryListResult ListTrash() => ListFromDirectory(_trashDirectory);

        private HistoryListResult ListFromDirectory(string targetDir)
        {
            if (!Directory.Exists(targetDir)) return new HistoryListResult();
            var entries = new List<HistoryEntry>();
            var warnings = new List<HistoryWarning>();
            var files = Directory.GetFiles(targetDir, "*" + Extension, SearchOption.TopDirectoryOnly);
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
            if (!File.Exists(path))
            {
                // Fallback check in trash if not in active directory
                var trashPath = TrashPathFor(id);
                if (File.Exists(trashPath)) path = trashPath;
                else throw new HistoryResultNotFoundException(id);
            }
            try
            {
                return ReadFile(path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is System.Text.Json.JsonException)
            {
                throw new CorruptedHistoryException(Path.GetFileName(path), exception);
            }
        }

        public bool Delete(string id)
        {
            if (!SimResultJsonSerializer.IsSafeId(id)) return false;
            var path = PathFor(id);
            if (!File.Exists(path)) return false;
            try
            {
                Directory.CreateDirectory(_trashDirectory);
                var trashPath = TrashPathFor(id);
                if (File.Exists(trashPath)) File.Delete(trashPath);
                File.Move(path, trashPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public int Clear()
        {
            if (!Directory.Exists(_directory)) return 0;
            Directory.CreateDirectory(_trashDirectory);
            var count = 0;
            var files = Directory.GetFiles(_directory, "*" + Extension, SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    var dest = Path.Combine(_trashDirectory, Path.GetFileName(file));
                    if (File.Exists(dest)) File.Delete(dest);
                    File.Move(file, dest);
                    count++;
                }
                catch { }
            }
            return count;
        }

        public bool Restore(string id)
        {
            if (!SimResultJsonSerializer.IsSafeId(id)) return false;
            var trashPath = TrashPathFor(id);
            if (!File.Exists(trashPath)) return false;
            try
            {
                Directory.CreateDirectory(_directory);
                var activePath = PathFor(id);
                if (File.Exists(activePath)) File.Delete(activePath);
                File.Move(trashPath, activePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public int RestoreAll()
        {
            if (!Directory.Exists(_trashDirectory)) return 0;
            Directory.CreateDirectory(_directory);
            var count = 0;
            var files = Directory.GetFiles(_trashDirectory, "*" + Extension, SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    var dest = Path.Combine(_directory, Path.GetFileName(file));
                    if (File.Exists(dest)) File.Delete(dest);
                    File.Move(file, dest);
                    count++;
                }
                catch { }
            }
            return count;
        }

        private SimResult ReadFile(string path) => SimResultJsonSerializer.Deserialize(File.ReadAllText(path));
        private string PathFor(string id) => Path.Combine(_directory, id + Extension);
        private string TrashPathFor(string id) => Path.Combine(_trashDirectory, id + Extension);

        private static HistoryEntry ToEntry(SimResult result) => new HistoryEntry
        {
            Id = result.Id,
            CreatedAt = result.CreatedAt,
            Name = result.Name,
            Summary = result.Summary
        };
    }
}
