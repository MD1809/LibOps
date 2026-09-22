using System;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class MemberDepositViewModel : ViewModelBase
    {
        private readonly MemberService _memberService;

        private int _memberId;
        private MemberEntity _memberInfo;
        private decimal _depositAmount = 100000m;
        private string _paymentMethod = "CASH";
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
                    LoadMember();
                }
            }
        }

        public MemberEntity MemberInfo
        {
            get => _memberInfo;
            set => SetProperty(ref _memberInfo, value);
        }

        public decimal DepositAmount
        {
            get => _depositAmount;
            set => SetProperty(ref _depositAmount, value);
        }

        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
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

        public ICommand ConfirmDepositCommand { get; }

        public event Action RequestClose;

        public MemberDepositViewModel(int memberId)
        {
            _memberService = new MemberService();
            ConfirmDepositCommand = new RelayCommand(ConfirmDeposit);
            MemberId = memberId;
        }

        private void LoadMember()
        {
            try
            {
                if (MemberId <= 0) return;
                MemberInfo = _memberService.GetMemberById(MemberId);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Không thể tải thông tin độc giả: " + ex.Message;
            }
        }

        private void ConfirmDeposit()
        {
            ErrorMessage = string.Empty;

            try
            {
                if (DepositAmount < 1000m)
                {
                    ErrorMessage = "Số tiền nạp thêm tối thiểu là 1.000 VNĐ.";
                    return;
                }

                if (DepositAmount > 50000000m)
                {
                    ErrorMessage = "Số tiền nạp thêm mỗi lần không vượt quá 50.000.000 VNĐ.";
                    return;
                }

                if (PaymentMethod == "BANK_TRANSFER")
                {
                    // Mở cửa sổ VietQR PayOS để độc giả quét mã chuyển khoản tự động
                    var qrVm = new VietQRQuickPayViewModel(
                        MemberId,
                        MemberInfo?.MemberCardCode ?? $"DG{MemberId:D5}",
                        MemberInfo?.FullName ?? "Độc giả",
                        DepositAmount,
                        "TOP_UP",
                        Notes);

                    if (DialogService.ShowVietQRQuickPayDialog(qrVm) == true)
                    {
                        IsSuccess = true;
                        RequestClose?.Invoke();
                    }
                    return;
                }

                // Thanh toán tiền mặt tại quầy
                int userId = AuthService.CurrentSession?.UserId ?? 1;
                bool ok = _memberService.TopUpDeposit(MemberId, DepositAmount, userId, Notes, out string error);
                if (ok)
                {
                    IsSuccess = true;
                    RequestClose?.Invoke();
                }
                else
                {
                    ErrorMessage = error;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Đã xảy ra lỗi khi nạp tiền cọc: " + ex.Message;
            }
        }
    }
}
