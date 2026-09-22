using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class StaffManagementViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private string _searchKeyword;
        private UserAccountEntity _selectedUser;
        private bool _isLoading;

        public PaginationController<UserAccountEntity> Paging { get; } = new PaginationController<UserAccountEntity>(10);
        public ObservableCollection<UserAccountEntity> Users => Paging.CurrentPageItems;

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    LoadUsers();
                }
            }
        }

        public UserAccountEntity SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(ToggleLockLabel));
                }
            }
        }

        public bool HasSelection => SelectedUser != null;
        public string ToggleLockLabel => (SelectedUser != null && SelectedUser.IsActive) ? "Khóa Tài Khoản" : "Mở Khóa";

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ToggleLockCommand { get; }
        public ICommand ResetPasswordCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }

        public StaffManagementViewModel()
        {
            _authService = new AuthService();

            RefreshCommand = new RelayCommand(LoadUsers);
            ToggleLockCommand = new RelayCommand(ExecuteToggleLock, () => HasSelection);
            ResetPasswordCommand = new RelayCommand(ExecuteResetPassword, () => HasSelection);
            AddUserCommand = new RelayCommand(ExecuteAddUser);
            EditUserCommand = new RelayCommand(ExecuteEditUser, () => HasSelection);

            LoadUsers();
        }

        private void ExecuteAddUser()
        {
            var vm = new StaffDetailViewModel(null);
            if (DialogService.ShowStaffDetailDialog(vm) == true)
            {
                LoadUsers();
            }
        }

        private void ExecuteEditUser()
        {
            if (SelectedUser == null) return;
            var vm = new StaffDetailViewModel(SelectedUser);
            if (DialogService.ShowStaffDetailDialog(vm) == true)
            {
                LoadUsers();
            }
        }

        public void LoadUsers()
        {
            try
            {
                IsLoading = true;
                var list = _authService.GetStaffUsers();
                if (list != null && !string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    string kw = SearchKeyword.Trim().ToLower();
                    list = list.Where(u => (u.Username != null && u.Username.ToLower().Contains(kw)) ||
                                           (u.FullName != null && u.FullName.ToLower().Contains(kw)) ||
                                           (u.RoleName != null && u.RoleName.ToLower().Contains(kw))).ToList();
                }
                Paging.SetSource(list);
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
            if (SelectedUser == null) return;

            if (string.Equals(SelectedUser.Username, "admin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(SelectedUser.RoleName, "ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                DialogService.ShowWarning("Tài khoản Quản trị viên (ADMIN) là tài khoản tối cao của hệ thống, không thể vô hiệu hóa hoặc ngừng hoạt động.");
                return;
            }

            bool newActive = !SelectedUser.IsActive;
            bool ok = _authService.ToggleUserStatus(SelectedUser.UserId, newActive, out string errorMsg);
            if (ok)
            {
                DialogService.ShowInformation($"Cập nhật trạng thái tài khoản '{SelectedUser.Username}' thành công!");
                LoadUsers();
            }
            else
            {
                DialogService.ShowWarning($"Lỗi cập nhật trạng thái: {errorMsg}");
            }
        }

        private void ExecuteResetPassword()
        {
            if (SelectedUser == null) return;

            if (DialogService.ShowConfirmation($"Bạn có chắc chắn muốn đặt lại mật khẩu về mặc định cho tài khoản '{SelectedUser.Username}'?", "Xác Nhận Đặt Lại Mật Khẩu"))
            {
                bool ok = _authService.ResetDefaultPassword(SelectedUser.UserId, out string defaultPass, out string errorMsg);
                if (ok)
                {
                    DialogService.ShowInformation($"Đã đặt lại mật khẩu thành công!\r\nMật khẩu mới: {defaultPass}", "Mật Khẩu Mới");
                }
                else
                {
                    DialogService.ShowError($"Lỗi reset mật khẩu: {errorMsg}");
                }
            }
        }
    }
}
