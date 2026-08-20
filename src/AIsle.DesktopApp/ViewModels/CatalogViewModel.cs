using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIsle.DesktopApp.ViewModels
{
    public partial class CatalogViewModel : ViewModelBase
    {
        public override string Title => "Quản lý Sản phẩm";

        private readonly Services.CatalogService _service;
        private readonly Services.LayoutService? _layoutService;

        public ObservableCollection<Services.Product> Products { get; } = new();
        public ObservableCollection<Services.Shelf> AvailableShelves { get; } = new();

        [ObservableProperty] private Services.Product? _selectedProduct;
        [ObservableProperty] private Services.Shelf? _selectedShelf;
        [ObservableProperty] private string _editId = "";
        [ObservableProperty] private string _editName = "";
        [ObservableProperty] private string _editCategory = "";
        [ObservableProperty] private string _editShelf = "";
        [ObservableProperty] private string _editPrice = "";
        [ObservableProperty] private bool _isEditing;
        [ObservableProperty] private string _statusMessage = "";
        [ObservableProperty] private bool _hasAvailableShelves;

        public CatalogViewModel(Services.CatalogService service, Services.LayoutService? layoutService = null)
        {
            _service = service;
            _layoutService = layoutService;
            RefreshAvailableShelves();
            LoadProducts();
        }

        public void RefreshAvailableShelves()
        {
            AvailableShelves.Clear();
            if (_layoutService != null)
            {
                var layout = _layoutService.GetLayout();
                foreach (var shelf in layout.Shelves)
                {
                    AvailableShelves.Add(shelf);
                }
            }
            HasAvailableShelves = AvailableShelves.Count > 0;
        }

        partial void OnSelectedShelfChanged(Services.Shelf? value)
        {
            if (value != null)
            {
                EditShelf = value.Id;
                EditCategory = value.Category;
            }
        }

        private void LoadProducts()
        {
            Products.Clear();
            foreach (var p in _service.GetAll()) Products.Add(p);
        }

        partial void OnSelectedProductChanged(Services.Product? value)
        {
            if (value != null)
            {
                RefreshAvailableShelves();
                EditId = value.Id;
                EditName = value.Name;
                EditCategory = value.Category;
                EditShelf = value.Shelf;
                EditPrice = value.Price.ToString("F0");
                SelectedShelf = null;
                foreach (var s in AvailableShelves)
                {
                    if (string.Equals(s.Id, value.Shelf, System.StringComparison.OrdinalIgnoreCase))
                    {
                        SelectedShelf = s;
                        break;
                    }
                }
                IsEditing = true;
            }
        }

        partial void OnHasAvailableShelvesChanged(bool value)
        {
            NewProductCommand.NotifyCanExecuteChanged();
            if (!value)
            {
                StatusMessage = "⚠ Chưa có kệ hàng nào! Vui lòng tạo kệ ở Setup Layout trước.";
            }
        }

        [RelayCommand(CanExecute = nameof(HasAvailableShelves))]
        private void NewProduct()
        {
            RefreshAvailableShelves();

            EditId = "";
            EditName = "";
            EditCategory = "";
            EditShelf = "";
            EditPrice = "";
            SelectedShelf = AvailableShelves.Count > 0 ? AvailableShelves[0] : null;
            IsEditing = true;
            SelectedProduct = null;
            StatusMessage = "Nhập thông tin sản phẩm mới...";
        }

        [RelayCommand]
        private void SaveProduct()
        {
            if (string.IsNullOrWhiteSpace(EditName))
            {
                StatusMessage = "⚠ Tên sản phẩm không được để trống!";
                return;
            }

            if (!double.TryParse(EditPrice, out var price) || price < 0)
            {
                StatusMessage = "⚠ Giá tiền không hợp lệ!";
                return;
            }

            var product = new Services.Product
            {
                Id = EditId,
                Name = EditName,
                Category = EditCategory,
                Shelf = EditShelf,
                Price = price
            };

            if (string.IsNullOrEmpty(product.Id))
            {
                _service.Add(product);
                StatusMessage = $"✅ Đã thêm sản phẩm: {product.Name}";
            }
            else
            {
                _service.Update(product);
                StatusMessage = $"✅ Đã cập nhật sản phẩm: {product.Name}";
            }

            LoadProducts();
            IsEditing = false;
        }

        [RelayCommand]
        private void DeleteProduct()
        {
            if (SelectedProduct == null) return;
            var name = SelectedProduct.Name;
            _service.Delete(SelectedProduct.Id);
            LoadProducts();
            IsEditing = false;
            SelectedProduct = null;
            StatusMessage = $"🗑 Đã xóa sản phẩm: {name}";
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
            SelectedProduct = null;
            StatusMessage = "";
        }
    }
}
