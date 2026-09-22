using System;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class MemberDetailViewModel : ViewModelBase
    {
        private readonly MemberService _memberService;
        private readonly SystemSettingsService _configService;

        private int _memberId;
        private string _memberCardCode;
        private string _fullName;
        private string _phoneNumber;
        private string _email;
        private string _identityCardNumber;
        private DateTime? _dateOfBirth = new DateTime(2000, 1, 1);
        private string _address;
        private decimal _cardIssuanceFee = 50000m;
        private decimal _initialDeposit = 200000m;
        private string _cardStatus = "ACTIVE";
        private string _errorMessage;
        private bool _isSavedSuccessfully;

        public string DialogTitle => _memberId == 0 ? "ĐĂNG KÝ HỒ SƠ ĐỘC GIẢ MỚI" : "CẬP NHẬT HỒ SƠ ĐỘC GIẢ";
        public bool IsNewMember => _memberId == 0;
        public bool IsMemberCardCodeReadOnly => !IsNewMember;

        public int MemberId
        {
            get => _memberId;
            set
            {
                if (SetProperty(ref _memberId, value))
                {
                    OnPropertyChanged(nameof(DialogTitle));
                    OnPropertyChanged(nameof(IsNewMember));
                    OnPropertyChanged(nameof(IsMemberCardCodeReadOnly));
                }
            }
        }

        public string MemberCardCode { get => _memberCardCode; set => SetProperty(ref _memberCardCode, value); }
        public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
        public string PhoneNumber { get => _phoneNumber; set => SetProperty(ref _phoneNumber, value); }
        public string Email { get => _email; set => SetProperty(ref _email, value); }
        public string IdentityCardNumber { get => _identityCardNumber; set => SetProperty(ref _identityCardNumber, value); }
        public DateTime? DateOfBirth { get => _dateOfBirth; set => SetProperty(ref _dateOfBirth, value); }
        public string Address { get => _address; set => SetProperty(ref _address, value); }
        
        public decimal CardIssuanceFee
        {
            get => _cardIssuanceFee;
            set
            {
                if (SetProperty(ref _cardIssuanceFee, value))
                {
                    OnPropertyChanged(nameof(TotalPaymentDue));
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
                    OnPropertyChanged(nameof(TotalPaymentDue));
                }
            }
        }

        public decimal TotalPaymentDue => CardIssuanceFee + InitialDeposit;

        private DateTime _issueDate = DateTime.Now;
        private DateTime _expiryDate = DateTime.Now.AddYears(1);

        public DateTime IssueDate { get => _issueDate; set => SetProperty(ref _issueDate, value); }
        public DateTime ExpiryDate { get => _expiryDate; set => SetProperty(ref _expiryDate, value); }

        public string CardStatus { get => _cardStatus; set => SetProperty(ref _cardStatus, value); }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public bool IsSavedSuccessfully { get => _isSavedSuccessfully; set => SetProperty(ref _isSavedSuccessfully, value); }

        public ICommand SaveCommand { get; }

        public event Action RequestClose;

        public MemberDetailViewModel(int memberId = 0)
        {
            _memberService = new MemberService();
            _configService = new SystemSettingsService();
            SaveCommand = new RelayCommand(SaveMember);

            if (memberId > 0)
            {
                LoadMember(memberId);
            }
            else
            {
                MemberCardCode = _memberService.GenerateNextMemberCardCode();
                CardIssuanceFee = _configService.GetCardIssuanceFee();
                InitialDeposit = _configService.GetDefaultDeposit();
                IssueDate = DateTime.Now;
                ExpiryDate = DateTime.Now.AddYears(1);
            }
        }

        private void LoadMember(int id)
        {
            try
            {
                var m = _memberService.GetMemberById(id);
                if (m != null)
                {
                    MemberId = m.MemberId;
                    MemberCardCode = m.MemberCardCode;
                    FullName = m.FullName;
                    PhoneNumber = m.PhoneNumber;
                    Email = m.Email;
                    IdentityCardNumber = m.IdentityCardNumber;
                    DateOfBirth = m.DateOfBirth;
                    Address = m.Address;
                    InitialDeposit = m.DepositBalance;
                    IssueDate = m.IssueDate > new DateTime(1753, 1, 1) ? m.IssueDate : DateTime.Now;
                    ExpiryDate = m.ExpiryDate > new DateTime(1753, 1, 1) ? m.ExpiryDate : DateTime.Now.AddYears(1);
                    CardStatus = m.CardStatus;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Không thể tải thông tin độc giả: " + ex.Message;
            }
        }

        private void SaveMember()
        {
            ErrorMessage = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(FullName))
                {
                    ErrorMessage = "Vui lòng nhập họ và tên độc giả.";
                    return;
                }

                if (FullName.Trim().Length > 100)
                {
                    ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(PhoneNumber))
                {
                    ErrorMessage = "Vui lòng nhập số điện thoại liên hệ.";
                    return;
                }

                if (!LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidPhoneNumber(PhoneNumber))
                {
                    ErrorMessage = "Số điện thoại không đúng định dạng (10 chữ số).";
                    return;
                }

                if (string.IsNullOrWhiteSpace(IdentityCardNumber))
                {
                    ErrorMessage = "Vui lòng nhập số CCCD/CMND độc giả.";
                    return;
                }

                if (!LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidIdentityCard(IdentityCardNumber))
                {
                    ErrorMessage = "Số CCCD/CMND không hợp lệ (9 hoặc 12 chữ số).";
                    return;
                }

                if (!string.IsNullOrWhiteSpace(Email) && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidEmail(Email))
                {
                    ErrorMessage = "Địa chỉ Email không đúng định dạng.";
                    return;
                }

                if (DateOfBirth.HasValue && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidDateOfBirth(DateOfBirth, out string dobErr))
                {
                    ErrorMessage = dobErr;
                    return;
                }

                var member = new MemberEntity
                {
                    MemberId = MemberId,
                    MemberCardCode = MemberCardCode?.Trim(),
                    FullName = FullName?.Trim(),
                    PhoneNumber = LibOps.CommonUtilities.Validation.InputValidationUtility.NormalizePhoneNumber(PhoneNumber),
                    Email = Email?.Trim(),
                    IdentityCardNumber = LibOps.CommonUtilities.Validation.InputValidationUtility.NormalizeIdentityCard(IdentityCardNumber),
                    DateOfBirth = DateOfBirth,
                    Address = Address?.Trim(),
                    DepositBalance = InitialDeposit,
                    IssueDate = IssueDate > new DateTime(1753, 1, 1) ? IssueDate : DateTime.Now,
                    ExpiryDate = ExpiryDate > new DateTime(1753, 1, 1) ? ExpiryDate : DateTime.Now.AddYears(1),
                    CardStatus = CardStatus
                };

                bool success;
                string error;

                if (MemberId == 0)
                {
                    success = _memberService.RegisterNewMember(member, CardIssuanceFee, InitialDeposit, 1, out error);
                }
                else
                {
                    success = _memberService.UpdateMember(member, out error);
                }

                if (success)
                {
                    IsSavedSuccessfully = true;
                    RequestClose?.Invoke();
                }
                else
                {
                    ErrorMessage = error;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Đã xảy ra lỗi khi lưu thông tin độc giả: " + ex.Message;
            }
        }
    }
}
