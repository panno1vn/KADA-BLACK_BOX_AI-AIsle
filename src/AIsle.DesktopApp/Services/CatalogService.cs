using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AIsle.DesktopApp.Services
{
    public class Product
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Shelf { get; set; } = "";
        public string ShelfId
        {
            get => Shelf;
            set => Shelf = value;
        }
        public double Price { get; set; }
    }

    public class CatalogService
    {
        private readonly string _filePath;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public CatalogService()
        {
            _filePath = FindCatalogPath();
        }

        public List<Product> GetAll()
        {
            if (!File.Exists(_filePath)) return new List<Product>();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<Product>>(json, JsonOptions) ?? new List<Product>();
        }

        public void Add(Product product)
        {
            var list = GetAll();
            if (string.IsNullOrEmpty(product.Id))
                product.Id = "p" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            list.Add(product);
            Save(list);
        }

        public void Update(Product product)
        {
            var list = GetAll();
            var index = list.FindIndex(p => p.Id == product.Id);
            if (index >= 0)
            {
                list[index] = product;
                Save(list);
            }
        }

        public void Delete(string id)
        {
            var list = GetAll();
            list.RemoveAll(p => p.Id == id);
            Save(list);
        }

        public void Save(List<Product> products)
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(products, JsonOptions));
        }

        private static string FindCatalogPath()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            while (dir != null)
            {
                var candidate = Path.Combine(dir, "runtime", "catalog.json");
                if (File.Exists(candidate)) return candidate;
                if (File.Exists(Path.Combine(dir, "AGENTS.md"))) return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return Path.Combine(Directory.GetCurrentDirectory(), "runtime", "catalog.json");
        }
    }
}
