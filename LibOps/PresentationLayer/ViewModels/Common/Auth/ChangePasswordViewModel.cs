using System;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class ChangePasswordViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private readonly int _userId;

        private string _currentPassword;
        private string _newPassword;
        private string _confirmPassword;
        private string _errorMessage;
        private bool _isSuccess;

        public string CurrentPassword { get => _currentPassword; set => SetProperty(ref _currentPassword, value); }
        public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }
        public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public bool IsSuccess { get => _isSuccess; set => SetProperty(ref _isSuccess, value); }

        public ICommand SaveCommand { get; }

        public event Action RequestClose;

        public ChangePasswordViewModel(int userId)
        {
            _authService = new AuthService();
            _userId = userId;
            SaveCommand = new RelayCommand(SaveChangePassword);
        }

        private void SaveChangePassword()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(CurrentPassword))
            {
                ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
            {
                ErrorMessage = "Mật khẩu mới phải có độ dài từ 6 ký tự trở lên.";
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "Mật khẩu xác nhận không trùng khớp.";
                return;
            }

            bool ok = _authService.ChangePassword(_userId, CurrentPassword, NewPassword, ConfirmPassword, out string error);
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
    }
}
