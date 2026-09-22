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
    /// ViewModel quản lý thẻ thư viện điện tử, nạp cọc mô phỏng, gia hạn thẻ, hủy thẻ và tra cứu toàn bộ dòng tiền
    /// </summary>
    public class ReaderFinanceViewModel : ViewModelBase
    {
        private readonly ReaderPortalService _readerService;
        private readonly UserSessionDto _currentSession;

        private ReaderCardSummaryDto _memberCardSummary;
        private decimal _topUpAmountInput = 100000;
        private bool _isTopUpPanelOpen;

        public ReaderCardSummaryDto MemberCardSummary
        {
            get => _memberCardSummary;
            set => SetProperty(ref _memberCardSummary, value);
        }

        public decimal TopUpAmountInput
        {
            get => _topUpAmountInput;
            set => SetProperty(ref _topUpAmountInput, value);
        }

        public bool IsTopUpPanelOpen
        {
            get => _isTopUpPanelOpen;
            set => SetProperty(ref _isTopUpPanelOpen, value);
        }

        public PaginationController<ReaderCashFlowDisplayDto> Paging { get; } = new PaginationController<ReaderCashFlowDisplayDto>(10);
        public ObservableCollection<ReaderCashFlowDisplayDto> CashFlowHistories => Paging.CurrentPageItems;

        public ICommand RenewCardCommand { get; }
        public ICommand ToggleTopUpCommand { get; }
        public ICommand SetQuickTopUpAmountCommand { get; }
        public ICommand SimulateTopUpCommand { get; }
        public ICommand CloseCardCommand { get; }
        public ICommand RefreshCommand { get; }

        public ReaderFinanceViewModel(UserSessionDto session = null)
        {
            _currentSession = session ?? AuthService.CurrentSession;
            _readerService = new ReaderPortalService();

            RenewCardCommand = new RelayCommand(ExecuteRenewCard);
            ToggleTopUpCommand = new RelayCommand(() => IsTopUpPanelOpen = !IsTopUpPanelOpen);
            SetQuickTopUpAmountCommand = new RelayCommand<object>(ExecuteSetQuickAmount);
            SimulateTopUpCommand = new RelayCommand(ExecuteSimulateTopUp);
            CloseCardCommand = new RelayCommand(ExecuteCloseCard);
            RefreshCommand = new RelayCommand(LoadData);

            LoadData();
        }

        public void LoadData()
        {
            LoadCardSummary();
            LoadCashFlowHistories();
        }

        private void LoadCardSummary()
        {
            try
            {
                if (_currentSession?.MemberId == null) return;
                MemberCardSummary = _readerService.GetCardSummary(_currentSession.MemberId.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadCardSummary Error]: {ex.Message}");
            }
        }

        private void LoadCashFlowHistories()
        {
            try
            {
                if (_currentSession?.MemberId == null) return;
                var list = _readerService.GetMemberAllCashFlowHistory(_currentSession.MemberId.Value);
                Paging.SetSource(list);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadCashFlowHistories Error]: {ex.Message}");
            }
        }

        private void ExecuteSetQuickAmount(object parameter)
        {
            if (parameter != null && decimal.TryParse(parameter.ToString(), out decimal val))
            {
                TopUpAmountInput = val;
            }
        }

        private void ExecuteRenewCard()
        {
            if (_currentSession?.MemberId == null || MemberCardSummary == null) return;

            var vm = new MemberRenewCardViewModel(_currentSession.MemberId.Value, isReaderMode: true);
            if (DialogService.ShowMemberRenewCardDialog(vm) == true)
            {
                DialogService.ShowInformation(
                    $"Gia hạn thẻ thư viện thành công!\nHạn sử dụng mới: {vm.NewExpiryDate:dd/MM/yyyy}",
                    "Gia Hạn Thành Công");
                LoadData();
            }
        }

        private void ExecuteSimulateTopUp()
        {
            if (_currentSession?.MemberId == null) return;

            if (TopUpAmountInput < 10000)
            {
                DialogService.ShowWarning("Số tiền nạp tối thiểu là 10.000 VNĐ.", "Số Tiền Không Hợp Lệ");
                return;
            }

            var vm = new VietQRQuickPayViewModel(
                _currentSession.MemberId.Value,
                MemberCardSummary?.MemberCardCode ?? _currentSession.Username,
                MemberCardSummary?.FullName ?? _currentSession.FullName,
                TopUpAmountInput,
                "TOP_UP",
                "Nạp tiền quỹ cọc qua Cổng VietQR PayOS");

            if (DialogService.ShowVietQRQuickPayDialog(vm) == true)
            {
                DialogService.ShowInformation(
                    $"Giao dịch thành công! Đã nạp {TopUpAmountInput:N0} VNĐ vào tài khoản độc giả.\nSố dư tiền cọc và công nợ đã được cập nhật.",
                    "Nạp Tiền Thành Công");
                IsTopUpPanelOpen = false;
                LoadData();
            }
        }

        private void ExecuteCloseCard()
        {
            if (_currentSession?.MemberId == null || MemberCardSummary == null) return;

            if (MemberCardSummary.ActiveBorrowCount > 0)
            {
                DialogService.ShowWarning(
                    $"Bạn hiện đang mượn {MemberCardSummary.ActiveBorrowCount} cuốn sách chưa trả.\nVui lòng hoàn trả toàn bộ sách trước khi gửi yêu cầu hủy thẻ!",
                    "Chưa Thể Hủy Thẻ");
                return;
            }

            if (MemberCardSummary.TotalDebt > 0)
            {
                DialogService.ShowWarning(
                    $"Tài khoản của bạn còn nợ phạt {MemberCardSummary.TotalDebt:N0} VNĐ.\nVui lòng thanh toán hết nợ phạt trước khi hủy thẻ!",
                    "Còn Nợ Phạt Chưa Xử Lý");
                return;
            }

            if (MemberCardSummary.HasPendingRequest)
            {
                DialogService.ShowWarning(
                    "Bạn đang có một yêu cầu chờ xét duyệt. Vui lòng chờ thủ thư xử lý trước khi gửi yêu cầu mới!",
                    "Đang Chờ Xử Lý");
                return;
            }

            // Mở Dialog thu thập phương thức hoàn tiền (Chuyển khoản hoặc Tiền mặt)
            var dialog = new Views.CloseCardRequestDialog(
                MemberCardSummary.DepositBalance, 
                _currentSession?.FullName ?? string.Empty);

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            bool ok = _readerService.SubmitClosureRequest(
                _currentSession.MemberId.Value,
                dialog.PayoutMethod,
                dialog.BankName,
                dialog.BankAccountNumber,
                dialog.BankAccountHolder,
                dialog.Reason,
                out string requestCode,
                out string errorMessage);

            if (ok)
            {
                string payoutInfo = dialog.PayoutMethod == "CASH" 
                    ? "Hình thức nhận: Tiền mặt tại quầy thư viện" 
                    : $"Hình thức nhận: Chuyển khoản {dialog.BankName} - STK: {dialog.BankAccountNumber}";

                DialogService.ShowInformation(
                    $"Đã gửi yêu cầu hủy thẻ thành công!\n\nMã yêu cầu: {requestCode}\nSố tiền hoàn: {MemberCardSummary.DepositBalance:N0} VNĐ\n{payoutInfo}\n\nThủ thư sẽ kiểm tra và hoàn trả tiền cọc cho bạn sớm nhất.",
                    "Gửi Yêu Cầu Thành Công");
                LoadData();
            }
            else
            {
                DialogService.ShowError($"Gửi yêu cầu hủy thẻ thất bại: {errorMessage}", "Thất Bại");
            }
        }
    }
}
