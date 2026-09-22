using System.Windows;
using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Views
{
    public partial class ChangePasswordDialog : Window
    {
        private readonly ChangePasswordViewModel _viewModel;

        public ChangePasswordDialog(ChangePasswordViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            viewModel.RequestClose += () =>
            {
                DialogResult = true;
                Close();
            };
        }

        private void PbCurrent_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CurrentPassword = PbCurrent.Password;
            }
        }

        private void PbNew_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.NewPassword = PbNew.Password;
            }
        }

        private void PbConfirm_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ConfirmPassword = PbConfirm.Password;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
