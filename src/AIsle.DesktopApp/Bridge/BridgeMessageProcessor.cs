using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AIsle.Contracts.Project;
using AIsle.Contracts.Population;
using AIsle.Contracts.Simulation;
using AIsle.DesktopApp.Application;
using AIsle.DesktopApp.Infrastructure;
using AIsle.Simulation.Results;

namespace AIsle.DesktopApp.Bridge
{
    public sealed class BridgeMessageProcessor : IDisposable
    {
        private readonly ProjectApplicationService? _projects;
        private readonly string? _defaultProjectPath;
        private readonly PopulationApplicationService _populations;
        private readonly SimulationApplicationService _simulations;
        private readonly IHistoryStore _history;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };

        public BridgeMessageProcessor(
            ProjectApplicationService? projects = null,
            string? defaultProjectPath = null,
            PopulationApplicationService? populations = null,
            SimulationApplicationService? simulations = null,
            IHistoryStore? history = null)
        {
            _projects = projects;
            _defaultProjectPath = defaultProjectPath;
            _populations = populations ?? new PopulationApplicationService();
            _simulations = simulations ?? new SimulationApplicationService();
            _history = history ?? new JsonHistoryStore();
        }

        public string Process(string? messageJson) => ProcessAsync(messageJson).GetAwaiter().GetResult();

        public void Dispose() => _simulations.Dispose();

