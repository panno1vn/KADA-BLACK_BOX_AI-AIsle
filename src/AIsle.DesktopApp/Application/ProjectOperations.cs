using System;
using System.Threading;
using System.Threading.Tasks;
using AIsle.Contracts.Project;

namespace AIsle.DesktopApp.Application
{
    public interface IProjectRepository
    {
        Task<ProjectDocument> LoadAsync(string path, CancellationToken cancellationToken = default);
        Task SaveAsync(string path, ProjectDocument project, CancellationToken cancellationToken = default);
    }

    public sealed class ProjectApplicationError
    {
        public string Code { get; }
        public string Message { get; }

        public ProjectApplicationError(string code, string message)
        {
            Code = code;
            Message = message;
        }
    }

    public sealed class ProjectOperationResult
    {
        public bool Ok { get; }
        public string Path { get; }
        public ProjectDocument? Project { get; }
        public LayoutValidationResult? Validation { get; }
        public ProjectApplicationError? Error { get; }

        private ProjectOperationResult(bool ok, string path, ProjectDocument? project, LayoutValidationResult? validation, ProjectApplicationError? error)
        {
            Ok = ok;
            Path = path;
            Project = project;
            Validation = validation;
            Error = error;
        }

        public static ProjectOperationResult Success(string path, ProjectDocument project, LayoutValidationResult validation) =>
            new ProjectOperationResult(true, path, project, validation, null);

        public static ProjectOperationResult Failure(string path, string code, string message) =>
            new ProjectOperationResult(false, path, null, null, new ProjectApplicationError(code, message));
    }

    public sealed class ProjectNotFoundException : Exception
    {
        public ProjectNotFoundException(string path) : base($"Project file was not found: {path}") { }
    }

    public sealed class MalformedProjectJsonException : Exception
    {
        public MalformedProjectJsonException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    public sealed class InvalidProjectSchemaException : Exception
    {
        public InvalidProjectSchemaException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    public sealed class ProjectPersistenceException : Exception
    {
        public ProjectPersistenceException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    public sealed class ProjectApplicationService
    {
        private readonly IProjectRepository _repository;
        private readonly LayoutValidator _validator;

        public ProjectApplicationService(IProjectRepository repository, LayoutValidator validator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<ProjectOperationResult> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return ProjectOperationResult.Failure(string.Empty, "invalid_path", "Project path is required.");
            }

            try
            {
                var project = await _repository.LoadAsync(path, cancellationToken);
                var validation = _validator.ValidateProject(project);
                return validation.IsValid
                    ? ProjectOperationResult.Success(path, project, validation)
                    : ProjectOperationResult.Failure(path, "invalid_layout", string.Join(" ", validation.Errors));
            }
            catch (ProjectNotFoundException exception)
            {
                return ProjectOperationResult.Failure(path, "project_not_found", exception.Message);
            }
            catch (MalformedProjectJsonException exception)
            {
                return ProjectOperationResult.Failure(path, "malformed_json", exception.Message);
            }
            catch (InvalidProjectSchemaException exception)
            {
                return ProjectOperationResult.Failure(path, "invalid_schema", exception.Message);
            }
            catch (ProjectPersistenceException exception)
            {
                return ProjectOperationResult.Failure(path, "project_io_error", exception.Message);
            }
        }

        public async Task<ProjectOperationResult> SaveAsync(string path, ProjectDocument project, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return ProjectOperationResult.Failure(string.Empty, "invalid_path", "Project path is required.");
            }

            var validation = _validator.ValidateProject(project);
            if (!validation.IsValid)
            {
                return ProjectOperationResult.Failure(path, "invalid_layout", string.Join(" ", validation.Errors));
            }

            try
            {
                await _repository.SaveAsync(path, project, cancellationToken);
                var reloaded = await _repository.LoadAsync(path, cancellationToken);
                var reloadedValidation = _validator.ValidateProject(reloaded);
                return reloadedValidation.IsValid
                    ? ProjectOperationResult.Success(path, reloaded, reloadedValidation)
                    : ProjectOperationResult.Failure(path, "round_trip_failed", string.Join(" ", reloadedValidation.Errors));
            }
            catch (InvalidProjectSchemaException exception)
            {
                return ProjectOperationResult.Failure(path, "invalid_schema", exception.Message);
            }
            catch (Exception exception) when (exception is ProjectPersistenceException or ProjectNotFoundException or MalformedProjectJsonException)
            {
                return ProjectOperationResult.Failure(path, "project_io_error", exception.Message);
            }
        }
    }
}
