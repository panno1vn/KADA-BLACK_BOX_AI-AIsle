using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIsle.DesktopApp.ViewModels
{
    public partial class LayoutViewModel : ViewModelBase
    {
        public override string Title => "Layout Cửa hàng";

        private readonly Services.LayoutService _service;

        public ObservableCollection<Services.Wall> Walls { get; } = new();
        public ObservableCollection<Services.Shelf> Shelves { get; } = new();
        public System.Collections.Generic.IReadOnlyCollection<AIsle.Contracts.Project.ShelfPresetDefinition> Presets => AIsle.Contracts.Project.ShelfPresets.All;

        [ObservableProperty] private double _storeWidth;
        [ObservableProperty] private double _storeHeight;
        [ObservableProperty] private string _entranceInfo = "";
        [ObservableProperty] private string _checkoutInfo = "";
        
        [ObservableProperty] private double _entranceX = -100;
        [ObservableProperty] private double _entranceY = -100;
        [ObservableProperty] private double _checkoutX = -100;
        [ObservableProperty] private double _checkoutY = -100;

        [ObservableProperty] private string _statusMessage = "";
        
        [ObservableProperty] private string _activeTool = "Select"; // Select, Wall, Shelf, Entrance, Checkout

        [ObservableProperty] private Services.Shelf? _selectedShelf;
        [ObservableProperty] private Services.Wall? _selectedWall;

        partial void OnSelectedShelfChanged(Services.Shelf? oldValue, Services.Shelf? newValue)
        {
            if (oldValue != null) oldValue.PropertyChanged -= SelectedObject_PropertyChanged;
            if (newValue != null) newValue.PropertyChanged += SelectedObject_PropertyChanged;
        }

        partial void OnSelectedWallChanged(Services.Wall? oldValue, Services.Wall? newValue)
        {
            if (oldValue != null) oldValue.PropertyChanged -= SelectedObject_PropertyChanged;
            if (newValue != null) newValue.PropertyChanged += SelectedObject_PropertyChanged;
        }

        private void SelectedObject_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is Services.Shelf shelf) _service.UpdateShelf(shelf);
            if (sender is Services.Wall wall) _service.UpdateWall(wall);
        }

        public event Action? RequestOpenCatalog;
        public event Action? RequestRunSimulation;

        public LayoutViewModel(Services.LayoutService service)
        {
            _service = service;
            LoadLayout();
        }

        private void LoadLayout()
        {
            var layout = _service.GetLayout();
            StoreWidth = layout.Width;
            StoreHeight = layout.Height;
            EntranceInfo = layout.Entrance != null ? $"({layout.Entrance.X}, {layout.Entrance.Y})" : "Chưa đặt";
            CheckoutInfo = layout.Checkout != null ? $"({layout.Checkout.X}, {layout.Checkout.Y})" : "Chưa đặt";

            EntranceX = layout.Entrance?.X ?? -100;
            EntranceY = layout.Entrance?.Y ?? -100;
            CheckoutX = layout.Checkout?.X ?? -100;
            CheckoutY = layout.Checkout?.Y ?? -100;

            Walls.Clear();
            foreach (var w in layout.Walls) Walls.Add(w);

            Shelves.Clear();
            foreach (var s in layout.Shelves) Shelves.Add(s);
        }

        [RelayCommand]
        private void AddWall()
        {
            _service.AddWall(new Services.Wall { X1 = 2, Y1 = 2, X2 = 4, Y2 = 2 });
            LoadLayout();
            StatusMessage = "✅ Đã thêm tường mới";
        }

        [RelayCommand]
        private void SetTool(string toolName)
        {
            ActiveTool = toolName;
            StatusMessage = $"Chế độ: {toolName}";
        }

        private bool _isDrawing;
        private double _startX, _startY;
        private Services.Wall? _tempWall;
        private Services.Shelf? _tempShelf;

        public void HandlePointerDown(double x, double y)
        {
            x = Math.Round(x * 2) / 2;
            y = Math.Round(y * 2) / 2;

            if (ActiveTool == "Select")
            {
                SelectedShelf = Shelves.FirstOrDefault(s => x >= s.X && x <= s.X + s.W && y >= s.Y && y <= s.Y + s.H);
                SelectedWall = null;
                if (SelectedShelf == null)
                {
                    SelectedWall = Walls.FirstOrDefault(w => DistanceToSegment(x, y, w.X1, w.Y1, w.X2, w.Y2) < 0.5);
                }
                StatusMessage = SelectedShelf != null ? $"Đã chọn kệ {SelectedShelf.Label}" : (SelectedWall != null ? $"Đã chọn tường {SelectedWall.Id}" : "Chưa chọn gì");
            }
            else if (ActiveTool == "Wall" || ActiveTool == "Shelf")
            {
                _isDrawing = true;
                _startX = x;
                _startY = y;

                if (ActiveTool == "Wall")
                {
                    _tempWall = new Services.Wall { X1 = x, Y1 = y, X2 = x, Y2 = y };
                    Walls.Add(_tempWall);
                }
                else if (ActiveTool == "Shelf")
                {
                    _tempShelf = new Services.Shelf { Label = "New", X = x, Y = y, W = 0, H = 0 };
                    Shelves.Add(_tempShelf);
                }
            }
            else if (ActiveTool == "Entrance")
            {
                _service.UpdateEntrance(new Services.PointData { X = x, Y = y });
                LoadLayout();
                StatusMessage = $"✅ Đã đặt Entrance tại ({x}, {y})";
            }
            else if (ActiveTool == "Checkout")
            {
                _service.UpdateCheckout(new Services.PointData { X = x, Y = y });
                LoadLayout();
                StatusMessage = $"✅ Đã đặt Checkout tại ({x}, {y})";
            }
        }

        private double DistanceToSegment(double px, double py, double x1, double y1, double x2, double y2)
        {
            double l2 = (x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1);
            if (l2 == 0) return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
            double t = Math.Max(0, Math.Min(1, ((px - x1) * (x2 - x1) + (py - y1) * (y2 - y1)) / l2));
            double projX = x1 + t * (x2 - x1);
            double projY = y1 + t * (y2 - y1);
            return Math.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
        }

        public void HandlePointerMove(double x, double y)
        {
            if (!_isDrawing) return;
            
            // Xử lý Shift hoặc snap cho mượt
            x = Math.Round(x * 2) / 2;
            y = Math.Round(y * 2) / 2;

            if (ActiveTool == "Wall" && _tempWall != null)
            {
                Walls.Remove(_tempWall);
                _tempWall.X2 = x;
                _tempWall.Y2 = y;
                Walls.Add(_tempWall);
            }
            else if (ActiveTool == "Shelf" && _tempShelf != null)
            {
                Shelves.Remove(_tempShelf);
                _tempShelf.W = Math.Max(0.5, Math.Abs(x - _startX));
                _tempShelf.H = Math.Max(0.5, Math.Abs(y - _startY));
                _tempShelf.X = Math.Min(_startX, x);
                _tempShelf.Y = Math.Min(_startY, y);
                Shelves.Add(_tempShelf);
            }
        }

        public void HandlePointerUp(double x, double y)
        {
            if (!_isDrawing) return;
            _isDrawing = false;
            
            if (ActiveTool == "Wall" && _tempWall != null)
            {
                Walls.Remove(_tempWall);
                if (_tempWall.X1 != _tempWall.X2 || _tempWall.Y1 != _tempWall.Y2)
                {
                    _service.AddWall(_tempWall);
                    StatusMessage = $"✅ Đã vẽ tường mới";
                }
                _tempWall = null;
                LoadLayout();
            }
            else if (ActiveTool == "Shelf" && _tempShelf != null)
            {
                Shelves.Remove(_tempShelf);
                if (_tempShelf.W > 0 && _tempShelf.H > 0)
                {
                    _service.AddShelf(_tempShelf);
                    StatusMessage = $"✅ Đã vẽ kệ mới";
                }
                _tempShelf = null;
                LoadLayout();
            }
        }

        [RelayCommand]
        private void DeleteWall(Services.Wall wall)
        {
            _service.DeleteWall(wall.Id);
            Walls.Remove(wall);
            if (SelectedWall == wall) SelectedWall = null;
            LoadLayout();
            StatusMessage = $"🗑 Đã xóa tường {wall.Id}";
        }

        [RelayCommand]
        private void SetSelectedWall(Services.Wall wall)
        {
            SelectedWall = wall;
            SelectedShelf = null;
        }

        [RelayCommand]
        private void SetSelectedShelf(Services.Shelf shelf)
        {
            SelectedShelf = shelf;
            SelectedWall = null;
        }

        [RelayCommand]
        private void AddShelf()
        {
            _service.AddShelf(new Services.Shelf
            {
                Label = $"Shelf {Shelves.Count + 1}",
                Category = "other",
                X = 3, Y = 3, W = 2, H = 0.7,
                Valence = 0.2
            });
            LoadLayout();
            StatusMessage = "✅ Đã thêm kệ hàng mới";
        }

        [RelayCommand]
        private void DeleteShelf(Services.Shelf? shelf)
        {
            if (shelf == null) return;
            _service.DeleteShelf(shelf.Id);
            LoadLayout();
            StatusMessage = $"🗑 Đã xóa kệ {shelf.Label}";
        }

        [RelayCommand]
        private void RotateSelectedShelf()
        {
            if (SelectedShelf != null)
            {
                SelectedShelf.Rotate90();
                _service.UpdateShelf(SelectedShelf);
                StatusMessage = $"🔄 Đã xoay kệ {SelectedShelf.Label}: {SelectedShelf.W}m × {SelectedShelf.H}m ({SelectedShelf.Rotation}°)";
            }
        }

        [RelayCommand]
        private void FlipSelectedShelfH()
        {
            if (SelectedShelf != null)
            {
                SelectedShelf.ToggleFlipX();
                _service.UpdateShelf(SelectedShelf);
                StatusMessage = $"⇄ Đã lật ngang kệ {SelectedShelf.Label}";
            }
        }

        [RelayCommand]
        private void FlipSelectedShelfV()
        {
            if (SelectedShelf != null)
            {
                SelectedShelf.ToggleFlipY();
                _service.UpdateShelf(SelectedShelf);
                StatusMessage = $"⇅ Đã lật dọc kệ {SelectedShelf.Label}";
            }
        }

        [RelayCommand]
        private void OpenCatalog()
        {
            RequestOpenCatalog?.Invoke();
        }

        [RelayCommand]
        private void RunSimulation()
        {
            RequestRunSimulation?.Invoke();
        }
    }
}
