using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    /// <summary>
    /// ViewModel cho Trang Tra Cứu Sách Nâng Cao (OPAC Catalog) bố cục 2 cột chuyên nghiệp
    /// </summary>
    public class ReaderOpacSearchViewModel : ViewModelBase
    {
        private readonly ReaderPortalService _readerService;
        private readonly Action<string, ViewModelBase> _navigateCallback;

        private string _searchKeyword;
        private CategoryCountDto _selectedCategory;
        private bool _onlyAvailable;
        private string _selectedSortOption = "DEFAULT";
        private string _viewMode = "GRID"; // "GRID" hoặc "LIST"
        private bool _isLoading;

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    ExecuteSearch();
                }
            }
        }

        public CategoryCountDto SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    OnPropertyChanged(nameof(SelectedCategoryTitle));
                    ExecuteSearch();
                }
            }
        }

        public string SelectedCategoryTitle => SelectedCategory != null && SelectedCategory.CategoryId > 0 
            ? SelectedCategory.CategoryName 
            : "Tất Cả Thể Loại";

        public bool OnlyAvailable
        {
            get => _onlyAvailable;
            set
            {
                if (SetProperty(ref _onlyAvailable, value))
                {
                    ExecuteSearch();
                }
            }
        }

        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (SetProperty(ref _selectedSortOption, value))
                {
                    ExecuteSearch();
                }
            }
        }

        public string ViewMode
        {
            get => _viewMode;
            set
            {
                if (SetProperty(ref _viewMode, value))
                {
                    OnPropertyChanged(nameof(IsGridView));
                    OnPropertyChanged(nameof(IsListView));
                }
            }
        }

        public bool IsGridView => ViewMode == "GRID";
        public bool IsListView => ViewMode == "LIST";

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool HasNoBooks => !IsLoading && Paging.TotalItems == 0;

        public PaginationController<ReaderBookSearchDto> Paging { get; } = new PaginationController<ReaderBookSearchDto>(8);
        public ObservableCollection<ReaderBookSearchDto> OpacBooks => Paging.CurrentPageItems;
        public ObservableCollection<CategoryCountDto> Categories { get; } = new ObservableCollection<CategoryCountDto>();

        public ICommand SearchCommand { get; }
        public ICommand SelectCategoryCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand SetGridViewCommand { get; }
        public ICommand SetListViewCommand { get; }
        public ICommand ViewBookDetailCommand { get; }

        public ReaderOpacSearchViewModel(
            Action<string, ViewModelBase> navigateCallback, 
            string initialKeyword = null, 
            int? initialCategoryId = null)
        {
            _readerService = new ReaderPortalService();
            _navigateCallback = navigateCallback;

            _searchKeyword = initialKeyword;

            SearchCommand = new RelayCommand(ExecuteSearch);
            SelectCategoryCommand = new RelayCommand<CategoryCountDto>(cat => SelectedCategory = cat);
            ResetFiltersCommand = new RelayCommand(ExecuteResetFilters);
            SetGridViewCommand = new RelayCommand(() => ViewMode = "GRID");
            SetListViewCommand = new RelayCommand(() => ViewMode = "LIST");
            ViewBookDetailCommand = new RelayCommand<ReaderBookSearchDto>(ExecuteViewBookDetail);

            LoadCategories(initialCategoryId);
            ExecuteSearch();
        }

        private void LoadCategories(int? selectCatId)
        {
            try
            {
                Categories.Clear();
                var catCounts = _readerService.GetCategoryWithCounts();
                int totalAllBooks = catCounts.Sum(c => c.BookCount);

                var allCategory = new CategoryCountDto
                {
                    CategoryId = 0,
                    CategoryName = "Tất Cả Sách",
                    BookCount = totalAllBooks
                };
                Categories.Add(allCategory);

                CategoryCountDto targetSelected = allCategory;
                foreach (var cat in catCounts)
                {
                    Categories.Add(cat);
                    if (selectCatId.HasValue && cat.CategoryId == selectCatId.Value)
                    {
                        targetSelected = cat;
                    }
                }

                _selectedCategory = targetSelected;
                OnPropertyChanged(nameof(SelectedCategory));
                OnPropertyChanged(nameof(SelectedCategoryTitle));
            }
            catch
            {
            }
        }

        public void ExecuteSearch()
        {
            try
            {
                IsLoading = true;
                int? catId = SelectedCategory != null && SelectedCategory.CategoryId > 0 
                    ? (int?)SelectedCategory.CategoryId 
                    : null;

                var rawList = _readerService.SearchBooks(SearchKeyword, catId, OnlyAvailable);
                var sortedList = ApplySorting(rawList);

                Paging.SetSource(sortedList);
                OnPropertyChanged(nameof(OpacBooks));
                OnPropertyChanged(nameof(HasNoBooks));
            }
            catch
            {
            }
            finally
            {
                IsLoading = false;
            }
        }

        private List<ReaderBookSearchDto> ApplySorting(List<ReaderBookSearchDto> list)
        {
            if (list == null) return new List<ReaderBookSearchDto>();

            switch (SelectedSortOption)
            {
                case "TITLE_ASC":
                    return list.OrderBy(b => b.Title).ToList();
                case "TITLE_DESC":
                    return list.OrderByDescending(b => b.Title).ToList();
                case "BORROW_DESC":
                    return list.OrderByDescending(b => b.BorrowCount).ThenBy(b => b.Title).ToList();
                case "YEAR_DESC":
                    return list.OrderByDescending(b => b.PublishYear ?? 0).ThenBy(b => b.Title).ToList();
                case "PRICE_ASC":
                    return list.OrderBy(b => b.Price).ToList();
                case "PRICE_DESC":
                    return list.OrderByDescending(b => b.Price).ToList();
                default:
                    return list.OrderByDescending(b => b.AvailableQuantity > 0).ThenByDescending(b => b.BookId).ToList();
            }
        }

        private void ExecuteResetFilters()
        {
            _searchKeyword = string.Empty;
            _selectedCategory = Categories.FirstOrDefault();
            _onlyAvailable = false;
            _selectedSortOption = "DEFAULT";

            OnPropertyChanged(nameof(SearchKeyword));
            OnPropertyChanged(nameof(SelectedCategory));
            OnPropertyChanged(nameof(SelectedCategoryTitle));
            OnPropertyChanged(nameof(OnlyAvailable));
            OnPropertyChanged(nameof(SelectedSortOption));

            ExecuteSearch();
        }

        private void ExecuteViewBookDetail(ReaderBookSearchDto book)
        {
            if (book == null) return;
            if (_navigateCallback != null)
            {
                var detailVm = new ReaderBookDetailViewModel(book, () => _navigateCallback("OpacSearch", this));
                _navigateCallback("BookDetail", detailVm);
            }
        }
    }
}
