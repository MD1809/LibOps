using System;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class MemberReactivateViewModel : ViewModelBase
    {
        private readonly MemberService _memberService;
        private readonly SystemSettingsService _configService;

        private int _memberId;
        private MemberEntity _memberInfo;
        private decimal _cardIssuanceFee = 50000m;
        private decimal _initialDeposit = 200000m;
        private DateTime _newExpiryDate;
        private string _notes;
        private string _errorMessage;
        private bool _isSuccess;

        public event Action RequestClose;

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

        public decimal CardIssuanceFee
        {
            get => _cardIssuanceFee;
            set
            {
                if (SetProperty(ref _cardIssuanceFee, value))
                {
                    OnPropertyChanged(nameof(TotalPaymentRequired));
                }
            }
        }

        public decimal InitialDeposit
        {
            get => _initialDeposit;
            set
            {
                if (SetProperty(ref _initialDeposit, value))
                {
                    OnPropertyChanged(nameof(TotalPaymentRequired));
                }
            }
        }

        public decimal TotalPaymentRequired => CardIssuanceFee + InitialDeposit;

        public DateTime NewExpiryDate
        {
            get => _newExpiryDate;
            set => SetProperty(ref _newExpiryDate, value);
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

        public ICommand ReactivateCommand { get; }
        public ICommand CancelCommand { get; }

        public MemberReactivateViewModel(int memberId)
        {
            _memberService = new MemberService();
            _configService = new SystemSettingsService();

            ReactivateCommand = new RelayCommand(ExecuteReactivate);
            CancelCommand = new RelayCommand(ExecuteCancel);

            _cardIssuanceFee = _configService.GetCardIssuanceFee();
            _initialDeposit = _configService.GetDefaultDeposit();
            _newExpiryDate = DateTime.Now.AddYears(1);

            MemberId = memberId;
        }

        private void LoadMemberData()
        {
            try
            {
                if (_memberId <= 0) return;
                MemberInfo = _memberService.GetMemberById(_memberId);
                if (MemberInfo == null)
                {
                    ErrorMessage = "Không tìm thấy thông tin độc giả.";
                    return;
                }

                if (MemberInfo.CardStatus != "CLOSED")
                {
                    ErrorMessage = $"Thẻ độc giả đang ở trạng thái '{MemberInfo.CardStatus}', không phải thẻ đã hủy.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Lỗi khi nạp dữ liệu độc giả: " + ex.Message;
            }
        }

        private void ExecuteReactivate()
        {
            ErrorMessage = string.Empty;

            if (MemberInfo == null)
            {
                ErrorMessage = "Thông tin độc giả không hợp lệ.";
                return;
            }

            if (MemberInfo.CardStatus != "CLOSED")
            {
                ErrorMessage = "Chỉ có thể tái cấp cho thẻ đang ở trạng thái 'Đã hủy' (CLOSED).";
                return;
            }

            if (CardIssuanceFee < 0)
            {
                ErrorMessage = "Phí phát hành lại thẻ không thể nhỏ hơn 0.";
                return;
            }

            if (InitialDeposit < 0)
            {
                ErrorMessage = "Tiền cọc ban đầu không thể nhỏ hơn 0.";
                return;
            }

            bool ok = _memberService.ReactivateMemberCard(
                MemberId,
                CardIssuanceFee,
                InitialDeposit,
                1, // Handled by default staff
                out string errMsg
            );

            if (ok)
            {
                IsSuccess = true;
                RequestClose?.Invoke();
            }
            else
            {
                ErrorMessage = errMsg;
            }
        }

        private void ExecuteCancel()
        {
            RequestClose?.Invoke();
        }
    }
}
