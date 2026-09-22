using System.Windows;
using System.Windows.Input;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        private readonly LoginViewModel _viewModel;

        public UserSessionDto LoggedInSession { get; private set; }

        public LoginView()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel();
            DataContext = _viewModel;

            _viewModel.LoginSuccessful += OnLoginSuccessful;
            _viewModel.RequestClose += OnRequestClose;

            Loaded += (s, e) => txtUsername.Focus();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.LoginCommand.Execute(pwdPassword.Password);
        }

        private void PwdPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.LoginCommand.Execute(pwdPassword.Password);
            }
            else if (e.Key == Key.Escape)
            {
                OnRequestClose();
            }
        }

        private void OnLoginSuccessful(UserSessionDto session)
        {
            LoggedInSession = session;
            DialogResult = true;
        }

        private void OnRequestClose()
        {
            DialogResult = false;
        }
    }
}
