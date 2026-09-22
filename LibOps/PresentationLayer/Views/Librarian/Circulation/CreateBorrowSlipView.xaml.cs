using System.Windows;
using System.Windows.Controls;
using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Views
{
    /// <summary>
    /// Interaction logic for CreateBorrowSlipView.xaml
    /// </summary>
    public partial class CreateBorrowSlipView : UserControl
    {
        public CreateBorrowSlipView()
        {
            InitializeComponent();
            var vm = new CreateBorrowSlipViewModel();
            DataContext = vm;

            vm.ShowMessageRequested += msg => MessageBox.Show(msg, "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
