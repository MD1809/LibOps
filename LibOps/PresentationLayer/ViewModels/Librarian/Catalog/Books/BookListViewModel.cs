using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class BookListViewModel : ViewModelBase
    {
        private readonly BookService _bookService;
        private readonly BookMetadataService _metaService;

        private string _searchKeyword;
        private CategoryEntity _selectedCategory;
        private BookGridDisplayDto _selectedBook;
        private bool _isLoading;

        public PaginationController<BookGridDisplayDto> Paging { get; } = new PaginationController<BookGridDisplayDto>(10);
        public ObservableCollection<BookGridDisplayDto> Books => Paging.CurrentPageItems;
        public ObservableCollection<CategoryEntity> Categories { get; }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    LoadBooks();
                }
            }
        }

        public CategoryEntity SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    LoadBooks();
                }
            }
        }

        public BookGridDisplayDto SelectedBook
        {
            get => _selectedBook;
            set
            {
                if (SetProperty(ref _selectedBook, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand AddBookCommand { get; }
        public ICommand ViewDetailCommand { get; }
        public ICommand EditBookCommand { get; }
        public ICommand DeleteBookCommand { get; }
        public ICommand ManageCopiesCommand { get; }

        public BookListViewModel()
        {
            _bookService = new BookService();
            _metaService = new BookMetadataService();

            Categories = new ObservableCollection<CategoryEntity>();

            RefreshCommand = new RelayCommand(LoadData);
            SearchCommand = new RelayCommand(LoadBooks);
            AddBookCommand = new RelayCommand(ExecuteAddBook);
            ViewDetailCommand = new RelayCommand(ExecuteViewDetail, () => SelectedBook != null);
            EditBookCommand = new RelayCommand(ExecuteEditBook, () => SelectedBook != null);
            DeleteBookCommand = new RelayCommand(ExecuteDeleteBook, () => SelectedBook != null);
            ManageCopiesCommand = new RelayCommand(ExecuteManageCopies, () => SelectedBook != null);

            LoadData();
        }

        private void ExecuteViewDetail()
        {
            if (SelectedBook == null) return;
            var vm = new BookDetailDialogViewModel(SelectedBook.BookId);
            if (DialogService.ShowBookDetailDialog(vm) == true)
            {
                LoadBooks();
            }
        }

        private void ExecuteAddBook()
        {
            var vm = new BookAddEditViewModel(0);
            if (DialogService.ShowBookAddEditDialog(vm) == true)
            {
                LoadBooks();
            }
        }

        private void ExecuteEditBook()
        {
            if (SelectedBook == null) return;
            var vm = new BookAddEditViewModel(SelectedBook.BookId);
            if (DialogService.ShowBookAddEditDialog(vm) == true)
            {
                LoadBooks();
            }
        }

        private void ExecuteDeleteBook()
        {
            if (SelectedBook == null) return;

            if (!DialogService.ShowConfirmation($"Bạn có chắc chắn muốn xóa đầu sách '{SelectedBook.Title}' không?", "Xác Nhận Xóa Sách"))
            {
                return;
            }

            bool ok = _bookService.DeleteBook(SelectedBook.BookId, out string error);
            if (ok)
            {
                DialogService.ShowInformation("Đã xóa đầu sách thành công!");
                LoadBooks();
            }
            else
            {
                DialogService.ShowError("Xóa đầu sách thất bại: " + error);
            }
        }

        private void ExecuteManageCopies()
        {
            if (SelectedBook == null) return;
            // Mở màn hình quản lý bản sao sách
            int bookId = SelectedBook.BookId;
            var vm = new BookCopyBarcodeViewModel(bookId);
            DialogService.ShowBookCopyManagementDialog(vm);
            LoadBooks();
        }

        public void LoadData()
        {
            LoadCategories();
            LoadBooks();
        }

        private void LoadCategories()
        {
            try
            {
                Categories.Clear();
                Categories.Add(new CategoryEntity { CategoryId = 0, CategoryName = "-- Tất cả thể loại --" });
                var list = _metaService.GetAllCategories();
                if (list != null)
                {
                    foreach (var cat in list)
                    {
                        Categories.Add(cat);
                    }
                }
                SelectedCategory = Categories.FirstOrDefault();
            }
            catch
            {
            }
        }

        public void LoadBooks()
        {
            try
            {
                IsLoading = true;
                int? catId = (SelectedCategory != null && SelectedCategory.CategoryId > 0) ? (int?)SelectedCategory.CategoryId : null;
                var list = _bookService.GetAllBooks(SearchKeyword, catId);
                Paging.SetSource(list);
            }
            catch
            {
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
