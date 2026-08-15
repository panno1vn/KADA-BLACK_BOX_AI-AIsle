using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIsle.DesktopApp.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ViewModelBase _currentView;

        [ObservableProperty]
        private bool _isSetupTabActive = true;

        [ObservableProperty]
        private bool _isRunTabActive = false;

        [ObservableProperty]
        private bool _isCatalogOpen = false;

        public CatalogViewModel CatalogVM { get; }
        private readonly LayoutViewModel _layoutVM;
        private readonly SimulationViewModel _simulationVM;
        private readonly Services.HistoryService _historyService;

        public MainViewModel()
        {
            var catalogService = new Services.CatalogService();
            var layoutService = new Services.LayoutService();
            _historyService = new Services.HistoryService();

            CatalogVM = new CatalogViewModel(catalogService);
            _layoutVM = new LayoutViewModel(layoutService);
            _simulationVM = new SimulationViewModel(catalogService, layoutService, _historyService);

            // Gắn event để mở Catalog từ Layout
            _layoutVM.RequestOpenCatalog += () => IsCatalogOpen = true;

            // Gắn event để chuyển sang Run từ Layout
            _layoutVM.RequestRunSimulation += SwitchToRunTab;

            CurrentView = _layoutVM;
        }

        [RelayCommand]
        private void SwitchToSetupTab()
        {
            IsSetupTabActive = true;
            IsRunTabActive = false;
            CurrentView = _layoutVM;
        }

        [RelayCommand]
        private void SwitchToRunTab()
        {
            IsSetupTabActive = false;
            IsRunTabActive = true;
            CurrentView = _simulationVM;
            _simulationVM.InitializeSimulation(); // Chạy khi chuyển sang màn này
        }

        [RelayCommand]
        private void CloseCatalog()
        {
            IsCatalogOpen = false;
        }
    }
}
