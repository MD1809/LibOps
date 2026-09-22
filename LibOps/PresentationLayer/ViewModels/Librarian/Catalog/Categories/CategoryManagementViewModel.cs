using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class CategoryManagementViewModel : ViewModelBase
    {
        private readonly BookMetadataService _metadataService;
        private CategoryEntity _selectedCategory;
        private string _categoryName;
        private string _categoryDescription;
        private string _statusMessage;
        private bool _isStatusError;

        public CategoryEntity SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value) && value != null)
                {
                    CategoryName = value.CategoryName;
                    CategoryDescription = value.Description;
                }
            }
        }

        public string CategoryName { get => _categoryName; set => SetProperty(ref _categoryName, value); }
        public string CategoryDescription { get => _categoryDescription; set => SetProperty(ref _categoryDescription, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public bool IsStatusError { get => _isStatusError; set => SetProperty(ref _isStatusError, value); }

        public PaginationController<CategoryEntity> Paging { get; } = new PaginationController<CategoryEntity>(10);
        public ObservableCollection<CategoryEntity> Categories => Paging.CurrentPageItems;

        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand RefreshCommand { get; }

        public CategoryManagementViewModel()
        {
            _metadataService = new BookMetadataService();

            SaveCommand = new RelayCommand(SaveCategory);
            DeleteCommand = new RelayCommand(DeleteCategory);
            NewCommand = new RelayCommand(NewCategory);
            RefreshCommand = new RelayCommand(LoadCategories);

            LoadCategories();
        }

        public void LoadCategories()
        {
            try
            {
                var list = _metadataService.GetAllCategories();
                Paging.SetSource(list);
                SetStatus($"Đã tải {Paging.TotalItems} thể loại sách.", false);
            }
            catch (Exception ex)
            {
                SetStatus("Lỗi tải danh mục thể loại: " + ex.Message, true);
            }
        }

        private void NewCategory()
        {
            SelectedCategory = null;
            CategoryName = string.Empty;
            CategoryDescription = string.Empty;
            SetStatus("Sẵn sàng thêm mới thể loại.", false);
        }

        private void SaveCategory()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CategoryName))
                {
                    SetStatus("Vui lòng nhập tên thể loại!", true);
                    return;
                }

                if (CategoryName.Trim().Length > 100)
                {
                    SetStatus("Tên thể loại không được vượt quá 100 ký tự!", true);
                    return;
                }

                var cat = new CategoryEntity
                {
                    CategoryId = SelectedCategory?.CategoryId ?? 0,
                    CategoryName = CategoryName.Trim(),
                    Description = CategoryDescription?.Trim()
                };

                bool ok = _metadataService.SaveCategory(cat, out string error);
                if (ok)
                {
                    SetStatus("Lưu thông tin thể loại thành công!", false);
                    LoadCategories();
                    NewCategory();
                }
                else
                {
                    SetStatus(error, true);
                }
            }
            catch (Exception ex)
            {
                SetStatus("Lỗi khi lưu thể loại: " + ex.Message, true);
            }
        }

        private void DeleteCategory()
        {
            try
            {
                if (SelectedCategory == null)
                {
                    SetStatus("Vui lòng chọn một thể loại cần xóa!", true);
                    return;
                }

                if (!DialogService.ShowConfirmation($"Xác nhận xóa thể loại '{SelectedCategory.CategoryName}'?", "Xác Nhận Xóa"))
                {
                    return;
                }

                bool ok = _metadataService.DeleteCategory(SelectedCategory.CategoryId, out string error);
                if (ok)
                {
                    SetStatus("Đã xóa thể loại thành công!", false);
                    LoadCategories();
                    NewCategory();
                }
                else
                {
                    SetStatus(error, true);
                }
            }
            catch (Exception ex)
            {
                SetStatus("Lỗi khi xóa thể loại: " + ex.Message, true);
            }
        }

        private void SetStatus(string msg, bool isError)
        {
            StatusMessage = msg;
            IsStatusError = isError;
        }
    }
}
