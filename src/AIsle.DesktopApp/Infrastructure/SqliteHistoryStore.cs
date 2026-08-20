using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AIsle.Contracts.Simulation;
using AIsle.DesktopApp.Application;
using Microsoft.Data.Sqlite;

namespace AIsle.DesktopApp.Infrastructure
{
    public sealed class SqliteHistoryStore : IHistoryStore, IDisposable
    {
        private const string LegacyExtension = ".sim-result.json";
        private readonly string _directory;
        private readonly SqliteConnection _connection;
        private readonly object _sync = new object();

        public SqliteHistoryStore(string? directory = null, string? seedDirectory = null)
        {
            _directory = directory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AIsle",
                "history-v1");
            Directory.CreateDirectory(_directory);

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(_directory, "history.db"),
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString();
            _connection = new SqliteConnection(connectionString);
            _connection.Open();

            using (var pragma = _connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
                pragma.ExecuteNonQuery();
            }

            EnsureSchema();
            MigrateLegacyJsonFiles();
            if (!string.IsNullOrWhiteSpace(seedDirectory)) ImportDirectory(seedDirectory, isTrashed: false);
        }

        public void Dispose() => _connection.Dispose();

        private void EnsureSchema()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
CREATE TABLE IF NOT EXISTS history (
    id TEXT PRIMARY KEY,
    created_at TEXT NOT NULL,
    name TEXT NOT NULL,
    is_trashed INTEGER NOT NULL DEFAULT 0,
    duration_seconds REAL NOT NULL DEFAULT 0,
    revenue REAL NOT NULL DEFAULT 0,
    purchases INTEGER NOT NULL DEFAULT 0,
    spawned INTEGER NOT NULL DEFAULT 0,
    converted INTEGER NOT NULL DEFAULT 0,
    main_buyers INTEGER NOT NULL DEFAULT 0,
    impulse_buyers INTEGER NOT NULL DEFAULT 0,
    not_found INTEGER NOT NULL DEFAULT 0,
    unreachable INTEGER NOT NULL DEFAULT 0,
    stuck_recoveries INTEGER NOT NULL DEFAULT 0,
    completed INTEGER NOT NULL DEFAULT 0,
    result_json TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_history_trashed_created ON history (is_trashed, created_at DESC);";
            command.ExecuteNonQuery();
        }

        // One-time-per-file import of any pre-existing JsonHistoryStore (.sim-result.json) data so
        // upgrading from the JSON store to SQLite does not lose runs already saved on disk. Source
        // files are left untouched (INSERT OR IGNORE keeps this idempotent across restarts).
        private void MigrateLegacyJsonFiles()
        {
            ImportDirectory(_directory, isTrashed: false);
            ImportDirectory(Path.Combine(_directory, ".trash"), isTrashed: true);
        }

        private void ImportDirectory(string sourceDirectory, bool isTrashed)
        {
            if (!Directory.Exists(sourceDirectory)) return;
            foreach (var file in Directory.GetFiles(sourceDirectory, "*" + LegacyExtension, SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var result = SimResultJsonSerializer.Deserialize(File.ReadAllText(file));
                    InsertOrIgnore(result, isTrashed);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is System.Text.Json.JsonException)
                {
                    // Corrupted legacy file: skip it, matching JsonHistoryStore's tolerant listing behavior.
                }
            }
        }

        private void InsertOrIgnore(SimResult result, bool isTrashed)
        {
            var json = SimResultJsonSerializer.Serialize(result);
            using var command = _connection.CreateCommand();
            command.CommandText = @"
INSERT OR IGNORE INTO history
    (id, created_at, name, is_trashed, duration_seconds, revenue, purchases, spawned, converted, main_buyers, impulse_buyers, not_found, unreachable, stuck_recoveries, completed, result_json)
VALUES
    ($id, $createdAt, $name, $isTrashed, $durationSeconds, $revenue, $purchases, $spawned, $converted, $mainBuyers, $impulseBuyers, $notFound, $unreachable, $stuckRecoveries, $completed, $resultJson);";
            BindSummary(command, result, json);
            command.Parameters.AddWithValue("$isTrashed", isTrashed ? 1 : 0);
            command.ExecuteNonQuery();
        }

        public HistoryEntry Save(SimResult result)
        {
            var json = SimResultJsonSerializer.Serialize(result);
            lock (_sync)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = @"
INSERT INTO history
    (id, created_at, name, is_trashed, duration_seconds, revenue, purchases, spawned, converted, main_buyers, impulse_buyers, not_found, unreachable, stuck_recoveries, completed, result_json)
VALUES
    ($id, $createdAt, $name, 0, $durationSeconds, $revenue, $purchases, $spawned, $converted, $mainBuyers, $impulseBuyers, $notFound, $unreachable, $stuckRecoveries, $completed, $resultJson);";
                BindSummary(command, result, json);
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (SqliteException exception) when (exception.SqliteErrorCode == 19 /* SQLITE_CONSTRAINT */)
                {
                    throw new DuplicateHistoryIdException(result.Id);
                }
            }