        public async Task<string> ProcessAsync(string? messageJson, CancellationToken cancellationToken = default)
        {
            string? requestId = null;

            try
            {
                using var document = JsonDocument.Parse(messageJson ?? string.Empty);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return Error(null, "invalid_request", "Bridge request must be a JSON object.");
                }

                requestId = ReadRequiredString(root, "requestId");
                var type = ReadRequiredString(root, "type");
                if (!root.TryGetProperty("payload", out var payload))
                {
                    return Error(requestId, "invalid_request", "Bridge request must contain payload.");
                }

                return type switch
                {
                    "app.ping" => Success(requestId, new { status = "ready", application = "AIsleDesktop" }),
                    "project.load" => await LoadProjectAsync(requestId, payload, cancellationToken),
                    "project.save" => await SaveProjectAsync(requestId, payload, cancellationToken),
                    "population.generate" => GeneratePopulation(requestId, payload),
                    "simulation.start" => StartSimulation(requestId, payload),
                    "simulation.pause" => SimulationCommand(requestId, _simulations.Pause),
                    "simulation.step" => SimulationCommand(requestId, _simulations.Step),
                    "simulation.reset" => SimulationCommand(requestId, _simulations.Reset),
                    "simulation.snapshot" => SimulationSnapshot(requestId),
                    "simulation.speed" => SetSimulationSpeed(requestId, payload),
                    "simulation.result" => SimulationResult(requestId, payload),
                    "history.save" => SaveHistory(requestId, payload),
                    "history.list" => Success(requestId, _history.List()),
                    "history.trash.list" => Success(requestId, _history.ListTrash()),
                    "history.read" => ReadHistory(requestId, payload),
                    "history.delete" => DeleteHistory(requestId, payload),
                    "history.clear" => ClearHistory(requestId),
                    "history.restore" => RestoreHistory(requestId, payload),
                    "history.restore.all" => Success(requestId, new { count = _history.RestoreAll(), restored = true }),
                    "replay.project" => ProjectReplay(requestId, payload),
                    "kpi.project" => ProjectKpis(requestId, payload),
                    "compare.results" => CompareResults(requestId, payload),
                    _ => Error(requestId, "unsupported_request", $"Unsupported bridge request type: {type}")
                };
            }
            catch (JsonException)
            {
                return Error(requestId, "invalid_json", "Bridge request is not valid JSON.");
            }
            catch (BridgeRequestException exception)
            {
                return Error(requestId, "invalid_request", exception.Message);
            }
            catch (InvalidProjectSchemaException exception)
            {
                return Error(requestId, "invalid_schema", exception.Message);
            }
            catch (DuplicateHistoryIdException exception)
            {
                return Error(requestId, "duplicate_history_id", exception.Message);
            }
            catch (HistoryResultNotFoundException exception)
            {
                return Error(requestId, "history_not_found", exception.Message);
            }
            catch (CorruptedHistoryException exception)
            {
                return Error(requestId, "corrupted_history", exception.Message);
            }
            catch (Exception)
            {
                return Error(requestId, "internal_error", "Bridge request could not be processed.");
            }
        }

        private string GeneratePopulation(string requestId, JsonElement payload)
        {
            if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty("config", out var configJson))
            {
                throw new BridgeRequestException("population.generate payload must contain config.");
            }

            var config = JsonSerializer.Deserialize<PopulationConfig>(configJson.GetRawText(), JsonOptions)
                ?? throw new BridgeRequestException("population.generate config is invalid.");
            return Success(requestId, _populations.Generate(config));
        }

        private string StartSimulation(string requestId, JsonElement payload)
        {
            SimulationStartInput? input = null;
            if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("input", out var inputJson))
            {
                input = JsonSerializer.Deserialize<SimulationStartInput>(inputJson.GetRawText(), JsonOptions)
                    ?? throw new BridgeRequestException("simulation.start input is invalid.");
            }

            return SimulationCommand(requestId, () => _simulations.Start(input));
        }

        private static string SimulationCommand(string requestId, Func<SimulationStateProjection> command)
        {
            try
            {
                return Success(requestId, command());
            }
            catch (ArgumentException exception)
            {
                throw new BridgeRequestException(exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                throw new BridgeRequestException(exception.Message);
            }
        }

        private string SimulationSnapshot(string requestId)
        {
            try { return Success(requestId, _simulations.Snapshot()); }
            catch (InvalidOperationException exception) { throw new BridgeRequestException(exception.Message); }
        }

        private string SetSimulationSpeed(string requestId, JsonElement payload)
        {
            if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty("multiplier", out var multiplierJson)
                || multiplierJson.ValueKind != JsonValueKind.Number || !multiplierJson.TryGetDouble(out var multiplier))
                throw new BridgeRequestException("simulation.speed payload must contain a numeric multiplier.");
            try { return Success(requestId, _simulations.SetSpeed(multiplier)); }
            catch (ArgumentException exception) { throw new BridgeRequestException(exception.Message); }
            catch (InvalidOperationException exception) { throw new BridgeRequestException(exception.Message); }
        }

        private string SimulationResult(string requestId, JsonElement payload)
        {
            string? name = null;
            if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("name", out var nameJson)
                && nameJson.ValueKind == JsonValueKind.String) name = nameJson.GetString();
            try { return Success(requestId, _simulations.Result(name)); }
            catch (InvalidOperationException exception) { throw new BridgeRequestException(exception.Message); }
        }

        private string SaveHistory(string requestId, JsonElement payload)
        {
            if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty("result", out var resultJson))
                throw new BridgeRequestException("history.save payload must contain result.");
            SimResult result;
            try { result = SimResultJsonSerializer.Deserialize(resultJson.GetRawText()); }
            catch (JsonException exception) { throw new BridgeRequestException("history.save result is invalid: " + exception.Message); }
            return Success(requestId, _history.Save(result));
        }

        private string ReadHistory(string requestId, JsonElement payload)
        {
            var id = ReadPayloadId(payload, "history.read");
            return Success(requestId, _history.Read(id));
        }

        private string DeleteHistory(string requestId, JsonElement payload)
        {
            var id = ReadPayloadId(payload, "history.delete");
            var deleted = _history.Delete(id);
            return Success(requestId, new { id, deleted });
        }

        private string ClearHistory(string requestId)
        {
            var count = _history.Clear();
            return Success(requestId, new { count, cleared = true });
        }

        private string RestoreHistory(string requestId, JsonElement payload)
        {
            var id = ReadPayloadId(payload, "history.restore");
            var restored = _history.Restore(id);
            return Success(requestId, new { id, restored });
        }

        private string ProjectReplay(string requestId, JsonElement payload)
        {
            var id = ReadPayloadId(payload, "replay.project");
            try { return Success(requestId, ReplayProjector.Project(_history.Read(id))); }
            catch (ArgumentException exception) { throw new BridgeRequestException("Stored replay is invalid: " + exception.Message); }
        }

        private string ProjectKpis(string requestId, JsonElement payload)
        {
            var id = ReadPayloadId(payload, "kpi.project");
            return Success(requestId, KpiProjector.Project(_history.Read(id)));
        }

        private string CompareResults(string requestId, JsonElement payload)
        {
            if (payload.ValueKind != JsonValueKind.Object) throw new BridgeRequestException("compare.results payload must be an object.");
            var runAId = ReadRequiredPayloadString(payload, "runAId", "compare.results");
            var runBId = ReadRequiredPayloadString(payload, "runBId", "compare.results");
            return Success(requestId, ResultComparer.Compare(_history.Read(runAId), _history.Read(runBId)));
        }

        private static string ReadPayloadId(JsonElement payload, string command)
        {
            if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty("id", out var idJson)
                || idJson.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(idJson.GetString()))
                throw new BridgeRequestException(command + " payload must contain a non-empty id.");
            return idJson.GetString()!;
        }

        private static string ReadRequiredPayloadString(JsonElement payload, string propertyName, string command)
        {
            if (!payload.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(value.GetString()))
                throw new BridgeRequestException(command + " payload must contain a non-empty " + propertyName + ".");
            return value.GetString()!;
        }

        private async Task<string> LoadProjectAsync(string requestId, JsonElement payload, CancellationToken cancellationToken)
        {
            var projects = RequireProjectService();
            var path = ReadPath(payload);
            var result = await projects.LoadAsync(path, cancellationToken);
            return result.Ok
                ? Success(requestId, new { result.Path, result.Project, result.Validation })
                : Error(requestId, result.Error!.Code, result.Error.Message);
        }

        private async Task<string> SaveProjectAsync(string requestId, JsonElement payload, CancellationToken cancellationToken)
        {
            var projects = RequireProjectService();
            var path = ReadPath(payload);
            if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty("project", out var projectJson))
            {
                throw new BridgeRequestException("project.save payload must contain project.");
            }

            var project = ProjectJsonSerializer.Deserialize(projectJson.GetRawText());
            var result = await projects.SaveAsync(path, project, cancellationToken);
            return result.Ok
                ? Success(requestId, new { result.Path, result.Project, result.Validation })
                : Error(requestId, result.Error!.Code, result.Error.Message);
        }

        private ProjectApplicationService RequireProjectService() =>
            _projects ?? throw new BridgeRequestException("Project application service is not configured.");

        private string ReadPath(JsonElement payload)
        {
            if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("path", out var pathElement) && pathElement.ValueKind == JsonValueKind.String)
            {
                var requestedPath = pathElement.GetString();
                if (!string.IsNullOrWhiteSpace(requestedPath)) return requestedPath;
            }

            if (!string.IsNullOrWhiteSpace(_defaultProjectPath)) return _defaultProjectPath;
            throw new BridgeRequestException("Project path is required.");
        }

        private static string ReadRequiredString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            {
                throw new BridgeRequestException($"Bridge request must contain a non-empty {propertyName}.");
            }

            return value.GetString()!;
        }

        private static string Success(string requestId, object payload) => JsonSerializer.Serialize(new
        {
            requestId,
            ok = true,
            payload,
            error = (object?)null
        }, JsonOptions);

        private static string Error(string? requestId, string code, string message) => JsonSerializer.Serialize(new
        {
            requestId,
            ok = false,
            payload = (object?)null,
            error = new { code, message }
        }, JsonOptions);

        private sealed class BridgeRequestException : Exception
        {
            public BridgeRequestException(string message) : base(message) { }
        }
    }
}
