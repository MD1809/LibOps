using System.Windows;
using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Views.Members
{
    /// <summary>
    /// Interaction logic for ExpiredDebtDialog.xaml
    /// </summary>
    public partial class ExpiredDebtDialog : Window
    {
        public ExpiredDebtDialog(ExpiredDebtViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