            return ToEntry(result);
        }

        private static void BindSummary(SqliteCommand command, SimResult result, string resultJson)
        {
            var summary = result.Summary;
            command.Parameters.AddWithValue("$id", result.Id);
            command.Parameters.AddWithValue("$createdAt", result.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$name", result.Name ?? string.Empty);
            command.Parameters.AddWithValue("$durationSeconds", summary.DurationSeconds);
            command.Parameters.AddWithValue("$revenue", summary.Revenue);
            command.Parameters.AddWithValue("$purchases", summary.Purchases);
            command.Parameters.AddWithValue("$spawned", summary.Spawned);
            command.Parameters.AddWithValue("$converted", summary.Converted);
            command.Parameters.AddWithValue("$mainBuyers", summary.MainBuyers);
            command.Parameters.AddWithValue("$impulseBuyers", summary.ImpulseBuyers);
            command.Parameters.AddWithValue("$notFound", summary.NotFound);
            command.Parameters.AddWithValue("$unreachable", summary.Unreachable);
            command.Parameters.AddWithValue("$stuckRecoveries", summary.StuckRecoveries);
            command.Parameters.AddWithValue("$completed", summary.Completed ? 1 : 0);
            command.Parameters.AddWithValue("$resultJson", resultJson);
        }

        public HistoryListResult List() => Query(isTrashed: false);

        public HistoryListResult ListTrash() => Query(isTrashed: true);

        private HistoryListResult Query(bool isTrashed)
        {
            var entries = new List<HistoryEntry>();
            var warnings = new List<HistoryWarning>();
            lock (_sync)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = @"
SELECT id, created_at, name, duration_seconds, revenue, purchases, spawned, converted, main_buyers, impulse_buyers, not_found, unreachable, stuck_recoveries, completed
FROM history WHERE is_trashed = $isTrashed ORDER BY created_at DESC;";
                command.Parameters.AddWithValue("$isTrashed", isTrashed ? 1 : 0);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var id = reader.GetString(0);
                    try
                    {
                        entries.Add(new HistoryEntry
                        {
                            Id = id,
                            CreatedAt = DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                            Name = reader.GetString(2),
                            Summary = new SimulationSummary
                            {
                                DurationSeconds = reader.GetDouble(3),
                                Revenue = reader.GetDouble(4),
                                Purchases = reader.GetInt32(5),
                                Spawned = reader.GetInt32(6),
                                Converted = reader.GetInt32(7),
                                MainBuyers = reader.GetInt32(8),
                                ImpulseBuyers = reader.GetInt32(9),
                                NotFound = reader.GetInt32(10),
                                Unreachable = reader.GetInt32(11),
                                StuckRecoveries = reader.GetInt32(12),
                                Completed = reader.GetInt64(13) != 0
                            }
                        });
                    }
                    catch (Exception exception) when (exception is InvalidCastException || exception is FormatException)
                    {
                        warnings.Add(new HistoryWarning { FileName = id, Code = "corrupted_history", Message = "The history item could not be read." });
                    }
                }
            }

            entries.Sort((left, right) => right.CreatedAt.CompareTo(left.CreatedAt));
            warnings.Sort((left, right) => string.CompareOrdinal(left.FileName, right.FileName));
            return new HistoryListResult { Items = entries.ToArray(), Warnings = warnings.ToArray() };
        }

        public SimResult Read(string id)
        {
            if (!SimResultJsonSerializer.IsSafeId(id)) throw new ArgumentException("History ID is invalid.", nameof(id));
            string? json;
            lock (_sync)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = "SELECT result_json FROM history WHERE id = $id;";
                command.Parameters.AddWithValue("$id", id);
                json = command.ExecuteScalar() as string;
            }

            if (json == null) throw new HistoryResultNotFoundException(id);
            try
            {
                return SimResultJsonSerializer.Deserialize(json);
            }
            catch (System.Text.Json.JsonException exception)
            {
                throw new CorruptedHistoryException(id, exception);
            }
        }

        public bool Delete(string id)
        {
            if (!SimResultJsonSerializer.IsSafeId(id)) return false;
            lock (_sync)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = "UPDATE history SET is_trashed = 1 WHERE id = $id AND is_trashed = 0;";
                command.Parameters.AddWithValue("$id", id);
                return command.ExecuteNonQuery() > 0;
            }
        }

        public int Clear()
        {
            lock (_sync)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = "UPDATE history SET is_trashed = 1 WHERE is_trashed = 0;";
                return command.ExecuteNonQuery();
            }
        }

        public bool Restore(string id)
        {
            if (!SimResultJsonSerializer.IsSafeId(id)) return false;
            lock (_sync)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = "UPDATE history SET is_trashed = 0 WHERE id = $id AND is_trashed = 1;";
                command.Parameters.AddWithValue("$id", id);
                return command.ExecuteNonQuery() > 0;
            }
        }

        public int RestoreAll()
        {
            lock (_sync)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = "UPDATE history SET is_trashed = 0 WHERE is_trashed = 1;";
                return command.ExecuteNonQuery();
            }
        }

        private static HistoryEntry ToEntry(SimResult result) => new HistoryEntry
        {
            Id = result.Id,
            CreatedAt = result.CreatedAt,
            Name = result.Name,
            Summary = result.Summary
        };
    }
}
