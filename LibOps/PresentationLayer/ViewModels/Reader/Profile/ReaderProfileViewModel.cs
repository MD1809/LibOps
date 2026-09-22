using System;
using System.Linq;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;
using LibOps.PresentationLayer.Services;

namespace LibOps.PresentationLayer.ViewModels
{
    /// <summary>
    /// ViewModel quản lý và hiển thị thông tin chi tiết hồ sơ độc giả, đổi mật khẩu cá nhân
    /// </summary>
    public class ReaderProfileViewModel : ViewModelBase
    {
        private readonly MemberService _memberService;
        private readonly ReaderPortalService _readerPortalService;
        private readonly UserSessionDto _currentSession;

        private MemberEntity _memberProfile;
        private ReaderCardSummaryDto _memberCardSummary;

        public MemberEntity MemberProfile
        {
            get => _memberProfile;
            set
            {
                if (SetProperty(ref _memberProfile, value))
                {
                    OnPropertyChanged(nameof(AvatarInitials));
                    OnPropertyChanged(nameof(FormattedBirthDate));
                    OnPropertyChanged(nameof(FormattedIssueDate));
                    OnPropertyChanged(nameof(FormattedExpiryDate));
                    OnPropertyChanged(nameof(DaysRemainingText));
                    OnPropertyChanged(nameof(IsCardActive));
                    OnPropertyChanged(nameof(FormattedDeposit));
                    OnPropertyChanged(nameof(FormattedDebt));
                }
            }
        }

        public ReaderCardSummaryDto MemberCardSummary
        {
            get => _memberCardSummary;
            set => SetProperty(ref _memberCardSummary, value);
        }

        public string MemberUsername => _currentSession?.Username ?? "reader";
        public string RoleDisplay => "Độc Giả Thư Viện";

        public string AvatarInitials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MemberProfile?.FullName))
                    return "DG";

                var parts = MemberProfile.FullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1)
                    return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

                return $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper();
            }
        }

        public string FormattedBirthDate => MemberProfile?.DateOfBirth.HasValue == true 
            ? MemberProfile.DateOfBirth.Value.ToString("dd/MM/yyyy") 
            : "Chưa cập nhật";

        public string FormattedIssueDate => MemberProfile != null 
            ? MemberProfile.IssueDate.ToString("dd/MM/yyyy") 
            : "--/--/----";

        public string FormattedExpiryDate => MemberProfile != null 
            ? MemberProfile.ExpiryDate.ToString("dd/MM/yyyy") 
            : "--/--/----";

        public string FormattedDeposit => MemberProfile != null 
            ? $"{MemberProfile.DepositBalance:N0} đ" 
            : "0 đ";

        public string FormattedDebt => MemberProfile != null 
            ? $"{MemberProfile.TotalDebt:N0} đ" 
            : "0 đ";

        public bool IsCardActive => MemberProfile != null && MemberProfile.CardStatus == "ACTIVE" && MemberProfile.ExpiryDate >= DateTime.Today;

        public string DaysRemainingText
        {
            get
            {
                if (MemberProfile == null) return "Chưa xác định";
                int days = (MemberProfile.ExpiryDate.Date - DateTime.Today).Days;
                if (days > 0) return $"Còn {days} ngày";
                if (days == 0) return "Hết hạn hôm nay";
                return $"Đã quá hạn {Math.Abs(days)} ngày";
            }
        }

        public ICommand ChangePasswordCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewCardBenefitsCommand { get; }

        public ReaderProfileViewModel(UserSessionDto session = null)
        {
            _currentSession = session ?? AuthService.CurrentSession;
            _memberService = new MemberService();
            _readerPortalService = new ReaderPortalService();

            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
            RefreshCommand = new RelayCommand(LoadProfile);
            ViewCardBenefitsCommand = new RelayCommand(ExecuteViewCardBenefits);

            LoadProfile();
        }

        public void LoadProfile()
        {
            try
            {
                if (_currentSession?.MemberId == null) return;
                int memberId = _currentSession.MemberId.Value;
                MemberProfile = _memberService.GetMemberById(memberId);
                MemberCardSummary = _readerPortalService.GetCardSummary(memberId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadProfile Error]: {ex.Message}");
            }
        }

        private void ExecuteChangePassword()
        {
            int userId = _currentSession?.UserId ?? 1;
            var vm = new ChangePasswordViewModel(userId);
            if (DialogService.ShowChangePasswordDialog(vm) == true)
            {
                DialogService.ShowInformation("Đổi mật khẩu tài khoản thành công!");
            }
        }

        private void ExecuteViewCardBenefits()
        {
            DialogService.ShowInformation(
                "QUYỀN LỢI THẺ ĐỘC GIẢ LIBOPS:\n\n" +
                "• Mượn tối đa 3 tài liệu cùng lúc trong thời hạn 14 ngày/lần.\n" +
                "• Tự gia hạn sách trực tuyến 1 lần (+7 ngày) khi chưa quá hạn.\n" +
                "• Tra cứu OPAC trực tuyến và xem tình trạng khả dụng thời gian thực.\n" +
                "• Nạp tiền cọc và thanh lý thẻ trực tuyến bất cứ lúc nào khi không còn sách mượn.",
                "Quyền Lợi & Quy Định Thẻ Thư Viện");
        }
    }
}
