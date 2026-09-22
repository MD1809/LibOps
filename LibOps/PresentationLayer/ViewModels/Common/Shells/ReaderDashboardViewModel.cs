using System;
using System.Windows;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;
using LibOps.PresentationLayer.Services;

namespace LibOps.PresentationLayer.ViewModels
{
    /// <summary>
    /// ViewModel chính (Shell / Host) cho Cổng Thông Tin Độc Giả, điều hướng động giữa 6 phân hệ
    /// </summary>
    public class ReaderDashboardViewModel : ViewModelBase
    {
        private readonly UserSessionDto _currentSession;
        private readonly Window _parentWindow;

        private ViewModelBase _currentView;
        private string _activeMenuName = "Home";
        private bool _isSidebarCollapsed;

        public ViewModelBase CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string ActiveMenuName
        {
            get => _activeMenuName;
            set => SetProperty(ref _activeMenuName, value);
        }

        public bool IsSidebarCollapsed
        {
            get => _isSidebarCollapsed;
            set
            {
                if (SetProperty(ref _isSidebarCollapsed, value))
                {
                    OnPropertyChanged(nameof(SidebarWidth));
                    OnPropertyChanged(nameof(SidebarToggleToolTip));
                }
            }
        }

        public double SidebarWidth => IsSidebarCollapsed ? 68 : 250;
        public string SidebarToggleToolTip => IsSidebarCollapsed ? "Mở rộng thanh Menu" : "Thu gọn thanh Menu";

        public string MemberDisplayName => _currentSession?.FullName ?? "Độc Giả";
        public string MemberUsername => _currentSession?.Username ?? "reader";

        // Navigation Commands
        public ICommand ToggleSidebarCommand { get; }
        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateOpacCommand { get; }
        public ICommand NavigateLoansCommand { get; }
        public ICommand NavigateFinanceCommand { get; }
        public ICommand NavigateRequestsCommand { get; }
        public ICommand NavigateProfileCommand { get; }
        public ICommand LogoutCommand { get; }

        public ReaderDashboardViewModel(UserSessionDto session, Window window)
        {
            _currentSession = session ?? AuthService.CurrentSession;
            _parentWindow = window;

            ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);

            NavigateHomeCommand = new RelayCommand(() => NavigateTo("Home", new ReaderHomeViewModel(NavigateTo)));
            NavigateOpacCommand = new RelayCommand(() => NavigateTo("OpacSearch", new ReaderOpacSearchViewModel(NavigateTo)));
            NavigateLoansCommand = new RelayCommand(() => NavigateTo("Loans", new ReaderLoansViewModel(_currentSession)));
            NavigateFinanceCommand = new RelayCommand(() => NavigateTo("Finance", new ReaderFinanceViewModel(_currentSession)));
            NavigateRequestsCommand = new RelayCommand(() => NavigateTo("Requests", new ReaderSelfServiceRequestsViewModel(_currentSession)));
            NavigateProfileCommand = new ReaderProfileViewModel(_currentSession) != null 
                ? new RelayCommand(() => NavigateTo("Profile", new ReaderProfileViewModel(_currentSession)))
                : null;

            LogoutCommand = new RelayCommand(ExecuteLogout);

            // Mặc định nạp màn hình Trang Chủ Khám Phá
            NavigateTo("Home", new ReaderHomeViewModel(NavigateTo));
        }

        public void NavigateTo(string menuName, ViewModelBase viewModel)
        {
            ActiveMenuName = menuName;
            CurrentView = viewModel;
        }

        private void ExecuteLogout()
        {
            if (DialogService.ShowConfirmation("Bạn có muốn đăng xuất khỏi Cổng Độc Giả không?", "Xác Nhận Đăng Xuất"))
            {
                AuthService.CurrentSession = null;
                _parentWindow?.Close();
            }
        }
    }
}
