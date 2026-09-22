using System;
using System.Drawing;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class MemberClosureViewModel : ViewModelBase
    {
        private readonly MemberService _memberService;

        private int _memberId;
        private MemberEntity _memberInfo;
        private int _borrowingCount;
        private decimal _refundAmount;
        private string _reason = "Độc giả tốt nghiệp ra trường / Không còn nhu cầu sử dụng thẻ";
        private string _paymentMethod = "CASH";
        private bool _isCashSelected = true;
        private bool _isBankTransferSelected;
        private string _bankName = "MBBank";
        private string _accountNumber;
        private string _accountHolder;
        private bool _isAutoPrintVoucher = true;
        private string _receiptCode;
        private Bitmap _generatedVoucherBitmap;

        private string _errorMessage;
        private string _warningMessage;
        private bool _canClose;
        private bool _isSuccess;

        public int MemberId
        {
            get => _memberId;
            set
            {
                if (SetProperty(ref _memberId, value))
                {
                    LoadMemberData();
                }
            }
        }

        public MemberEntity MemberInfo { get => _memberInfo; set => SetProperty(ref _memberInfo, value); }
        public int BorrowingCount { get => _borrowingCount; set => SetProperty(ref _borrowingCount, value); }
        public decimal RefundAmount { get => _refundAmount; set => SetProperty(ref _refundAmount, value); }
        public string Reason { get => _reason; set => SetProperty(ref _reason, value); }
        public string PaymentMethod { get => _paymentMethod; set => SetProperty(ref _paymentMethod, value); }

        public bool IsCashSelected
        {
            get => _isCashSelected;
            set
            {
                if (SetProperty(ref _isCashSelected, value) && value)
                {
                    PaymentMethod = "CASH";
                    IsBankTransferSelected = false;
                }
            }
        }

        public bool IsBankTransferSelected
        {
            get => _isBankTransferSelected;
            set
            {
                if (SetProperty(ref _isBankTransferSelected, value) && value)
                {
                    PaymentMethod = "BANK_TRANSFER";
                    IsCashSelected = false;
                }
            }
        }

        public string BankName { get => _bankName; set => SetProperty(ref _bankName, value); }
        public string AccountNumber { get => _accountNumber; set => SetProperty(ref _accountNumber, value); }
        public string AccountHolder { get => _accountHolder; set => SetProperty(ref _accountHolder, value); }
        public bool IsAutoPrintVoucher { get => _isAutoPrintVoucher; set => SetProperty(ref _isAutoPrintVoucher, value); }
        public string ReceiptCode { get => _receiptCode; set => SetProperty(ref _receiptCode, value); }
        public Bitmap GeneratedVoucherBitmap { get => _generatedVoucherBitmap; set => SetProperty(ref _generatedVoucherBitmap, value); }

        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public string WarningMessage { get => _warningMessage; set => SetProperty(ref _warningMessage, value); }
        public bool CanClose { get => _canClose; set => SetProperty(ref _canClose, value); }
        public bool IsSuccess { get => _isSuccess; set => SetProperty(ref _isSuccess, value); }

        public ICommand ConfirmClosureCommand { get; }

        public event Action RequestClose;

        public MemberClosureViewModel(int memberId)
        {
            _memberService = new MemberService();
            ConfirmClosureCommand = new RelayCommand(ConfirmClosure, () => CanClose);
            MemberId = memberId;
        }

        private void LoadMemberData()
        {
            if (MemberId <= 0) return;

            try
            {
                MemberInfo = _memberService.GetMemberById(MemberId);
                BorrowingCount = _memberService.GetCurrentlyBorrowingCount(MemberId);

                if (MemberInfo == null)
                {
                    ErrorMessage = "Hồ sơ độc giả không tồn tại.";
                    CanClose = false;
                    return;
                }

                if (MemberInfo.CardStatus == "CLOSED")
                {
                    ErrorMessage = "Thẻ thư viện này đã ở trạng thái ĐÃ HỦY trước đó.";
                    CanClose = false;
                    return;
                }

                if (BorrowingCount > 0)
                {
                    WarningMessage = $"Độc giả hiện đang mượn {BorrowingCount} cuốn sách chưa trả. Yêu cầu độc giả trả hết sách trước khi làm thủ tục hủy thẻ.";
                    CanClose = false;
                    return;
                }

                RefundAmount = MemberInfo.DepositBalance - MemberInfo.TotalDebt;
                if (RefundAmount < 0)
                {
                    WarningMessage = $"Tiền cọc ({MemberInfo.DepositBalance:N0} đ) không đủ bù nợ phạt ({MemberInfo.TotalDebt:N0} đ). Độc giả cần nộp thêm {(-RefundAmount):N0} đ.";
                    CanClose = false;
                    return;
                }

                AccountHolder = MemberInfo.FullName?.ToUpper();
                CanClose = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Lỗi tải thông tin hủy thẻ: " + ex.Message;
                CanClose = false;
            }
        }

        private void ConfirmClosure()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Reason))
            {
                ErrorMessage = "Vui lòng nhập lý do hủy thẻ.";
                return;
            }

            if (IsBankTransferSelected && RefundAmount > 0)
            {
                if (string.IsNullOrWhiteSpace(AccountNumber))
                {
                    ErrorMessage = "Vui lòng nhập Số tài khoản ngân hàng để thực hiện chuyển khoản.";
                    return;
                }
                if (string.IsNullOrWhiteSpace(AccountHolder))
                {
                    ErrorMessage = "Vui lòng nhập Tên chủ tài khoản ngân hàng.";
                    return;
                }
            }

            int userId = AuthService.CurrentSession?.UserId ?? 1;
            string handledByName = AuthService.CurrentSession?.FullName ?? "Thủ thư";

            bool ok = _memberService.CloseMemberCard(
                MemberId,
                userId,
                Reason,
                PaymentMethod,
                BankName,
                AccountNumber,
                AccountHolder,
                out string receiptCode,
                out string error);

            if (ok)
            {
                ReceiptCode = receiptCode;
                IsSuccess = true;

                if (RefundAmount > 0)
                {
                    string paymentMethodDisplay = IsBankTransferSelected
                        ? $"Chuyển khoản ({BankName} - {AccountNumber})"
                        : "Tiền mặt tại quầy";

                    GeneratedVoucherBitmap = _memberService.GenerateExpenseVoucherBitmap(
                        receiptCode,
                        MemberInfo.MemberCardCode,
                        MemberInfo.FullName,
                        MemberInfo.PhoneNumber,
                        RefundAmount,
                        paymentMethodDisplay,
                        Reason,
                        handledByName
                    );
                }

                RequestClose?.Invoke();
            }
            else
            {
                ErrorMessage = error;
            }
        }
    }
}
