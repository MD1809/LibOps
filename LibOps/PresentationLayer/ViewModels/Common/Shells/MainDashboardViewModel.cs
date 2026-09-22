using System;
using System.Windows.Input;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class MainDashboardViewModel : ViewModelBase
    {
        private ViewModelBase _currentView;
        private string _activeMenuName = "Dashboard";
        private UserSessionDto _currentSession;

        public UserSessionDto CurrentSession
        {
            get => _currentSession;
            set => SetProperty(ref _currentSession, value);
        }

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

        private bool _isSidebarCollapsed;

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

        public string UserDisplayName => CurrentSession?.FullName ?? "Quản Trị Viên";
        public string UserRoleName => CurrentSession?.RoleName ?? "ADMIN";
        public string SystemTimeDisplay => DateTime.Now.ToString("dd/MM/yyyy - HH:mm");

        // Commands điều hướng & tiện ích
        public ICommand ToggleSidebarCommand { get; }
        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateBooksCommand { get; }
        public ICommand NavigateCopiesCommand { get; }
        public ICommand NavigateCategoriesCommand { get; }
        public ICommand NavigateAuthorsCommand { get; }
        public ICommand NavigatePublishersCommand { get; }
        public ICommand NavigateMetadataCommand { get; }
        public ICommand NavigateMembersCommand { get; }
        public ICommand NavigateBorrowCommand { get; }
        public ICommand NavigateReturnCommand { get; }
        public ICommand NavigateBorrowSlipsCommand { get; }
        public ICommand NavigateReportsCommand { get; }
        public ICommand NavigateUsersCommand { get; }
        public ICommand NavigateSettingsCommand { get; }
        public ICommand NavigateReaderRequestsCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand LogoutCommand { get; }

        public event Action RequestLogout;

        public MainDashboardViewModel(UserSessionDto session)
        {
            CurrentSession = session ?? AuthService.CurrentSession;

            ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);
            NavigateDashboardCommand = new RelayCommand(() => NavigateTo("Dashboard", new DashboardOverviewViewModel()));
            NavigateBooksCommand = new RelayCommand(() => NavigateTo("Books", new BookListViewModel()));
            NavigateCopiesCommand = new RelayCommand(() => NavigateTo("Copies", new BookCopyBarcodeViewModel()));
            NavigateCategoriesCommand = new RelayCommand(() => NavigateTo("Categories", new CategoryManagementViewModel()));
            NavigateAuthorsCommand = new RelayCommand(() => NavigateTo("Authors", new AuthorManagementViewModel()));
            NavigatePublishersCommand = new RelayCommand(() => NavigateTo("Publishers", new PublisherManagementViewModel()));
            NavigateMetadataCommand = new RelayCommand(() => NavigateTo("Categories", new CategoryManagementViewModel()));
            NavigateMembersCommand = new RelayCommand(() => NavigateTo("Members", new MemberListViewModel()));
            NavigateBorrowCommand = new RelayCommand(() => NavigateTo("Borrow", new CreateBorrowSlipViewModel()));
            NavigateReturnCommand = new RelayCommand(() => NavigateTo("Return", new ReturnBookProcessViewModel()));
            NavigateBorrowSlipsCommand = new RelayCommand(() => NavigateTo("BorrowSlips", new BorrowSlipListViewModel()));
            NavigateReportsCommand = new RelayCommand(() => NavigateTo("Reports", new ReportAnalyticsViewModel()));
            NavigateUsersCommand = new RelayCommand(() => NavigateTo("Users", new StaffManagementViewModel()));
            NavigateSettingsCommand = new RelayCommand(() => NavigateTo("Settings", new SystemSettingsViewModel()));
            NavigateReaderRequestsCommand = new RelayCommand(() => NavigateTo("ReaderRequests", new ReaderRequestListViewModel()));
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
            LogoutCommand = new RelayCommand(ExecuteLogout);

            // Mặc định nạp Dashboard Overview
            NavigateDashboardCommand.Execute(null);
        }

        public void NavigateTo(string menuName, ViewModelBase viewModel)
        {
            ActiveMenuName = menuName;
            if (viewModel != null)
            {
                CurrentView = viewModel;
            }
        }

        private void ExecuteChangePassword()
        {
            int userId = CurrentSession?.UserId ?? 1;
            var vm = new ChangePasswordViewModel(userId);
            if (DialogService.ShowChangePasswordDialog(vm) == true)
            {
                DialogService.ShowInformation("Đổi mật khẩu thành công!");
            }
        }

        private void ExecuteLogout()
        {
            AuthService.CurrentSession = null;
            RequestLogout?.Invoke();
        }
    }
}
