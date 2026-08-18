using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AIsle.Contracts.Project;
using AIsle.DesktopApp.Application;
using AIsle.DesktopApp.Infrastructure;

namespace AIsle.DesktopApp.Bridge
{
    public sealed class BridgeMessageProcessor
    {
        private readonly ProjectApplicationService? _projects;
        private readonly string? _defaultProjectPath;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public BridgeMessageProcessor(ProjectApplicationService? projects = null, string? defaultProjectPath = null)
        {
            _projects = projects;
            _defaultProjectPath = defaultProjectPath;
        }

        public string Process(string? messageJson) => ProcessAsync(messageJson).GetAwaiter().GetResult();

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
            catch (Exception)
            {
                return Error(requestId, "internal_error", "Bridge request could not be processed.");
            }
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
