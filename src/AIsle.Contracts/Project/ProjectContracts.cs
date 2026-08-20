#nullable enable
using System;

namespace AIsle.Contracts.Project
{
    public static class ProjectSchema
    {
        public const string Version = "aisle.project.v1";
    }

    [Serializable]
    public sealed class ProjectDocument
    {
        public string? SchemaVersion { get; set; }
        public ProjectLayout? Layout { get; set; }
        public ProjectProduct[]? Catalog { get; set; }
    }

    [Serializable]
    public sealed class ProjectLayout
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public ProjectWall[]? Walls { get; set; }
        public ProjectShelf[]? Shelves { get; set; }
        public ProjectPoint? Entrance { get; set; }
        public ProjectPoint? Checkout { get; set; }
        public ProjectSpawnRatePoint[]? SpawnRateCurve { get; set; }
    }

    [Serializable]
    public sealed class ProjectPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    [Serializable]
    public sealed class ProjectWall
    {
        public string? Id { get; set; }
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
    }

    [Serializable]
    public sealed class ProjectShelf
    {
        public string? Id { get; set; }
        public string? Label { get; set; }
        public string? Category { get; set; }
        public string? PresetId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double W { get; set; }
        public double H { get; set; }
        public double Valence { get; set; }
    }

    [Serializable]
    public sealed class ProjectProduct
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Shelf { get; set; }
        public string? ShelfId
        {
            get => Shelf;
            set => Shelf = value;
        }
        public double Price { get; set; }
    }

    [Serializable]
    public sealed class ProjectSpawnRatePoint
    {
        public double Minute { get; set; }
        public double Rate { get; set; }
    }
}
