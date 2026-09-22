using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;
using LibOps.PresentationLayer.Services;

namespace LibOps.PresentationLayer.ViewModels
{
    /// <summary>
    /// ViewModel quản lý mượn và trả sách của độc giả (gồm 2 Sub-Tabs: Đang mượn & Lịch sử đã trả)
    /// </summary>
    public class ReaderLoansViewModel : ViewModelBase
    {
        private readonly ReaderPortalService _readerService;
        private readonly UserSessionDto _currentSession;

        private bool _isActiveLoansSubTabSelected = true;
        private bool _isReturnHistorySubTabSelected = false;
        private ReaderLoanDisplayDto _selectedLoan;
        private ReaderBorrowHistoryDto _selectedReturnHistory;

        public PaginationController<ReaderLoanDisplayDto> ActiveLoansPaging { get; } = new PaginationController<ReaderLoanDisplayDto>(10);
        public ObservableCollection<ReaderLoanDisplayDto> ActiveLoans => ActiveLoansPaging.CurrentPageItems;

        public PaginationController<ReaderBorrowHistoryDto> ReturnHistoriesPaging { get; } = new PaginationController<ReaderBorrowHistoryDto>(10);
        public ObservableCollection<ReaderBorrowHistoryDto> ReturnHistories => ReturnHistoriesPaging.CurrentPageItems;

        public bool IsActiveLoansSubTabSelected
        {
            get => _isActiveLoansSubTabSelected;
            set
            {
                if (SetProperty(ref _isActiveLoansSubTabSelected, value) && value)
                {
                    LoadActiveLoans();
                }
            }
        }

        public bool IsReturnHistorySubTabSelected
        {
            get => _isReturnHistorySubTabSelected;
            set
            {
                if (SetProperty(ref _isReturnHistorySubTabSelected, value) && value)
                {
                    LoadReturnHistory();
                }
            }
        }

        public ReaderLoanDisplayDto SelectedLoan
        {
            get => _selectedLoan;
            set => SetProperty(ref _selectedLoan, value);
        }

        public ReaderBorrowHistoryDto SelectedReturnHistory
        {
            get => _selectedReturnHistory;
            set => SetProperty(ref _selectedReturnHistory, value);
        }

        public ICommand RefreshLoansCommand { get; }
        public ICommand RenewLoanCommand { get; }

        public ReaderLoansViewModel(UserSessionDto session = null)
        {
            _currentSession = session ?? AuthService.CurrentSession;
            _readerService = new ReaderPortalService();

            RefreshLoansCommand = new RelayCommand(ExecuteRefresh);
            RenewLoanCommand = new RelayCommand(ExecuteRenewLoan, () => SelectedLoan != null);

            LoadActiveLoans();
        }

        private void ExecuteRefresh()
        {
            if (IsActiveLoansSubTabSelected)
            {
                LoadActiveLoans();
            }
            else
            {
                LoadReturnHistory();
            }
        }

        private void LoadActiveLoans()
        {
            try
            {
                if (_currentSession?.MemberId == null) return;
                var list = _readerService.GetActiveLoans(_currentSession.MemberId.Value);
                ActiveLoansPaging.SetSource(list);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadActiveLoans Error]: {ex.Message}");
            }
        }

        private void LoadReturnHistory()
        {
            try
            {
                if (_currentSession?.MemberId == null) return;
                var list = _readerService.GetCompletedReturnHistory(_currentSession.MemberId.Value);
                ReturnHistoriesPaging.SetSource(list);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadReturnHistory Error]: {ex.Message}");
            }
        }

        private void ExecuteRenewLoan()
        {
            if (SelectedLoan == null || _currentSession?.MemberId == null) return;

            bool ok = _readerService.RenewLoan(SelectedLoan.BorrowSlipId, _currentSession.MemberId.Value, out DateTime newDueDate, out string errorMsg);
            if (ok)
            {
                DialogService.ShowInformation($"Gia hạn sách thành công!\r\nHạn trả mới: {newDueDate:dd/MM/yyyy}", "Gia Hạn Thành Công");
                LoadActiveLoans();
            }
            else
            {
                DialogService.ShowWarning($"Không thể gia hạn: {errorMsg}", "Lỗi Gia Hạn");
            }
        }
    }
}
