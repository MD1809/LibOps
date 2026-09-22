using System;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private string _username;
        private string _errorMessage;
        private bool _isAuthenticating;

        public event Action<UserSessionDto> LoginSuccessful;
        public event Action RequestClose;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsAuthenticating
        {
            get => _isAuthenticating;
            set
            {
                if (SetProperty(ref _isAuthenticating, value))
                {
                    OnPropertyChanged(nameof(LoginButtonText));
                    ((RelayCommand<string>)LoginCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string LoginButtonText => IsAuthenticating ? "ĐANG XÁC THỰC..." : "ĐĂNG NHẬP HỆ THỐNG";

        public ICommand LoginCommand { get; }
        public ICommand CloseCommand { get; }

        public LoginViewModel()
        {
            _authService = new AuthService();
            LoginCommand = new RelayCommand<string>(ExecuteLogin, CanExecuteLogin);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        private bool CanExecuteLogin(string password)
        {
            return !IsAuthenticating && !string.IsNullOrWhiteSpace(Username);
        }

        private void ExecuteLogin(string password)
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Vui lòng nhập tên đăng nhập.";
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ErrorMessage = "Vui lòng nhập mật khẩu.";
                return;
            }

            try
            {
                IsAuthenticating = true;

                var session = _authService.AuthenticateUser(Username.Trim(), password, out string authError);
                if (session != null)
                {
                    LoginSuccessful?.Invoke(session);
                }
                else
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(authError) ? "Tên đăng nhập hoặc mật khẩu không đúng." : authError;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Lỗi xác thực hệ thống: " + ex.Message;
            }
            finally
            {
                IsAuthenticating = false;
            }
        }

        private void ExecuteClose()
        {
            RequestClose?.Invoke();
        }
    }
}
