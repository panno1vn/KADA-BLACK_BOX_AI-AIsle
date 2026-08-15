using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AIsle.DesktopApp.Services
{
    public class Npc
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public partial class Wall : ObservableObject
    {
        [ObservableProperty] private string _id = "";
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Length))]
        private double _x1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Length))]
        private double _y1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Length))]
        private double _x2;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Length))]
        private double _y2;

        public double Length => Math.Round(Math.Sqrt((X2 - X1) * (X2 - X1) + (Y2 - Y1) * (Y2 - Y1)), 2);
    }

    public partial class Shelf : ObservableObject
    {
        [ObservableProperty] private string _id = "";
        [ObservableProperty] private string _label = "";
        [ObservableProperty] private string _category = "";
        [ObservableProperty] private double _x;
        [ObservableProperty] private double _y;
        [ObservableProperty] private double _w;
        [ObservableProperty] private double _h;
        [ObservableProperty] private double _valence;
    }

    public class PointData
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class StoreLayout
    {
        public double Width { get; set; } = 12;
        public double Height { get; set; } = 8;
        public List<Wall> Walls { get; set; } = new();
        public List<Shelf> Shelves { get; set; } = new();
        public PointData? Entrance { get; set; }
        public PointData? Checkout { get; set; }
    }

    public class LayoutService
    {
        private readonly string _filePath;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public LayoutService()
        {
            _filePath = FindLayoutPath();
        }

        public StoreLayout GetLayout()
        {
            if (!File.Exists(_filePath)) return new StoreLayout();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<StoreLayout>(json, JsonOptions) ?? new StoreLayout();
        }

        public void AddWall(Wall wall)
        {
            var layout = GetLayout();
            if (string.IsNullOrEmpty(wall.Id))
                wall.Id = "w" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            layout.Walls.Add(wall);
            Save(layout);
        }

        public void AddShelf(Shelf shelf)
        {
            var layout = GetLayout();
            if (string.IsNullOrEmpty(shelf.Id))
                shelf.Id = "s" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            layout.Shelves.Add(shelf);
            Save(layout);
        }

        public void UpdateWall(Wall wall)
        {
            var layout = GetLayout();
            var index = layout.Walls.FindIndex(w => w.Id == wall.Id);
            if (index >= 0) { layout.Walls[index] = wall; Save(layout); }
        }

        public void UpdateShelf(Shelf shelf)
        {
            var layout = GetLayout();
            var index = layout.Shelves.FindIndex(s => s.Id == shelf.Id);
            if (index >= 0) { layout.Shelves[index] = shelf; Save(layout); }
        }

        public void DeleteWall(string id)
        {
            var layout = GetLayout();
            layout.Walls.RemoveAll(w => w.Id == id);
            Save(layout);
        }

        public void DeleteShelf(string id)
        {
            var layout = GetLayout();
            layout.Shelves.RemoveAll(s => s.Id == id);
            Save(layout);
        }

        public void UpdateEntrance(PointData p)
        {
            var layout = GetLayout();
            layout.Entrance = p;
            Save(layout);
        }

        public void UpdateCheckout(PointData p)
        {
            var layout = GetLayout();
            layout.Checkout = p;
            Save(layout);
        }

        public void Save(StoreLayout layout)
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(layout, JsonOptions));
        }

        private static string FindLayoutPath()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            while (dir != null)
            {
                var candidate = Path.Combine(dir, "runtime", "layout.json");
                if (File.Exists(candidate)) return candidate;
                if (File.Exists(Path.Combine(dir, "AGENTS.md"))) return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return Path.Combine(Directory.GetCurrentDirectory(), "runtime", "layout.json");
        }
    }
}
