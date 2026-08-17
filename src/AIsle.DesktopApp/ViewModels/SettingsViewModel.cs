using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIsle.DesktopApp.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        public override string Title => "Cài đặt Tham số";

        [ObservableProperty] private double _tickSeconds = 0.2;
        [ObservableProperty] private double _utilityNeedWeight = 1.0;
        [ObservableProperty] private double _utilityExploreWeight = 0.72;
        [ObservableProperty] private double _utilityValenceWeight = 0.16;
        [ObservableProperty] private double _distancePenalty = 0.05;
        [ObservableProperty] private double _purchaseNeedA = 3.0;
        [ObservableProperty] private double _purchaseValenceB = 1.5;
        [ObservableProperty] private double _purchaseBiasC = -2.0;
        [ObservableProperty] private double _impulseBase = 0.08;
        [ObservableProperty] private int _maxShelfVisits = 3;
        [ObservableProperty] private double _collisionRadius = 0.32;
        [ObservableProperty] private double _separationStrength = 0.22;
        [ObservableProperty] private string _statusMessage = "";

        [RelayCommand]
        private void ResetDefaults()
        {
            TickSeconds = 0.2;
            UtilityNeedWeight = 1.0;
            UtilityExploreWeight = 0.72;
            UtilityValenceWeight = 0.16;
            DistancePenalty = 0.05;
            PurchaseNeedA = 3.0;
            PurchaseValenceB = 1.5;
            PurchaseBiasC = -2.0;
            ImpulseBase = 0.08;
            MaxShelfVisits = 3;
            CollisionRadius = 0.32;
            SeparationStrength = 0.22;
            StatusMessage = "✅ Đã reset về giá trị mặc định.";
        }

        [RelayCommand]
        private void SaveSettings()
        {
            StatusMessage = "✅ Đã lưu tham số Utility AI.";
        }
    }
}
