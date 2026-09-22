using System.Windows;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Views
{
    /// <summary>
    /// Interaction logic for MainDashboardView.xaml
    /// </summary>
    public partial class MainDashboardView : Window
    {
        private readonly MainDashboardViewModel _viewModel;

        public MainDashboardView(UserSessionDto session)
        {
            InitializeComponent();
            _viewModel = new MainDashboardViewModel(session);
            DataContext = _viewModel;

            _viewModel.RequestLogout += OnRequestLogout;
        }

        private void OnRequestLogout()
        {
            Close();
        }
    }
}
