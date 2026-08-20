using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AIsle.Contracts.Project;
using AIsle.DesktopApp.Application;

namespace AIsle.DesktopApp.Infrastructure
{
    public static class ProjectJsonSerializer
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true
        };

        public static ProjectDocument Deserialize(string json)
        {
            try
            {
                using var _ = JsonDocument.Parse(json);
            }
            catch (JsonException exception)
            {
                throw new MalformedProjectJsonException("Project file contains malformed JSON.", exception);
            }

            ProjectDocument? project;
            try
            {
                project = JsonSerializer.Deserialize<ProjectDocument>(json, Options);
            }
            catch (JsonException exception)
            {
                throw new InvalidProjectSchemaException("Project JSON does not match the aisle.project.v1 contract.", exception);
            }

            EnsureSchema(project);
            return project!;
        }

        public static string Serialize(ProjectDocument project)
        {
            EnsureSchema(project);
            try
            {
                return JsonSerializer.Serialize(project, Options);
            }
            catch (Exception exception) when (exception is JsonException or NotSupportedException)
            {
                throw new InvalidProjectSchemaException("Project could not be serialized with the aisle.project.v1 contract.", exception);
            }
        }

        private static void EnsureSchema(ProjectDocument? project)
        {
            if (project == null)
            {
                throw new InvalidProjectSchemaException("Project document must be a JSON object.");
            }

            if (!string.Equals(project.SchemaVersion, ProjectSchema.Version, StringComparison.Ordinal))
            {
                throw new InvalidProjectSchemaException($"Unsupported project schemaVersion. Expected '{ProjectSchema.Version}'.");
            }

            if (project.Layout == null || project.Layout.Walls == null || project.Layout.Shelves == null || project.Catalog == null)
            {
                throw new InvalidProjectSchemaException("Project requires layout, walls, shelves, and catalog members.");
            }
        }
    }

    public sealed class JsonProjectRepository : IProjectRepository
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public async Task<ProjectDocument> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            var fullPath = NormalizePath(path);
            string json;

            try
            {
                json = await File.ReadAllTextAsync(fullPath, Utf8WithoutBom, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                throw new ProjectNotFoundException(fullPath);
            }
            catch (DirectoryNotFoundException)
            {
                throw new ProjectNotFoundException(fullPath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new ProjectPersistenceException($"Project file could not be read: {fullPath}", exception);
            }

            return ProjectJsonSerializer.Deserialize(json);
        }

        public async Task SaveAsync(string path, ProjectDocument project, CancellationToken cancellationToken = default)
        {
            var fullPath = NormalizePath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new ProjectPersistenceException("Project path must include a directory.");
            }

            var json = ProjectJsonSerializer.Serialize(project);
            var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

            try
            {
                Directory.CreateDirectory(directory);
                await File.WriteAllTextAsync(temporaryPath, json, Utf8WithoutBom, cancellationToken);
                File.Move(temporaryPath, fullPath, true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new ProjectPersistenceException($"Project file could not be saved: {fullPath}", exception);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ProjectPersistenceException("Project path is required.");
            }

            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw new ProjectPersistenceException("Project path is invalid.", exception);
            }
        }
    }

    public static class DefaultProjectLocation
    {
        public static string Ensure(string packagedDefaultProjectPath)
        {
            if (!File.Exists(packagedDefaultProjectPath))
            {
                throw new FileNotFoundException("Packaged default project is missing.", packagedDefaultProjectPath);
            }

            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AIsle");
            var projectPath = Path.Combine(directory, "project-v1.json");
            if (File.Exists(projectPath)) return projectPath;

            Directory.CreateDirectory(directory);
            File.Copy(packagedDefaultProjectPath, projectPath, false);
            return projectPath;
        }
    }
}
