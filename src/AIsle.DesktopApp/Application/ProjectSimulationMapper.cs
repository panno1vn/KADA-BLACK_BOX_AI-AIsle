using System;
using AIsle.Contracts.Project;
using AIsle.Contracts.Simulation;

namespace AIsle.DesktopApp.Application
{
    public static class ProjectSimulationMapper
    {
        public static LayoutDefinition MapLayout(ProjectDocument project)
        {
            var layout = project?.Layout ?? throw new ArgumentException("Project layout is required.", nameof(project));
            var walls = layout.Walls ?? Array.Empty<ProjectWall>();
            var shelves = layout.Shelves ?? Array.Empty<ProjectShelf>();
            var curve = layout.SpawnRateCurve ?? Array.Empty<ProjectSpawnRatePoint>();
            var result = new LayoutDefinition
            {
                Width = layout.Width,
                Height = layout.Height,
                Entrance = MapPoint(layout.Entrance, "entrance"),
                Checkout = MapPoint(layout.Checkout, "checkout"),
                Walls = new WallDefinition[walls.Length],
                Shelves = new ShelfDefinition[shelves.Length],
                SpawnRateCurve = new SpawnRatePoint[curve.Length]
            };
            for (var index = 0; index < walls.Length; index++)
                result.Walls[index] = new WallDefinition { Id = walls[index].Id ?? string.Empty, X1 = walls[index].X1, Y1 = walls[index].Y1, X2 = walls[index].X2, Y2 = walls[index].Y2 };
            for (var index = 0; index < shelves.Length; index++)
                result.Shelves[index] = new ShelfDefinition { Id = shelves[index].Id ?? string.Empty, Label = shelves[index].Label ?? string.Empty, Category = shelves[index].Category ?? string.Empty, X = shelves[index].X, Y = shelves[index].Y, Width = shelves[index].W, Height = shelves[index].H, Valence = shelves[index].Valence };
            for (var index = 0; index < curve.Length; index++) result.SpawnRateCurve[index] = new SpawnRatePoint { Minute = curve[index].Minute, Rate = curve[index].Rate };
            return result;
        }

        public static ProductDefinition[] MapCatalog(ProjectDocument project)
        {
            var catalog = project?.Catalog ?? Array.Empty<ProjectProduct>();
            var result = new ProductDefinition[catalog.Length];
            for (var index = 0; index < catalog.Length; index++)
                result[index] = new ProductDefinition { Id = catalog[index].Id ?? string.Empty, Name = catalog[index].Name ?? string.Empty, Category = catalog[index].Category ?? string.Empty, ShelfId = catalog[index].Shelf ?? string.Empty, Price = catalog[index].Price };
            return result;
        }

        private static Position2D MapPoint(ProjectPoint? point, string name)
        {
            if (point == null) throw new ArgumentException("Project " + name + " is required.");
            return new Position2D(point.X, point.Y);
        }
    }
}
