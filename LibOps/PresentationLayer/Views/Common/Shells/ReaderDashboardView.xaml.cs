using System.Windows;
using LibOps.DataModels.DataTransferObjects;

namespace LibOps.PresentationLayer.Views
{
    public partial class ReaderDashboardView : Window
    {
        public ReaderDashboardView(UserSessionDto session)
        {
            InitializeComponent();
            DataContext = new ViewModels.ReaderDashboardViewModel(session, this);
        }
    }
}
