using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;
using LibOps.PresentationLayer.Services;

namespace LibOps.PresentationLayer.ViewModels
{
    public class ExpiredDebtViewModel : ViewModelBase
    {
        private readonly MemberService _memberService;
        private System.Collections.Generic.List<ExpiredMemberDebtDto> _allDebtors = new System.Collections.Generic.List<ExpiredMemberDebtDto>();
        private ObservableCollection<ExpiredMemberDebtDto> _debtors;
        private ExpiredMemberDebtDto _selectedDebtor;
        private string _searchKeyword;
        private string _selectedFilterStatus = "ALL";
        private bool _isLoading;
        private int _totalExpiredCount;
        private decimal _totalDebtAmount;
        private decimal _totalPossibleSettlement;

        public ObservableCollection<ExpiredMemberDebtDto> Debtors
        {
            get => _debtors;
            set => SetProperty(ref _debtors, value);
        }

        public ExpiredMemberDebtDto SelectedDebtor
        {
            get => _selectedDebtor;
            set => SetProperty(ref _selectedDebtor, value);
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string SelectedFilterStatus
        {
            get => _selectedFilterStatus;
            set
            {
                if (SetProperty(ref _selectedFilterStatus, value))
                {
                    ApplyFilter();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int TotalExpiredCount
        {
            get => _totalExpiredCount;
            set => SetProperty(ref _totalExpiredCount, value);
        }

        public decimal TotalDebtAmount
        {
            get => _totalDebtAmount;
            set => SetProperty(ref _totalDebtAmount, value);
        }

        public decimal TotalPossibleSettlement
        {
            get => _totalPossibleSettlement;
            set => SetProperty(ref _totalPossibleSettlement, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand SettleSingleCommand { get; }
        public ICommand SettleAllCommand { get; }

        public ExpiredDebtViewModel()
        {
            _memberService = new MemberService();
            Debtors = new ObservableCollection<ExpiredMemberDebtDto>();

            RefreshCommand = new RelayCommand(LoadData);
            SettleSingleCommand = new RelayCommand<ExpiredMemberDebtDto>(ExecuteSettleSingle);
            SettleAllCommand = new RelayCommand(ExecuteSettleAll, () => _allDebtors.Any(x => x.CanSettleDebt));

            LoadData();
        }

        public void LoadData()
        {
            IsLoading = true;
            try
            {
                _allDebtors = _memberService.GetExpiredMembersWithDebt() ?? new System.Collections.Generic.List<ExpiredMemberDebtDto>();

                TotalExpiredCount = _allDebtors.Count;
                TotalDebtAmount = _allDebtors.Sum(x => x.TotalDebt);
                TotalPossibleSettlement = _allDebtors.Where(x => x.CanSettleDebt).Sum(x => x.SettlementAmount);

                ApplyFilter();
                ((RelayCommand)SettleAllCommand)?.RaiseCanExecuteChanged();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            Debtors.Clear();
            var filtered = _allDebtors.AsEnumerable();

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(SelectedFilterStatus) && SelectedFilterStatus != "ALL")
            {
                switch (SelectedFilterStatus)
                {
                    case "ELIGIBLE":
                        filtered = filtered.Where(x => x.CanSettleDebt);
                        break;
                    case "BORROWING":
                        filtered = filtered.Where(x => x.CurrentlyBorrowingCount > 0);
                        break;
                    case "NO_DEPOSIT":
                        filtered = filtered.Where(x => x.DepositBalance <= 0);
                        break;
                }
            }

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                string kw = SearchKeyword.Trim().ToLower();
                filtered = filtered.Where(x =>
                    (!string.IsNullOrEmpty(x.MemberCardCode) && x.MemberCardCode.ToLower().Contains(kw)) ||
                    (!string.IsNullOrEmpty(x.FullName) && x.FullName.ToLower().Contains(kw)) ||
                    (!string.IsNullOrEmpty(x.PhoneNumber) && x.PhoneNumber.Contains(kw))
                );
            }

            int seq = 1;
            foreach (var item in filtered.ToList())
            {
                item.SequenceNumber = seq++;
                Debtors.Add(item);
            }
        }

        private void ExecuteSettleSingle(ExpiredMemberDebtDto debtor)
        {
            if (debtor == null) return;

            // Chốt chặn 1: Nếu độc giả vẫn đang mượn sách -> Tuyệt đối không cho cấn trừ
            if (debtor.CurrentlyBorrowingCount > 0)
            {
                DialogService.ShowWarning(
                    $"Độc giả {debtor.MemberCardCode} - {debtor.FullName} hiện vẫn đang giữ {debtor.CurrentlyBorrowingCount} cuốn sách chưa trả!\r\n\r\n" +
                    "Hệ thống khóa chức năng cấn trừ cọc để đảm bảo tài sản thế chân của thư viện. Thủ thư vui lòng yêu cầu trả sách hoặc lập phiếu báo mất trước!",
                    "Khóa Thao Tác - Chưa Trả Sách");
                return;
            }

            if (debtor.DepositBalance <= 0)
            {
                DialogService.ShowWarning(
                    $"Độc giả {debtor.MemberCardCode} có số dư cọc bằng 0 VNĐ. Không thể thực hiện cấn trừ!",
                    "Không Có Tiền Cọc");
                return;
            }

            string confirmMsg = $"Bạn có chắc chắn muốn cấn trừ nợ phạt cho độc giả sau:\r\n\r\n" +
                               $"Độc giả: {debtor.FullName} ({debtor.MemberCardCode})\r\n" +
                               $"Hạn thẻ: {debtor.ExpiryDate:dd/MM/yyyy} (Quá {debtor.OverdueDays} ngày)\r\n" +
                               $"Nợ phạt hiện tại: {debtor.TotalDebt:N0} VNĐ\r\n" +
                               $"Tiền cọc hiện tại: {debtor.DepositBalance:N0} VNĐ\r\n" +
                               $"----------------------------------------\r\n" +
                               $"Số tiền cấn trừ vào cọc: {debtor.SettlementAmount:N0} VNĐ\r\n" +
                               $"Tiền cọc còn lại sau trừ: {debtor.RemainingDepositAfterSettlement:N0} VNĐ\r\n" +
                               $"Nợ còn lại sau trừ: {debtor.RemainingDebtAfterSettlement:N0} VNĐ";

            if (!DialogService.ShowConfirmation(confirmMsg, "Xác Nhận Cấn Trừ Nợ Phạt"))
            {
                return;
            }

            int userId = AuthService.CurrentSession?.UserId ?? 1;
            bool success = _memberService.SettleExpiredMemberDebt(debtor.MemberId, userId, out string msg, out string err);

            if (success)
            {
                DialogService.ShowInformation(msg, "Cấn Trừ Nợ Thành Công");
                LoadData();
            }
            else
            {
                DialogService.ShowError(err, "Cấn Trừ Nợ Thất Bại");
            }
        }

        private void ExecuteSettleAll()
        {
            var eligibleDebtors = Debtors.Where(x => x.CanSettleDebt).ToList();
            if (eligibleDebtors.Count == 0)
            {
                DialogService.ShowInformation("Không có độc giả nào đủ điều kiện cấn trừ (Đã hết nợ hoặc đang giữ sách).", "Thông Báo");
                return;
            }

            int borrowingCount = Debtors.Count(x => x.CurrentlyBorrowingCount > 0);
            string confirmMsg = $"Bạn có chắc chắn muốn thực thi cấn trừ nợ hàng loạt cho {eligibleDebtors.Count} độc giả hợp lệ?\r\n\r\n" +
                               $"Tổng tiền cấn trừ dự kiến: {TotalPossibleSettlement:N0} VNĐ\r\n" +
                               $"Số độc giả tự động BỎ QUA (do đang giữ sách chưa trả): {borrowingCount} độc giả\r\n\r\n" +
                               "Thao tác này sẽ ghi nhận biên lai thu phạt và cập nhật số dư cọc ngay lập tức.";

            if (!DialogService.ShowConfirmation(confirmMsg, "Xác Nhận Cấn Trừ Nợ Hàng Loạt"))
            {
                return;
            }

            int userId = AuthService.CurrentSession?.UserId ?? 1;
            _memberService.SettleAllExpiredMembersDebt(
                userId, 
                out int settledCount, 
                out int skippedBorrowingCount, 
                out int skippedZeroDepositCount, 
                out decimal totalSettledAmount, 
                out string summaryMessage);

            DialogService.ShowInformation(summaryMessage, "Hoàn Tất Xử Lý Hàng Loạt");
            LoadData();
        }
    }
}
