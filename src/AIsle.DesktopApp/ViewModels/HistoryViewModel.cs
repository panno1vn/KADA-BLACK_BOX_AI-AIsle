using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIsle.DesktopApp.ViewModels
{
    public partial class HistoryViewModel : ViewModelBase
    {
        public override string Title => "Lịch sử Phiên chạy";

        private readonly Services.HistoryService _service;

        public ObservableCollection<Services.SimRunSummary> Runs { get; } = new();

        [ObservableProperty] private Services.SimRunSummary? _selectedRun;
        [ObservableProperty] private string _detailText = "";

        public HistoryViewModel(Services.HistoryService service)
        {
            _service = service;
            Refresh();
        }

        public void Refresh()
        {
            Runs.Clear();
            foreach (var run in _service.ListAll()) Runs.Add(run);
        }

        partial void OnSelectedRunChanged(Services.SimRunSummary? value)
        {
            if (value == null) { DetailText = ""; return; }
            var json = _service.GetRunJson(value.Id);
            DetailText = json != null
                ? $"ID: {value.Id}\nTên: {value.Name}\nNgày: {value.CreatedAt}\nSeed: {value.Seed}\nThời lượng: {value.DurationMinutes} phút\n\nDoanh thu: {value.Summary?.Revenue:N0} ₫\nSố lượt mua: {value.Summary?.Purchases}\nTỷ lệ chuyển đổi: {value.Summary?.ConversionRate * 100:F1}%\nKhông tìm thấy: {value.Summary?.NotFoundRate * 100:F1}%"
                : "Không tìm thấy file chi tiết.";
        }

        [RelayCommand]
        private void ExportRun()
        {
            if (SelectedRun == null) return;
            var json = _service.GetRunJson(SelectedRun.Id);
            if (json == null) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"aisle-{SelectedRun.Id}.sim-result.json",
                DefaultExt = ".json",
                Filter = "JSON files (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(dialog.FileName, json);
            }
        }
    }
}
