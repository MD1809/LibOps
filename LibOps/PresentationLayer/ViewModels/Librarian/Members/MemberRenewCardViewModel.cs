using System;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class MemberRenewCardViewModel : ViewModelBase
    {
        private readonly MemberService _memberService;
        private readonly SystemSettingsService _configService;
        private readonly ReaderPortalService _readerService;

        private int _memberId;
        private MemberEntity _memberInfo;
        private decimal _renewalFee = 50000m;
        private decimal _minDeposit = 200000m;
        private DateTime _currentExpiryDate;
        private DateTime _newExpiryDate;
        private string _paymentMethod = "CASH"; // CASH, BANK_TRANSFER, DEDUCT_DEPOSIT
        private bool _isReaderMode;
        private string _notes;
        private string _errorMessage;
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

        public MemberEntity MemberInfo
        {
            get => _memberInfo;
            set => SetProperty(ref _memberInfo, value);
        }

        public decimal RenewalFee
        {
            get => _renewalFee;
            set => SetProperty(ref _renewalFee, value);
        }

        public decimal MinDeposit
        {
            get => _minDeposit;
            set => SetProperty(ref _minDeposit, value);
        }

        public decimal RequiredBalance => MinDeposit + RenewalFee;

        public DateTime CurrentExpiryDate
        {
            get => _currentExpiryDate;
            set => SetProperty(ref _currentExpiryDate, value);
        }

        public DateTime NewExpiryDate
        {
            get => _newExpiryDate;
            set => SetProperty(ref _newExpiryDate, value);
        }

        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        public bool IsReaderMode
        {
            get => _isReaderMode;
            set
            {
                if (SetProperty(ref _isReaderMode, value))
                {
                    if (value && PaymentMethod == "CASH")
                    {
                        PaymentMethod = CanDeductFromDeposit ? "DEDUCT_DEPOSIT" : "BANK_TRANSFER";
                    }
                }
            }
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        public bool CanDeductFromDeposit => MemberInfo != null && MemberInfo.DepositBalance >= RequiredBalance;

        public string DeductDepositHint
        {
            get
            {
                if (MemberInfo == null) return string.Empty;
                if (CanDeductFromDeposit)
                {
                    return $"Đủ điều kiện (Số dư sau khi trừ: {(MemberInfo.DepositBalance - RenewalFee):N0} VNĐ ≥ {MinDeposit:N0} VNĐ cọc duy trì)";
                }
                return $"Cần số dư ≥ {RequiredBalance:N0} VNĐ ({MinDeposit:N0} VNĐ cọc duy trì + {RenewalFee:N0} VNĐ phí). Hiện tại: {MemberInfo.DepositBalance:N0} VNĐ";
            }
        }

        public ICommand ConfirmRenewCommand { get; }

        public event Action RequestClose;

        public MemberRenewCardViewModel(int memberId, bool isReaderMode = false)
        {
            _memberService = new MemberService();
            _configService = new SystemSettingsService();
            _readerService = new ReaderPortalService();

            RenewalFee = _readerService.GetAnnualFee();
            if (RenewalFee <= 0) RenewalFee = 50000m;

            MinDeposit = _configService.GetDefaultDeposit();
            if (MinDeposit <= 0) MinDeposit = 200000m;

            _isReaderMode = isReaderMode;
            _paymentMethod = isReaderMode ? "BANK_TRANSFER" : "CASH";

            ConfirmRenewCommand = new RelayCommand(ConfirmRenew);
            MemberId = memberId;
        }

        private void LoadMemberData()
        {
            if (MemberId <= 0) return;
            MemberInfo = _memberService.GetMemberById(MemberId);
            if (MemberInfo != null)
            {
                CurrentExpiryDate = MemberInfo.ExpiryDate;
                int validityDays = _configService.GetMemberValidityDays();
                if (validityDays <= 0) validityDays = 365;

                DateTime baseDate = MemberInfo.ExpiryDate > DateTime.Now ? MemberInfo.ExpiryDate : DateTime.Now;
                NewExpiryDate = baseDate.AddDays(validityDays);

                OnPropertyChanged(nameof(RequiredBalance));
                OnPropertyChanged(nameof(CanDeductFromDeposit));
                OnPropertyChanged(nameof(DeductDepositHint));

                if (IsReaderMode)
                {
                    PaymentMethod = CanDeductFromDeposit ? "DEDUCT_DEPOSIT" : "BANK_TRANSFER";
                }
            }
        }

        private void ConfirmRenew()
        {
            ErrorMessage = string.Empty;

            if (MemberInfo == null)
            {
                ErrorMessage = "Không tìm thấy thông tin độc giả.";
                return;
            }

            if (MemberInfo.CardStatus == "CLOSED")
            {
                ErrorMessage = "Không thể gia hạn thẻ đã bị hủy (CLOSED).";
                return;
            }

            if (MemberInfo.DepositBalance < 0)
            {
                ErrorMessage = $"Độc giả đang có nợ phạt ({Math.Abs(MemberInfo.DepositBalance):N0} VNĐ). Vui lòng nạp tiền thanh toán hết nợ trước!";
                return;
            }

            int userId = AuthService.CurrentSession?.UserId ?? 1;

            // 1. Quét mã VietQR PayOS
            if (PaymentMethod == "BANK_TRANSFER")
            {
                var qrVm = new VietQRQuickPayViewModel(
                    MemberId,
                    MemberInfo.MemberCardCode ?? $"DG{MemberId:D5}",
                    MemberInfo.FullName ?? "Độc giả",
                    RenewalFee,
                    "RENEW",
                    $"Gia hạn thẻ thư viện (+1 năm) - {MemberInfo.MemberCardCode}");

                if (DialogService.ShowVietQRQuickPayDialog(qrVm) == true)
                {
                    IsSuccess = true;
                    RequestClose?.Invoke();
                }
                return;
            }

            // 2. Khấu trừ trực tiếp từ Quỹ cọc
            if (PaymentMethod == "DEDUCT_DEPOSIT")
            {
                if (MemberInfo.DepositBalance < RequiredBalance)
                {
                    ErrorMessage = $"Số dư tiền cọc hiện tại ({MemberInfo.DepositBalance:N0} VNĐ) không đủ để duy trì cọc tối thiểu ({MinDeposit:N0} VNĐ) sau khi trừ phí ({RenewalFee:N0} VNĐ). Yêu cầu tối thiểu: {RequiredBalance:N0} VNĐ!";
                    return;
                }

                bool deductOk = _readerService.RenewCardWithDepositDeduction(MemberId, RenewalFee, out DateTime newExpDeduct, out string errDeduct);
                if (deductOk)
                {
                    IsSuccess = true;
                    RequestClose?.Invoke();
                }
                else
                {
                    ErrorMessage = errDeduct;
                }
                return;
            }

            // 3. Tiền mặt tại quầy (CASH)
            bool ok = _memberService.RenewMemberCard(MemberId, userId, out DateTime newExp, out string msg, out string err);
            if (ok)
            {
                IsSuccess = true;
                RequestClose?.Invoke();
            }
            else
            {
                ErrorMessage = err;
            }
        }
    }
}
