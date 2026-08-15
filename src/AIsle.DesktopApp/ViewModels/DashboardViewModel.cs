using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AIsle.DesktopApp.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        public override string Title => "Dashboard";

        private readonly Services.HistoryService _historyService;

        [ObservableProperty] private int _totalRuns;
        [ObservableProperty] private string _avgRevenue = "0 ₫";
        [ObservableProperty] private string _avgConversion = "0%";
        [ObservableProperty] private int _totalProducts;

        public ObservableCollection<RunCard> RecentRuns { get; } = new();

        public DashboardViewModel(Services.HistoryService historyService)
        {
            _historyService = historyService;
            Refresh();
        }

        public void Refresh()
        {
            var runs = _historyService.ListAll();
            TotalRuns = runs.Count;

            if (runs.Count > 0)
            {
                var avgRev = runs.Where(r => r.Summary != null).Select(r => r.Summary!.Revenue).DefaultIfEmpty(0).Average();
                AvgRevenue = $"{avgRev:N0} ₫";
                var avgConv = runs.Where(r => r.Summary != null).Select(r => r.Summary!.ConversionRate).DefaultIfEmpty(0).Average();
                AvgConversion = $"{avgConv * 100:F1}%";
            }
            else
            {
                AvgRevenue = "0 ₫";
                AvgConversion = "0%";
            }

            RecentRuns.Clear();
            foreach (var run in runs.Take(5))
            {
                RecentRuns.Add(new RunCard
                {
                    Name = run.Name ?? run.Id,
                    Date = run.CreatedAt ?? "",
                    Revenue = $"{run.Summary?.Revenue ?? 0:N0} ₫",
                    Purchases = run.Summary?.Purchases ?? 0,
                });
            }
        }
    }

    public class RunCard
    {
        public string Name { get; set; } = "";
        public string Date { get; set; } = "";
        public string Revenue { get; set; } = "0 ₫";
        public int Purchases { get; set; }
    }
}
