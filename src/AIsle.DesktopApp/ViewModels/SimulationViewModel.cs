using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIsle.DesktopApp.ViewModels
{
    public partial class SimulationViewModel : ViewModelBase
    {
        private readonly Services.CatalogService _catalogService;
        private readonly Services.LayoutService _layoutService;
        private readonly Services.HistoryService _historyService;

        [ObservableProperty] private bool _isRunning;
        [ObservableProperty] private TimeSpan _currentTime;
        
        // Layout Properties
        [ObservableProperty] private double _storeWidth;
        [ObservableProperty] private double _storeHeight;
        public ObservableCollection<Services.Wall> Walls { get; } = new();
        public ObservableCollection<Services.Shelf> Shelves { get; } = new();
        public ObservableCollection<Services.Npc> ActiveNpcs { get; } = new();

        // Metrics Properties
        [ObservableProperty] private int _totalShoppers = 0;
        [ObservableProperty] private double _totalRevenue = 0;
        [ObservableProperty] private double _conversionRate = 0;
        [ObservableProperty] private int _convertedShoppers = 0;
        [ObservableProperty] private int _totalPurchases = 0;

        public SimulationViewModel(Services.CatalogService catalogService, Services.LayoutService layoutService, Services.HistoryService historyService)
        {
            _catalogService = catalogService;
            _layoutService = layoutService;
            _historyService = historyService;
        }

        public void InitializeSimulation()
        {
            var layout = _layoutService.GetLayout();
            StoreWidth = layout.Width;
            StoreHeight = layout.Height;

            Walls.Clear();
            foreach (var w in layout.Walls) Walls.Add(w);

            Shelves.Clear();
            foreach (var s in layout.Shelves) Shelves.Add(s);
            
            ResetSimulation();
        }

        [RelayCommand]
        private void StartSimulation()
        {
            if (IsRunning) return;
            IsRunning = true;
            
            // TODO: Start actual simulation loop via AIsle.Simulation
            // For now, mock a quick completion
            _ = Task.Run(async () =>
            {
                for (int i = 0; i <= 30; i++)
                {
                    if (!IsRunning) break;
                    CurrentTime = TimeSpan.FromMinutes(i);
                    await Task.Delay(50);
                }
                
                if (IsRunning)
                {
                    // Simulation finished
                    IsRunning = false;
                    TotalShoppers = 180;
                    TotalRevenue = 250000;
                    ConversionRate = 42.5;
                    ConvertedShoppers = 76;
                    TotalPurchases = 89;

                    SaveLog();
                }
            });
        }

        [RelayCommand]
        private void StopSimulation()
        {
            if (!IsRunning) return;
            IsRunning = false;
            SaveLog();
            ResetSimulation();
        }

        private void SaveLog()
        {
            var summary = new Services.SimRunSummary
            {
                Id = "run-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                SchemaVersion = "1.0",
                CreatedAt = DateTime.UtcNow.ToString("O"),
                Name = "Layout A - live test",
                Seed = 42,
                DurationMinutes = 30,
                Summary = new Services.SimSummaryData
                {
                    Time = 30,
                    Revenue = TotalRevenue,
                    Purchases = TotalPurchases,
                    ConversionRate = ConversionRate,
                    NotFoundRate = 0,
                    Spawned = TotalShoppers,
                    Active = 0
                }
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(summary, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            
            _historyService.SaveRun(summary.Id, json);
        }

        private void ResetSimulation()
        {
            IsRunning = false;
            CurrentTime = TimeSpan.Zero;
            TotalShoppers = 0;
            TotalRevenue = 0;
            ConversionRate = 0;
            ConvertedShoppers = 0;
            TotalPurchases = 0;
            ActiveNpcs.Clear();
        }
    }
}
