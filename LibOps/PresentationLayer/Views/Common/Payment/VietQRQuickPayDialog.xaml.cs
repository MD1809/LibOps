using System.Windows;
using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Views.Payment
{
    /// <summary>
    /// Interaction logic for VietQRQuickPayDialog.xaml
    /// </summary>
    public partial class VietQRQuickPayDialog : Window
    {
        public VietQRQuickPayDialog(VietQRQuickPayViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            if (viewModel != null)
            {
                viewModel.RequestClose += (result) =>
                {
                    DialogResult = result;
                    Close();
                };
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
