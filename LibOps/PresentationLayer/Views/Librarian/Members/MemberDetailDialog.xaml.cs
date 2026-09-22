using System.Windows;
using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Views
{
    public partial class MemberDetailDialog : Window
    {
        public MemberDetailDialog(MemberDetailViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += () =>
            {
                DialogResult = true;
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
