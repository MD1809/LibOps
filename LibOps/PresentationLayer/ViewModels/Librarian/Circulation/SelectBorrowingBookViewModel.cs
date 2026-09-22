using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    /// <summary>
    /// ViewModel cho hộp thoại Tra cứu sách mượn theo Độc Giả (hỗ trợ tìm kiếm khi độc giả làm mất sách / không có mã vạch)
    /// </summary>
    public class SelectBorrowingBookViewModel : ViewModelBase
    {
        private readonly FineCalculationService _fineService;
        private string _searchKeyword;
        private ReturnBookLookupDto _selectedBook;
        private string _selectedCondition = "AVAILABLE";
        private string _statusMessage;
        private bool _hasResults;
        private bool _isLoading;

        public event Action<bool> RequestClose;

        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        public ObservableCollection<ReturnBookLookupDto> BorrowingBooks { get; }

        public ReturnBookLookupDto SelectedBook
        {
            get => _selectedBook;
            set
            {
                if (SetProperty(ref _selectedBook, value))
                {
                    ((RelayCommand)SelectReturnCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)ReportLostCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string SelectedCondition
        {
            get => _selectedCondition;
            set => SetProperty(ref _selectedCondition, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool HasResults
        {
            get => _hasResults;
            set => SetProperty(ref _hasResults, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand SearchCommand { get; }
        public ICommand SelectReturnCommand { get; }
        public ICommand ReportLostCommand { get; }
        public ICommand CancelCommand { get; }

        public SelectBorrowingBookViewModel()
        {
            _fineService = new FineCalculationService();
            BorrowingBooks = new ObservableCollection<ReturnBookLookupDto>();

            SearchCommand = new RelayCommand(ExecuteSearch);
            SelectReturnCommand = new RelayCommand(ExecuteSelectReturn, () => SelectedBook != null);
            ReportLostCommand = new RelayCommand(ExecuteReportLost, () => SelectedBook != null);
            CancelCommand = new RelayCommand(ExecuteCancel);

            StatusMessage = "Nhập Mã thẻ, Họ tên, Số điện thoại hoặc CCCD của độc giả để tìm kiếm.";
        }

        private void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                StatusMessage = "Vui lòng nhập từ khóa tìm kiếm (Mã thẻ, Tên, SĐT, CCCD).";
                BorrowingBooks.Clear();
                HasResults = false;
                return;
            }

            IsLoading = true;
            try
            {
                BorrowingBooks.Clear();
                var results = _fineService.GetActiveBorrowingBooksByMember(SearchKeyword.Trim());
                if (results != null && results.Count > 0)
                {
                    foreach (var book in results)
                    {
                        BorrowingBooks.Add(book);
                    }
                    HasResults = true;
                    SelectedBook = BorrowingBooks[0];
                    StatusMessage = $"Tìm thấy {results.Count} cuốn sách đang mượn của độc giả.";
                }
                else
                {
                    HasResults = false;
                    SelectedBook = null;
                    StatusMessage = "Không tìm thấy cuốn sách nào đang được mượn bởi độc giả này.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi khi tìm kiếm: " + ex.Message;
                HasResults = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteSelectReturn()
        {
            if (SelectedBook == null) return;
            SelectedCondition = "AVAILABLE";
            RequestClose?.Invoke(true);
        }

        private void ExecuteReportLost()
        {
            if (SelectedBook == null) return;
            SelectedCondition = "LOST";
            RequestClose?.Invoke(true);
        }

        private void ExecuteCancel()
        {
            RequestClose?.Invoke(false);
        }
    }
}
