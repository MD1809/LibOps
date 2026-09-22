using System.Windows;
using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Views.Members
{
    public partial class MemberRenewCardDialog : Window
    {
        private readonly MemberRenewCardViewModel _viewModel;

        public MemberRenewCardDialog(MemberRenewCardViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.RequestClose += () =>
            {
                DialogResult = _viewModel.IsSuccess;
                Close();
            };
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
