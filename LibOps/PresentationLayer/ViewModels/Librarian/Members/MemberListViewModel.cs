using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class MemberListViewModel : ViewModelBase
    {
        private readonly MemberService _memberService;
        private string _searchKeyword;
        private string _selectedStatusFilter = "ALL";
        private MemberGridDisplayDto _selectedMember;
        private bool _isLoading;
        private decimal _totalDepositFund;

        public PaginationController<MemberGridDisplayDto> Paging { get; } = new PaginationController<MemberGridDisplayDto>(10);
        public ObservableCollection<MemberGridDisplayDto> Members => Paging.CurrentPageItems;

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    LoadMembers();
                }
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    LoadMembers();
                }
            }
        }

        public MemberGridDisplayDto SelectedMember
        {
            get => _selectedMember;
            set
            {
                if (SetProperty(ref _selectedMember, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(ToggleLockLabel));
                }
            }
        }

        public bool HasSelection => SelectedMember != null;
        public string ToggleLockLabel => SelectedMember?.CardStatus == "LOCKED" ? "Mở Khóa Thẻ" : "Tạm Khóa Thẻ";

        public decimal TotalDepositFund
        {
            get => _totalDepositFund;
            set => SetProperty(ref _totalDepositFund, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ToggleLockCommand { get; }
        public ICommand AddMemberCommand { get; }
        public ICommand EditMemberCommand { get; }
        public ICommand RenewCardCommand { get; }
        public ICommand ReactivateCardCommand { get; }
        public ICommand DepositCommand { get; }
        public ICommand CloseCardCommand { get; }

        public MemberListViewModel()
        {
            _memberService = new MemberService();

            RefreshCommand = new RelayCommand(LoadMembers);
            ToggleLockCommand = new RelayCommand(ExecuteToggleLock, () => HasSelection);
            AddMemberCommand = new RelayCommand(ExecuteAddMember);
            EditMemberCommand = new RelayCommand(ExecuteEditMember, () => HasSelection);
            RenewCardCommand = new RelayCommand(ExecuteRenewCard, () => HasSelection);
            ReactivateCardCommand = new RelayCommand(ExecuteReactivateCard, () => HasSelection);
            DepositCommand = new RelayCommand(ExecuteDeposit, () => HasSelection);
            CloseCardCommand = new RelayCommand(ExecuteCloseCard, () => HasSelection);

            LoadMembers();
        }

        private void ExecuteReactivateCard()
        {
            if (SelectedMember == null) return;
            if (SelectedMember.CardStatus != "CLOSED")
            {
                DialogService.ShowWarning("Chức năng Tái Cấp Thẻ chỉ áp dụng cho độc giả có thẻ ở trạng thái 'Đã hủy' (CLOSED).", "Thông Báo");
                return;
            }

            var vm = new MemberReactivateViewModel(SelectedMember.MemberId);
            if (DialogService.ShowMemberReactivateDialog(vm) == true)
            {
                DialogService.ShowInformation(
                    $"Tái cấp thẻ cho độc giả {SelectedMember.FullName} thành công!\n\n" +
                    $"Mã thẻ: {SelectedMember.MemberCardCode}\n" +
                    $"Hạn sử dụng mới: {vm.NewExpiryDate:dd/MM/yyyy}\n" +
                    $"Số dư tiền cọc mới: {vm.InitialDeposit:N0} VNĐ\n\n" +
                    "Tài khoản Reader đã được kích hoạt lại và sẵn sàng sử dụng.",
                    "Tái Cấp Thẻ Thành Công");
                LoadMembers();
            }
        }

        private void ExecuteRenewCard()
        {
            if (SelectedMember == null) return;
            var vm = new MemberRenewCardViewModel(SelectedMember.MemberId);
            if (DialogService.ShowMemberRenewCardDialog(vm) == true)
            {
                DialogService.ShowInformation(
                    $"Gia hạn thẻ độc giả {SelectedMember.MemberCardCode} thành công!\nHạn mới: {vm.NewExpiryDate:dd/MM/yyyy}",
                    "Gia Hạn Thành Công");
                LoadMembers();
            }
        }

        private void ExecuteAddMember()
        {
            var vm = new MemberDetailViewModel(0);
            if (DialogService.ShowMemberDetailDialog(vm) == true)
            {
                DialogService.ShowInformation(
                    $"Đăng ký thẻ độc giả thành công!\n\n" +
                    $"Mã thẻ: {vm.MemberCardCode}\n" +
                    $"Tên độc giả: {vm.FullName}\n" +
                    $"Tài khoản đăng nhập Reader: {vm.MemberCardCode}\n" +
                    $"Mật khẩu khởi tạo: {MemberService.DEFAULT_READER_PASSWORD}\n\n" +
                    "Độc giả có thể đăng nhập ngay vào hệ thống để tra cứu và sử dụng dịch vụ.",
                    "Đăng Ký Độc Giả Thành Công");
                LoadMembers();
            }
        }

        private void ExecuteEditMember()
        {
            if (SelectedMember == null) return;
            var vm = new MemberDetailViewModel(SelectedMember.MemberId);
            if (DialogService.ShowMemberDetailDialog(vm) == true)
            {
                LoadMembers();
            }
        }

        private void ExecuteDeposit()
        {
            if (SelectedMember == null) return;
            var vm = new MemberDepositViewModel(SelectedMember.MemberId);
            if (DialogService.ShowMemberDepositDialog(vm) == true)
            {
                DialogService.ShowInformation("Nạp tiền cọc thành công!");
                LoadMembers();
            }
        }

        private void ExecuteCloseCard()
        {
            if (SelectedMember == null) return;
            var vm = new MemberClosureViewModel(SelectedMember.MemberId);
            if (DialogService.ShowMemberClosureDialog(vm) == true)
            {
                DialogService.ShowInformation("Đã hủy thẻ và tất toán cọc thành công!");
                LoadMembers();
            }
        }

        public void LoadMembers()
        {
            try
            {
                IsLoading = true;
                string status = SelectedStatusFilter == "ALL" ? null : SelectedStatusFilter;
                var list = _memberService.GetAllMembers(SearchKeyword, status);
                Paging.SetSource(list);
                TotalDepositFund = list != null ? list.Sum(m => m.DepositBalance) : 0;
            }
            catch
            {
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteToggleLock()
        {
            if (SelectedMember == null) return;

            int userId = AuthService.CurrentSession?.UserId ?? 1;
            bool ok = _memberService.ToggleMemberCardStatus(SelectedMember.MemberId, userId, out string newStatus, out _, out _);
            if (ok)
            {
                LoadMembers();
            }
        }
    }
}
