using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.CommonUtilities;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class ReportAnalyticsViewModel : ViewModelBase
    {
        private readonly ReportService _reportService;

        private bool _isOverdueTabSelected = true;
        private bool _isCashFlowTabSelected;

        // Báo cáo quá hạn
        private string _overdueSearchKeyword;
        private int _totalOverdueCount;
        private decimal _totalEstimatedFine;
        private int _totalOverdueMembers;

        // Báo cáo dòng tiền
        private DateTime _fromDate = DateTime.Today.AddDays(-30);
        private DateTime _toDate = DateTime.Today;
        private string _selectedCashFlowType = "ALL";
        private string _cashFlowSearchKeyword;

        private decimal _totalDepositInitial;
        private decimal _totalDepositTopUp;
        private decimal _totalFineCollected;
        private decimal _totalDepositRefunded;
        private decimal _totalInflow;
        private decimal _totalOutflow;
        private decimal _netCashFlow;
        private decimal _currentDepositFundBalance;

        private bool _isLoading;

        private List<OverdueReportDto> _allOverdueItems = new List<OverdueReportDto>();
        private List<OverdueReportDto> _filteredOverdueList = new List<OverdueReportDto>();
        private List<FinancialTransactionDisplayDto> _allCashFlowItems = new List<FinancialTransactionDisplayDto>();
        private List<FinancialTransactionDisplayDto> _filteredCashFlowList = new List<FinancialTransactionDisplayDto>();

        public PaginationController<OverdueReportDto> OverduePaging { get; } = new PaginationController<OverdueReportDto>(10);
        public ObservableCollection<OverdueReportDto> OverdueList => OverduePaging.CurrentPageItems;

        public PaginationController<FinancialTransactionDisplayDto> CashFlowPaging { get; } = new PaginationController<FinancialTransactionDisplayDto>(10);
        public ObservableCollection<FinancialTransactionDisplayDto> CashFlowTransactions => CashFlowPaging.CurrentPageItems;

        public bool IsOverdueTabSelected
        {
            get => _isOverdueTabSelected;
            set
            {
                if (SetProperty(ref _isOverdueTabSelected, value) && value)
                {
                    LoadOverdueReport();
                }
            }
        }

        public bool IsCashFlowTabSelected
        {
            get => _isCashFlowTabSelected;
            set
            {
                if (SetProperty(ref _isCashFlowTabSelected, value) && value)
                {
                    LoadCashFlowReport();
                }
            }
        }

        #region Thuộc tính Báo Cáo Quá Hạn

        public string OverdueSearchKeyword
        {
            get => _overdueSearchKeyword;
            set
            {
                if (SetProperty(ref _overdueSearchKeyword, value))
                {
                    ApplyOverdueFilter();
                }
            }
        }

        public int TotalOverdueCount
        {
            get => _totalOverdueCount;
            set => SetProperty(ref _totalOverdueCount, value);
        }

        public decimal TotalEstimatedFine
        {
            get => _totalEstimatedFine;
            set => SetProperty(ref _totalEstimatedFine, value);
        }

        public int TotalOverdueMembers
        {
            get => _totalOverdueMembers;
            set => SetProperty(ref _totalOverdueMembers, value);
        }

        #endregion

        #region Thuộc tính Báo Cáo Dòng Tiền

        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value))
                {
                    LoadCashFlowReport();
                }
            }
        }

        public DateTime ToDate
        {
            get => _toDate;
            set
            {
                if (SetProperty(ref _toDate, value))
                {
                    LoadCashFlowReport();
                }
            }
        }

        public string SelectedCashFlowType
        {
            get => _selectedCashFlowType;
            set
            {
                if (SetProperty(ref _selectedCashFlowType, value))
                {
                    ApplyCashFlowFilter();
                }
            }
        }

        public string CashFlowSearchKeyword
        {
            get => _cashFlowSearchKeyword;
            set
            {
                if (SetProperty(ref _cashFlowSearchKeyword, value))
                {
                    ApplyCashFlowFilter();
                }
            }
        }

        public decimal TotalDepositInitial
        {
            get => _totalDepositInitial;
            set => SetProperty(ref _totalDepositInitial, value);
        }

        public decimal TotalDepositTopUp
        {
            get => _totalDepositTopUp;
            set => SetProperty(ref _totalDepositTopUp, value);
        }

        public decimal TotalFineCollected
        {
            get => _totalFineCollected;
            set => SetProperty(ref _totalFineCollected, value);
        }

        public decimal TotalDepositRefunded
        {
            get => _totalDepositRefunded;
            set => SetProperty(ref _totalDepositRefunded, value);
        }

        public decimal TotalInflow
        {
            get => _totalInflow;
            set => SetProperty(ref _totalInflow, value);
        }

        public decimal TotalOutflow
        {
            get => _totalOutflow;
            set => SetProperty(ref _totalOutflow, value);
        }

        public decimal NetCashFlow
        {
            get => _netCashFlow;
            set => SetProperty(ref _netCashFlow, value);
        }

        public decimal CurrentDepositFundBalance
        {
            get => _currentDepositFundBalance;
            set => SetProperty(ref _currentDepositFundBalance, value);
        }

        #endregion

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand OpenExpiredDebtSettlementCommand { get; }

        public ReportAnalyticsViewModel()
        {
            _reportService = new ReportService();

            RefreshCommand = new RelayCommand(RefreshCurrentTab);
            ExportCsvCommand = new RelayCommand(ExecuteExportCsv);
            OpenExpiredDebtSettlementCommand = new RelayCommand(ExecuteOpenExpiredDebtSettlement);

            LoadOverdueReport();
        }

        private void ExecuteOpenExpiredDebtSettlement()
        {
            var vm = new ExpiredDebtViewModel();
            DialogService.ShowExpiredDebtSettlementDialog(vm);
            RefreshCurrentTab();
        }

        private void RefreshCurrentTab()
        {
            if (IsOverdueTabSelected) LoadOverdueReport();
            else if (IsCashFlowTabSelected) LoadCashFlowReport();
        }

        #region Tải & Lọc Báo Cáo Quá Hạn

        private void LoadOverdueReport()
        {
            try
            {
                IsLoading = true;
                _allOverdueItems = _reportService.GetOverdueReport() ?? new List<OverdueReportDto>();
                ApplyOverdueFilter();
            }
            catch (Exception ex)
            {
                DialogService.ShowError("Lỗi tải báo cáo phiếu mượn quá hạn: " + ex.Message, "Lỗi");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyOverdueFilter()
        {
            var filtered = _allOverdueItems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(OverdueSearchKeyword))
            {
                string kw = OverdueSearchKeyword.Trim().ToLower();
                filtered = filtered.Where(x =>
                    (!string.IsNullOrEmpty(x.SlipCode) && x.SlipCode.ToLower().Contains(kw)) ||
                    (!string.IsNullOrEmpty(x.MemberCardCode) && x.MemberCardCode.ToLower().Contains(kw)) ||
                    (!string.IsNullOrEmpty(x.MemberFullName) && x.MemberFullName.ToLower().Contains(kw)) ||
                    (!string.IsNullOrEmpty(x.PhoneNumber) && x.PhoneNumber.Contains(kw)) ||
                    (!string.IsNullOrEmpty(x.BookTitle) && x.BookTitle.ToLower().Contains(kw)) ||
                    (!string.IsNullOrEmpty(x.Barcode) && x.Barcode.ToLower().Contains(kw))
                );
            }

            var resultList = filtered.ToList();
            int seq = 1;
            foreach (var item in resultList)
            {
                item.SequenceNumber = seq++;
            }

            _filteredOverdueList = resultList;
            OverduePaging.SetSource(resultList);

            TotalOverdueCount = resultList.Count;
            TotalEstimatedFine = resultList.Sum(x => x.EstimatedFine);
            TotalOverdueMembers = resultList.Select(x => x.MemberCardCode).Distinct().Count();
        }

        #endregion

        #region Tải & Lọc Báo Cáo Dòng Tiền

        private void LoadCashFlowReport()
        {
            try
            {
                IsLoading = true;

                // Tóm tắt chỉ số tài chính
                var summary = _reportService.GetRevenueReportSummary(FromDate, ToDate);
                if (summary != null)
                {
                    TotalDepositInitial = summary.TotalDepositInitial;
                    TotalDepositTopUp = summary.TotalDepositTopUp;
                    TotalFineCollected = summary.TotalFineCollected;
                    TotalDepositRefunded = summary.TotalDepositRefunded;
                    TotalInflow = summary.TotalInflow;
                    TotalOutflow = summary.TotalOutflow;
                    NetCashFlow = summary.NetCashFlow;
                    CurrentDepositFundBalance = summary.CurrentDepositFundBalance;
                }

                _allCashFlowItems = _reportService.GetFinancialTransactions(FromDate, ToDate, "ALL") ?? new List<FinancialTransactionDisplayDto>();
                ApplyCashFlowFilter();
            }
            catch (Exception ex)
            {
                DialogService.ShowError("Lỗi tải báo cáo dòng tiền: " + ex.Message, "Lỗi");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyCashFlowFilter()
        {
            var filtered = _allCashFlowItems.AsEnumerable();

            // Lọc theo loại dòng tiền
            if (!string.IsNullOrEmpty(SelectedCashFlowType) && SelectedCashFlowType != "ALL")
            {
                filtered = filtered.Where(x => x.RawTransactionType == SelectedCashFlowType);
            }

            // Lọc theo từ khóa
            if (!string.IsNullOrWhiteSpace(CashFlowSearchKeyword))
            {
                string kw = CashFlowSearchKeyword.Trim().ToLower();
                filtered = filtered.Where(x =>
                    (!string.IsNullOrEmpty(x.Code) && x.Code.ToLower().Contains(kw)) ||
                    (!string.IsNullOrEmpty(x.MemberCardCode) && x.MemberCardCode.ToLower().Contains(kw)) ||
                    (!string.IsNullOrEmpty(x.MemberFullName) && x.MemberFullName.ToLower().Contains(kw)) ||
                    (!string.IsNullOrEmpty(x.TransactionTypeDisplay) && x.TransactionTypeDisplay.ToLower().Contains(kw)) ||
                    (!string.IsNullOrEmpty(x.Notes) && x.Notes.ToLower().Contains(kw))
                );
            }

            var resultList = filtered.ToList();
            int seq = 1;
            foreach (var item in resultList)
            {
                item.SequenceNumber = seq++;
            }

            _filteredCashFlowList = resultList;
            CashFlowPaging.SetSource(resultList);
        }

        #endregion

        #region Xuất Báo Cáo Excel (.xlsx)

        private void ExecuteExportCsv()
        {
            try
            {
                if (IsOverdueTabSelected)
                {
                    ExcelExportUtility.ExportOverdueReportToExcel(_filteredOverdueList);
                }
                else
                {
                    ExcelExportUtility.ExportCashFlowReportToExcel(
                        _filteredCashFlowList,
                        FromDate,
                        ToDate,
                        TotalInflow,
                        TotalOutflow,
                        NetCashFlow);
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowError("Lỗi xuất báo cáo Excel: " + ex.Message, "Lỗi");
            }
        }

        #endregion
    }
}
