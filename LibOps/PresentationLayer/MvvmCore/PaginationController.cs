using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace LibOps.PresentationLayer.MvvmCore
{
    /// <summary>
    /// Bộ điều khiển phân trang tổng quát (Generic Pagination) trên bộ nhớ cho danh sách dữ liệu trong WPF MVVM
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của các phần tử trong danh sách</typeparam>
    public class PaginationController<T> : ViewModelBase
    {
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalItems = 0;
        private List<T> _sourceItems = new List<T>();

        public ObservableCollection<T> CurrentPageItems { get; } = new ObservableCollection<T>();

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                int maxPage = Math.Max(1, TotalPages);
                int target = Math.Max(1, Math.Min(value, maxPage));
                if (SetProperty(ref _currentPage, target))
                {
                    RefreshPage();
                    UpdateProperties();
                }
            }
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value < 1) value = 10;
                if (SetProperty(ref _pageSize, value))
                {
                    _currentPage = 1;
                    OnPropertyChanged(nameof(CurrentPage));
                    RefreshPage();
                    UpdateProperties();
                }
            }
        }

        public int TotalItems
        {
            get => _totalItems;
            private set => SetProperty(ref _totalItems, value);
        }

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / (PageSize > 0 ? PageSize : 10));

        public bool CanGoPrevious => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;

        public int StartItemIndex => TotalItems == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
        public int EndItemIndex => Math.Min(CurrentPage * PageSize, TotalItems);

        public string PageSummaryText => TotalItems == 0
            ? "Không có dữ liệu"
            : $"Hiển thị {StartItemIndex} - {EndItemIndex} trên tổng số {TotalItems} bản ghi";

        public string PageDisplayInfo => $"Trang {CurrentPage} / {Math.Max(1, TotalPages)}";

        public ICommand FirstPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }

        public PaginationController(int pageSize = 10)
        {
            _pageSize = pageSize;
            FirstPageCommand = new RelayCommand(() => CurrentPage = 1, () => CanGoPrevious);
            PreviousPageCommand = new RelayCommand(() => CurrentPage--, () => CanGoPrevious);
            NextPageCommand = new RelayCommand(() => CurrentPage++, () => CanGoNext);
            LastPageCommand = new RelayCommand(() => CurrentPage = TotalPages, () => CanGoNext);
        }

        /// <summary>
        /// Nạp danh sách dữ liệu nguồn mới, đặt lại trang hiện tại về 1 và làm mới danh sách hiển thị
        /// </summary>
        public void SetSource(IEnumerable<T> source)
        {
            _sourceItems = source != null ? source.ToList() : new List<T>();
            TotalItems = _sourceItems.Count;
            _currentPage = 1;
            OnPropertyChanged(nameof(CurrentPage));
            RefreshPage();
            UpdateProperties();
        }

        private void RefreshPage()
        {
            CurrentPageItems.Clear();
            if (_sourceItems == null || !_sourceItems.Any()) return;

            var paged = _sourceItems.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
            foreach (var item in paged)
            {
                CurrentPageItems.Add(item);
            }
        }

        private void UpdateProperties()
        {
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(StartItemIndex));
            OnPropertyChanged(nameof(EndItemIndex));
            OnPropertyChanged(nameof(PageSummaryText));
            OnPropertyChanged(nameof(PageDisplayInfo));

            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }
}
