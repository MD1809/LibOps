using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.Entities;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class StaffDetailViewModel : ViewModelBase
    {
        private readonly AuthService _authService;

        private int _userId;
        private string _username;
        private string _initialPassword = "LibOps@" + DateTime.Now.Year;
        private string _fullName;
        private string _email;
        private string _phoneNumber;
        private int _selectedRoleId = 2; // Default LIBRARIAN
        private bool _isActive = true;
        private string _errorMessage;
        private bool _isSuccess;

        public string DialogTitle => _userId == 0 ? "THÊM MỚI TÀI KHOẢN NHÂN VIÊN" : "CẬP NHẬT TÀI KHOẢN NHÂN VIÊN";
        public bool IsNewUser => _userId == 0;
        public bool IsUsernameReadOnly => !IsNewUser;
        public bool IsAdminUser => string.Equals(_username, "admin", StringComparison.OrdinalIgnoreCase);
        public bool CanChangeActiveStatus => !IsAdminUser;

        public int UserId
        {
            get => _userId;
            set
            {
                if (SetProperty(ref _userId, value))
                {
                    OnPropertyChanged(nameof(DialogTitle));
                    OnPropertyChanged(nameof(IsNewUser));
                    OnPropertyChanged(nameof(IsUsernameReadOnly));
                }
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    OnPropertyChanged(nameof(IsAdminUser));
                    OnPropertyChanged(nameof(CanChangeActiveStatus));
                }
            }
        }
        public string InitialPassword { get => _initialPassword; set => SetProperty(ref _initialPassword, value); }
        public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
        public string Email { get => _email; set => SetProperty(ref _email, value); }
        public string PhoneNumber { get => _phoneNumber; set => SetProperty(ref _phoneNumber, value); }
        public int SelectedRoleId { get => _selectedRoleId; set => SetProperty(ref _selectedRoleId, value); }
        public bool IsActive
        {
            get => IsAdminUser ? true : _isActive;
            set => SetProperty(ref _isActive, IsAdminUser ? true : value);
        }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public bool IsSuccess { get => _isSuccess; set => SetProperty(ref _isSuccess, value); }

        public ObservableCollection<RoleEntity> Roles { get; }

        public ICommand SaveCommand { get; }

        public event Action RequestClose;

        public StaffDetailViewModel(UserAccountEntity user = null)
        {
            _authService = new AuthService();
            Roles = new ObservableCollection<RoleEntity>();
            SaveCommand = new RelayCommand(SaveUser);

            LoadRoles();

            if (user != null)
            {
                UserId = user.UserId;
                Username = user.Username;
                FullName = user.FullName;
                Email = user.Email;
                PhoneNumber = user.PhoneNumber;
                SelectedRoleId = user.RoleId;
                IsActive = user.IsActive;
            }
        }

        private void LoadRoles()
        {
            try
            {
                Roles.Clear();
                foreach (var r in _authService.GetAllRoles())
                {
                    // Chỉ nạp ADMIN và LIBRARIAN cho phân hệ nhân sự nội bộ
                    if (r.RoleName == "ADMIN" || r.RoleName == "LIBRARIAN")
                    {
                        Roles.Add(r);
                    }
                }
                if (Roles.Count > 0 && SelectedRoleId == 0)
                {
                    SelectedRoleId = Roles[0].RoleId;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Không thể tải danh sách vai trò: " + ex.Message;
            }
        }

        private void SaveUser()
        {
            ErrorMessage = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(Username))
                {
                    ErrorMessage = "Vui lòng nhập tên đăng nhập.";
                    return;
                }

                if (!LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidUsername(Username))
                {
                    ErrorMessage = "Tên đăng nhập không hợp lệ (3-50 ký tự, không dấu cách).";
                    return;
                }

                if (string.IsNullOrWhiteSpace(FullName))
                {
                    ErrorMessage = "Vui lòng nhập họ và tên nhân viên.";
                    return;
                }

                if (FullName.Trim().Length > 100)
                {
                    ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.";
                    return;
                }

                if (UserId == 0 && (string.IsNullOrWhiteSpace(InitialPassword) || InitialPassword.Length < 6))
                {
                    ErrorMessage = "Mật khẩu khởi tạo phải từ 6 ký tự trở lên.";
                    return;
                }

                if (!string.IsNullOrWhiteSpace(Email) && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidEmail(Email))
                {
                    ErrorMessage = "Địa chỉ Email không đúng định dạng.";
                    return;
                }

                if (!string.IsNullOrWhiteSpace(PhoneNumber) && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidPhoneNumber(PhoneNumber))
                {
                    ErrorMessage = "Số điện thoại không đúng định dạng (10 chữ số).";
                    return;
                }

                var entity = new UserAccountEntity
                {
                    UserId = UserId,
                    Username = Username.Trim(),
                    FullName = FullName.Trim(),
                    Email = Email?.Trim(),
                    PhoneNumber = LibOps.CommonUtilities.Validation.InputValidationUtility.NormalizePhoneNumber(PhoneNumber),
                    RoleId = SelectedRoleId,
                    IsActive = IsActive
                };

                bool ok;
                string error;

                if (UserId == 0)
                {
                    ok = _authService.CreateUserAccount(entity, InitialPassword, out error);
                }
                else
                {
                    ok = _authService.UpdateUserAccount(entity, out error);
                }

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
                ErrorMessage = "Đã xảy ra lỗi khi lưu thông tin nhân viên: " + ex.Message;
            }
        }
    }
}
