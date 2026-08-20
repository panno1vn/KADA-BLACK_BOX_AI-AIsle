using System;
using System.Collections.Generic;
using System.Linq;
using AIsle.Contracts.Project;
using AIsle.Contracts.Simulation;
using AIsle.Simulation.Runtime;

namespace AIsle.DesktopApp.Application
{
    public sealed class LayoutValidationResult
    {
        public bool IsValid => Errors.Length == 0;
        public string[] Errors { get; }
        public string[] Warnings { get; }
        public string[] UnreachableShelfIds { get; }

        public LayoutValidationResult(IEnumerable<string> errors, IEnumerable<string> warnings, IEnumerable<string> unreachableShelfIds)
        {
            Errors = errors.ToArray();
            Warnings = warnings.ToArray();
            UnreachableShelfIds = unreachableShelfIds.ToArray();
        }
    }

    public sealed class LayoutValidator
    {
        private const double GeometryEpsilon = 0.000001;

        public LayoutValidationResult ValidateProject(ProjectDocument? project)
        {
            if (project == null)
            {
                return Invalid("Project document is required.");
            }

            if (!string.Equals(project.SchemaVersion, ProjectSchema.Version, StringComparison.Ordinal))
            {
                return Invalid($"Project schemaVersion must be '{ProjectSchema.Version}'.");
            }

            if (project.Layout == null)
            {
                return Invalid("Project layout is required.");
            }

            return ValidateLayout(project.Layout);
        }

        public LayoutValidationResult ValidateLayout(ProjectLayout layout)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var unreachableShelfIds = new List<string>();

            if (!PositiveFinite(layout.Width) || !PositiveFinite(layout.Height))
            {
                errors.Add("Layout width and height must be finite values greater than zero.");
            }

            ValidateRequiredPoint("entrance", layout.Entrance, layout, errors);
            ValidateRequiredPoint("checkout", layout.Checkout, layout, errors);
            ValidateWalls(layout, errors);
            ValidateShelves(layout, errors);
            ValidateSpawnRateCurve(layout.SpawnRateCurve, errors);

            if (errors.Count == 0)
            {
                CheckReachability(layout, warnings, unreachableShelfIds, errors);
            }

            return new LayoutValidationResult(errors, warnings, unreachableShelfIds);
        }

        private static void ValidateRequiredPoint(string name, ProjectPoint? point, ProjectLayout layout, ICollection<string> errors)
        {
            if (point == null)
            {
                errors.Add($"Layout requires an {name} point.");
                return;
            }

            if (!Finite(point.X) || !Finite(point.Y))
            {
                errors.Add($"Layout {name} coordinates must be finite.");
                return;
            }

            if (PositiveFinite(layout.Width) && PositiveFinite(layout.Height) && !Inside(point.X, point.Y, layout))
            {
                errors.Add($"Layout {name} must be inside the layout bounds.");
            }
        }

        private static void ValidateWalls(ProjectLayout layout, ICollection<string> errors)
        {
            if (layout.Walls == null)
            {
                errors.Add("Layout must contain a walls array.");
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var wall in layout.Walls)
            {
                if (wall == null)
                {
                    errors.Add("Layout walls cannot contain null items.");
                    continue;
                }

                var id = wall.Id?.Trim();
                if (string.IsNullOrEmpty(id)) errors.Add("Every wall requires a non-empty id.");
                else if (!ids.Add(id)) errors.Add($"Wall id '{id}' is duplicated.");

                if (!Finite(wall.X1) || !Finite(wall.Y1) || !Finite(wall.X2) || !Finite(wall.Y2))
                {
                    errors.Add($"Wall '{id ?? "<unknown>"}' coordinates must be finite.");
                    continue;
                }

                if (PositiveFinite(layout.Width) && PositiveFinite(layout.Height) &&
                    (!Inside(wall.X1, wall.Y1, layout) || !Inside(wall.X2, wall.Y2, layout)))
                {
                    errors.Add($"Wall '{id ?? "<unknown>"}' must be inside the layout bounds.");
                }

                var dx = wall.X2 - wall.X1;
                var dy = wall.Y2 - wall.Y1;
                if ((dx * dx) + (dy * dy) <= GeometryEpsilon)
                {
                    errors.Add($"Wall '{id ?? "<unknown>"}' must have a positive length.");
                }
            }
        }

