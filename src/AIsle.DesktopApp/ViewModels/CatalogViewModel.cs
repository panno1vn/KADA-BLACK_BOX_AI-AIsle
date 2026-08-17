using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIsle.DesktopApp.ViewModels
{
    public partial class CatalogViewModel : ViewModelBase
    {
        public override string Title => "Quản lý Sản phẩm";

        private readonly Services.CatalogService _service;

        public ObservableCollection<Services.Product> Products { get; } = new();

        [ObservableProperty] private Services.Product? _selectedProduct;
        [ObservableProperty] private string _editId = "";
        [ObservableProperty] private string _editName = "";
        [ObservableProperty] private string _editCategory = "";
        [ObservableProperty] private string _editShelf = "";
        [ObservableProperty] private string _editPrice = "";
        [ObservableProperty] private bool _isEditing;
        [ObservableProperty] private string _statusMessage = "";

        public CatalogViewModel(Services.CatalogService service)
        {
            _service = service;
            LoadProducts();
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
                EditId = value.Id;
                EditName = value.Name;
                EditCategory = value.Category;
                EditShelf = value.Shelf;
                EditPrice = value.Price.ToString("F0");
                IsEditing = true;
            }
        }

        [RelayCommand]
        private void NewProduct()
        {
            EditId = "";
            EditName = "";
            EditCategory = "";
            EditShelf = "";
            EditPrice = "";
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
