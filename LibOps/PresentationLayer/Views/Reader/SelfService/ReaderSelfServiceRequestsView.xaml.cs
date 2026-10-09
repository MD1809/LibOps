using System.Windows.Controls;
using System.Windows.Input;
using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Views
{
    public partial class ReaderSelfServiceRequestsView : UserControl
    {
        public ReaderSelfServiceRequestsView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ReaderSelfServiceRequestsViewModel vm && vm.SelectedRequest != null)
            {
                if (vm.ViewDetailCommand.CanExecute(vm.SelectedRequest))
                {
                    vm.ViewDetailCommand.Execute(vm.SelectedRequest);
                }
            }
        }
    }
}
