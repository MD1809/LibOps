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
    /// ViewModel cho Trang Chủ Độc Giả (Discovery Hub) phong cách đa tầng hiện đại (Top mượn, Sách mới, Thể loại nổi bật)
    /// </summary>
    public class ReaderHomeViewModel : ViewModelBase
    {
        private readonly ReaderPortalService _readerService;
        private readonly Action<string, ViewModelBase> _navigateCallback;

        private string _quickSearchKeyword;

        public string QuickSearchKeyword
        {
            get => _quickSearchKeyword;
            set => SetProperty(ref _quickSearchKeyword, value);
        }

        public ObservableCollection<ReaderBookSearchDto> TrendingBooks { get; } = new ObservableCollection<ReaderBookSearchDto>();
        public ObservableCollection<ReaderBookSearchDto> NewArrivals { get; } = new ObservableCollection<ReaderBookSearchDto>();
        public ObservableCollection<CategoryCountDto> Categories { get; } = new ObservableCollection<CategoryCountDto>();

        public ICommand QuickSearchCommand { get; }
        public ICommand SearchKeywordTagCommand { get; }
        public ICommand ViewBookDetailCommand { get; }
        public ICommand ExploreCategoryCommand { get; }
        public ICommand ViewAllTrendingCommand { get; }
        public ICommand ViewAllNewArrivalsCommand { get; }
        public ICommand NavigateToCatalogCommand { get; }

        public ReaderHomeViewModel(Action<string, ViewModelBase> navigateCallback)
        {
            _readerService = new ReaderPortalService();
            _navigateCallback = navigateCallback;

            QuickSearchCommand = new RelayCommand(ExecuteQuickSearch);
            SearchKeywordTagCommand = new RelayCommand<string>(tag => ExecuteSearchWithKeyword(tag));
            ViewBookDetailCommand = new RelayCommand<ReaderBookSearchDto>(ExecuteViewBookDetail);
            ExploreCategoryCommand = new RelayCommand<CategoryCountDto>(ExecuteExploreCategory);
            ViewAllTrendingCommand = new RelayCommand(() => NavigateToOpacCatalog(null, null, "BORROW_DESC"));
            ViewAllNewArrivalsCommand = new RelayCommand(() => NavigateToOpacCatalog(null, null, "DEFAULT"));
            NavigateToCatalogCommand = new RelayCommand(() => NavigateToOpacCatalog(null, null, null));

            LoadDiscoveryData();
        }

        private void LoadDiscoveryData()
        {
            try
            {
                // 1. Top sách mượn nhiều nhất
                TrendingBooks.Clear();
                var trending = _readerService.GetTopTrendingBooks(6);
                foreach (var b in trending) TrendingBooks.Add(b);

                // 2. Sách mới nhập về
                NewArrivals.Clear();
                var newBooks = _readerService.GetNewArrivalBooks(6);
                foreach (var b in newBooks) NewArrivals.Add(b);

                // 3. Danh mục thể loại
                Categories.Clear();
                var catCounts = _readerService.GetCategoryWithCounts();
                foreach (var c in catCounts) Categories.Add(c);
            }
            catch
            {
            }
        }

        private void ExecuteQuickSearch()
        {
            if (!string.IsNullOrWhiteSpace(QuickSearchKeyword))
            {
                NavigateToOpacCatalog(QuickSearchKeyword.Trim(), null, null);
            }
            else
            {
                NavigateToOpacCatalog(null, null, null);
            }
        }

        private void ExecuteSearchWithKeyword(string keyword)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                NavigateToOpacCatalog(keyword.Trim(), null, null);
            }
        }

        private void ExecuteExploreCategory(CategoryCountDto category)
        {
            if (category != null && category.CategoryId > 0)
            {
                NavigateToOpacCatalog(null, category.CategoryId, null);
            }
            else
            {
                NavigateToOpacCatalog(null, null, null);
            }
        }

        private void NavigateToOpacCatalog(string keyword, int? categoryId, string sortOption)
        {
            if (_navigateCallback != null)
            {
                var opacVm = new ReaderOpacSearchViewModel(_navigateCallback, keyword, categoryId);
                if (!string.IsNullOrWhiteSpace(sortOption))
                {
                    opacVm.SelectedSortOption = sortOption;
                }
                _navigateCallback("OpacSearch", opacVm);
            }
        }

        private void ExecuteViewBookDetail(ReaderBookSearchDto book)
        {
            if (book == null) return;
            if (_navigateCallback != null)
            {
                var detailVm = new ReaderBookDetailViewModel(book, () => _navigateCallback("Home", this));
                _navigateCallback("BookDetail", detailVm);
            }
        }
    }
}
