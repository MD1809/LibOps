using System;
using System.Configuration;

namespace LibOps.DataAccessLayer.DatabaseConnection
{
    /// <summary>
    /// Lớp quản lý và cung cấp chuỗi kết nối đến cơ sở dữ liệu SQL Server từ App.config
    /// </summary>
    public static class DatabaseConfiguration
    {
        private const string ConnectionStringName = "LibOpsDatabaseConnection";
        private const string FallbackConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=LibOpsDb;Integrated Security=True;TrustServerCertificate=True;";

        /// <summary>
        /// Lấy chuỗi kết nối hiện hành từ App.config
        /// </summary>
        public static string GetConnectionString()
        {
            var setting = ConfigurationManager.ConnectionStrings[ConnectionStringName];
            if (setting != null && !string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                return setting.ConnectionString;
            }

            return FallbackConnectionString;
        }
    }
}
