using System.Windows.Controls;
using System.Windows.Input;
using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Views
{
    /// <summary>
    /// Interaction logic for BookListView.xaml
    /// </summary>
    public partial class BookListView : UserControl
    {
        public BookListView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is BookListViewModel vm && vm.ViewDetailCommand.CanExecute(null))
            {
                vm.ViewDetailCommand.Execute(null);
            }
        }
    }
}