        private static void ValidateShelves(ProjectLayout layout, ICollection<string> errors)
        {
            if (layout.Shelves == null)
            {
                errors.Add("Layout must contain a shelves array.");
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var shelf in layout.Shelves)
            {
                if (shelf == null)
                {
                    errors.Add("Layout shelves cannot contain null items.");
                    continue;
                }

                var id = shelf.Id?.Trim();
                if (string.IsNullOrEmpty(id)) errors.Add("Every shelf requires a non-empty id.");
                else if (!ids.Add(id)) errors.Add($"Shelf id '{id}' is duplicated.");

                if (!Finite(shelf.X) || !Finite(shelf.Y) || !PositiveFinite(shelf.W) || !PositiveFinite(shelf.H))
                {
                    errors.Add($"Shelf '{id ?? "<unknown>"}' geometry must use finite coordinates and positive width/height.");
                    continue;
                }

                if (PositiveFinite(layout.Width) && PositiveFinite(layout.Height) &&
                    (shelf.X < 0 || shelf.Y < 0 || shelf.X + shelf.W > layout.Width || shelf.Y + shelf.H > layout.Height))
                {
                    errors.Add($"Shelf '{id ?? "<unknown>"}' must be inside the layout bounds.");
                }

                if (!Finite(shelf.Valence) || shelf.Valence < -1 || shelf.Valence > 1)
                {
                    errors.Add($"Shelf '{id ?? "<unknown>"}' valence must be between -1 and 1.");
                }
            }
        }

        private static void ValidateSpawnRateCurve(ProjectSpawnRatePoint[]? curve, ICollection<string> errors)
        {
            if (curve == null) return;

            foreach (var point in curve)
            {
                if (point == null || !Finite(point.Minute) || !Finite(point.Rate) || point.Minute < 0 || point.Rate < 0)
                {
                    errors.Add("Spawn-rate points must contain finite, non-negative minute and rate values.");
                    return;
                }
            }
        }

        private static void CheckReachability(ProjectLayout layout, ICollection<string> warnings, ICollection<string> unreachableShelfIds, ICollection<string> errors)
        {
            try
            {
                var simulationLayout = ToSimulationLayout(layout);
                var grid = new PathGrid(simulationLayout, new SimulationConfig());
                var entrance = simulationLayout.Entrance;

                if (!grid.IsPointWalkable(entrance))
                {
                    errors.Add("Layout entrance is blocked by wall or shelf geometry.");
                    return;
                }

                if (!grid.IsPointWalkable(simulationLayout.Checkout))
                {
                    errors.Add("Layout checkout is blocked by wall or shelf geometry.");
                    return;
                }

                foreach (var shelf in simulationLayout.Shelves)
                {
                    if (grid.ShelfAccessPaths(shelf, entrance).Count > 0) continue;
                    unreachableShelfIds.Add(shelf.Id);
                    warnings.Add($"Shelf '{(string.IsNullOrWhiteSpace(shelf.Label) ? shelf.Id : shelf.Label)}' cannot be reached from the entrance.");
                }
            }
            catch (Exception exception)
            {
                errors.Add($"Layout navigation validation failed: {exception.Message}");
            }
        }

        private static LayoutDefinition ToSimulationLayout(ProjectLayout layout) => new LayoutDefinition
        {
            Width = layout.Width,
            Height = layout.Height,
            Entrance = new Position2D(layout.Entrance!.X, layout.Entrance.Y),
            Checkout = new Position2D(layout.Checkout!.X, layout.Checkout.Y),
            Walls = layout.Walls!.Select(wall => new WallDefinition
            {
                Id = wall.Id!, X1 = wall.X1, Y1 = wall.Y1, X2 = wall.X2, Y2 = wall.Y2
            }).ToArray(),
            Shelves = layout.Shelves!.Select(shelf => new ShelfDefinition
            {
                Id = shelf.Id!, Label = shelf.Label ?? string.Empty, Category = shelf.Category ?? string.Empty,
                X = shelf.X, Y = shelf.Y, Width = shelf.W, Height = shelf.H, Valence = shelf.Valence
            }).ToArray(),
            SpawnRateCurve = (layout.SpawnRateCurve ?? Array.Empty<ProjectSpawnRatePoint>()).Select(point => new SpawnRatePoint
            {
                Minute = point.Minute, Rate = point.Rate
            }).ToArray()
        };

        private static LayoutValidationResult Invalid(string message) =>
            new LayoutValidationResult(new[] { message }, Array.Empty<string>(), Array.Empty<string>());

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool PositiveFinite(double value) => Finite(value) && value > 0;
        private static bool Inside(double x, double y, ProjectLayout layout) => x >= 0 && y >= 0 && x <= layout.Width && y <= layout.Height;
    }
}
