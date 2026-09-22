using System.Windows;
using System.Windows.Controls;
using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Views
{
    /// <summary>
    /// Interaction logic for ReturnBookProcessView.xaml
    /// </summary>
    public partial class ReturnBookProcessView : UserControl
    {
        public ReturnBookProcessView()
        {
            InitializeComponent();
            var vm = new ReturnBookProcessViewModel();
            DataContext = vm;

            vm.ShowMessageRequested += msg => MessageBox.Show(msg, "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
