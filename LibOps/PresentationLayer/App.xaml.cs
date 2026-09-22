using System;
using System.Windows;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.PresentationLayer.Views;

namespace LibOps.PresentationLayer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Khởi tạo & cập nhật Schema Cơ sở dữ liệu
            DatabaseConnectionHelper.EnsureDatabaseSchemaUpToDate();

            // 3. Khởi chạy giao diện người dùng theo vòng lặp phiên làm việc
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            bool keepRunning = true;
            while (keepRunning)
            {
                var loginView = new LoginView();
                if (loginView.ShowDialog() == true && loginView.LoggedInSession != null)
                {
                    var session = loginView.LoggedInSession;
                    if (session.IsReader)
                    {
                        var readerDashboard = new ReaderDashboardView(session);
                        readerDashboard.ShowDialog();
                    }
                    else
                    {
                        var dashboard = new MainDashboardView(session);
                        dashboard.ShowDialog();
                    }
                }
                else
                {
                    keepRunning = false;
                }
            }

            Shutdown();
        }
    }
}
