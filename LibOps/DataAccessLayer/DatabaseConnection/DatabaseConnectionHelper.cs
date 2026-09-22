using System;
using System.Data;
using System.Data.SqlClient;

namespace LibOps.DataAccessLayer.DatabaseConnection
{
    /// <summary>
    /// Tiện ích kết nối và thực thi các câu lệnh truy vấn SQL Server an toàn bằng Parameterized Command
    /// </summary>
    public static class DatabaseConnectionHelper
    {
        /// <summary>
        /// Tạo và mở một kết nối SqlConnection mới
        /// </summary>
        public static SqlConnection CreateAndOpenConnection()
        {
            string connectionString = DatabaseConfiguration.GetConnectionString();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Kiểm tra tính khả dụng của kết nối đến SQL Server
        /// </summary>
        public static bool TestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using (var connection = CreateAndOpenConnection())
                {
                    return connection.State == ConnectionState.Open;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Thực thi câu lệnh SQL INSERT / UPDATE / DELETE và trả về số dòng bị tác động
        /// </summary>
        public static int ExecuteNonQuery(string sqlCommandText, SqlParameter[] parameters = null, SqlTransaction transaction = null)
        {
            if (transaction != null)
            {
                using (var command = new SqlCommand(sqlCommandText, transaction.Connection, transaction))
                {
                    command.CommandType = CommandType.Text;
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteNonQuery();
                }
            }
            else
            {
                using (var connection = CreateAndOpenConnection())
                using (var command = new SqlCommand(sqlCommandText, connection))
                {
                    command.CommandType = CommandType.Text;
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Thực thi câu lệnh SQL và trả về giá trị của cột đầu tiên ở dòng đầu tiên (Scalar)
        /// </summary>
        public static object ExecuteScalar(string sqlCommandText, SqlParameter[] parameters = null, SqlTransaction transaction = null)
        {
            if (transaction != null)
            {
                using (var command = new SqlCommand(sqlCommandText, transaction.Connection, transaction))
                {
                    command.CommandType = CommandType.Text;
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteScalar();
                }
            }
            else
            {
                using (var connection = CreateAndOpenConnection())
                using (var command = new SqlCommand(sqlCommandText, connection))
                {
                    command.CommandType = CommandType.Text;
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Thực thi câu lệnh SQL SELECT và nạp kết quả vào một DataTable
        /// </summary>
        public static DataTable ExecuteQueryToDataTable(string sqlCommandText, SqlParameter[] parameters = null, SqlTransaction transaction = null)
        {
            var dataTable = new DataTable();

            if (transaction != null)
            {
                using (var command = new SqlCommand(sqlCommandText, transaction.Connection, transaction))
                {
                    command.CommandType = CommandType.Text;
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            else
            {
                using (var connection = CreateAndOpenConnection())
                using (var command = new SqlCommand(sqlCommandText, connection))
                {
                    command.CommandType = CommandType.Text;
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }

            return dataTable;
        }

        /// <summary>
        /// Thực thi một câu lệnh SQL và nuốt lỗi an toàn nếu có
        /// </summary>
        private static void ExecuteSilentNonQuery(string sql)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    ExecuteNonQuery(sql);
                }
            }
            catch
            {
                // Bỏ qua lỗi để tiếp tục các bước migration tiếp theo
            }
        }

        /// <summary>
        /// Đảm bảo cấu trúc cơ sở dữ liệu luôn cập nhật đầy đủ các trường mới (tự động migration)
        /// </summary>
        public static void EnsureDatabaseSchemaUpToDate()
        {
            // 1. Migration Cột TotalDebt cho Bảng Members (Bắt buộc chạy độc lập trước)
            ExecuteSilentNonQuery(@"
                IF OBJECT_ID(N'dbo.Members', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.Members', N'TotalDebt') IS NULL
                    BEGIN
                        ALTER TABLE dbo.Members ADD TotalDebt DECIMAL(18,2) NOT NULL CONSTRAINT DF_Members_TotalDebt DEFAULT (0);
                    END
                END
            ");

            // 2. Migration Cột RenewalCount cho Bảng BorrowSlips
            ExecuteSilentNonQuery(@"
                IF OBJECT_ID(N'dbo.BorrowSlips', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.BorrowSlips', N'RenewalCount') IS NULL
                    BEGIN
                        ALTER TABLE dbo.BorrowSlips ADD RenewalCount INT NOT NULL CONSTRAINT DF_BorrowSlips_RenewalCount DEFAULT (0);
                    END
                END
            ");

            // 3. Migration Tham Số Cấu Hình Hệ Thống (SystemSettings)
            ExecuteSilentNonQuery(@"
                IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = 'MAX_RENEWAL_COUNT')
                        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES ('MAX_RENEWAL_COUNT', '1', N'Số lần gia hạn tối đa cho một lượt mượn sách (Lần)');

                    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = 'RENEWAL_DAYS')
                        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES ('RENEWAL_DAYS', '7', N'Số ngày gia hạn thêm mỗi lần gia hạn (Ngày)');

                    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = 'MEMBER_CARD_ANNUAL_FEE')
                        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES ('MEMBER_CARD_ANNUAL_FEE', '50000', N'Phí thường niên khi gia hạn thẻ độc giả (VNĐ/năm)');

                    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = 'CARD_ISSUANCE_FEE')
                        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES ('CARD_ISSUANCE_FEE', '50000', N'Phí dịch vụ phát hành thẻ độc giả mới (Không hoàn lại - VNĐ)');

                    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = 'MEMBER_CARD_DEFAULT_DEPOSIT')
                        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES ('MEMBER_CARD_DEFAULT_DEPOSIT', '200000', N'Mức tiền cọc thế chân tối thiểu để mượn sách (VNĐ)');

                    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = 'LOST_BOOK_FINE_RATE')
                        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES ('LOST_BOOK_FINE_RATE', '2.0', N'Hệ số bồi thường khi làm mất sách (200% giá bìa sách)');
                    ELSE
                        UPDATE dbo.SystemSettings SET SettingValue = '2.0', Description = N'Hệ số bồi thường khi làm mất sách (200% giá bìa sách)' WHERE SettingKey = 'LOST_BOOK_FINE_RATE' AND SettingValue IN ('1.0', '1', '100', '100.00', '1.00', '1.000');

                    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = 'DAMAGED_BOOK_FINE_RATE')
                        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES ('DAMAGED_BOOK_FINE_RATE', '0.5', N'Hệ số bồi thường khi làm hư hại sách (50% giá bìa sách)');

                    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = 'SMTP_HOST')
                        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES ('SMTP_HOST', 'smtp.gmail.com', N'Máy chủ SMTP gửi email thông báo hệ thống');

                    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = 'SMTP_PORT')
                        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES ('SMTP_PORT', '587', N'Cổng kết nối máy chủ SMTP (587 TLS/STARTTLS)');

                    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = 'SMTP_ENABLE_SSL')
                        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES ('SMTP_ENABLE_SSL', 'true', N'Bật mã hóa SSL/TLS an toàn khi gửi email (true/false)');

                    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = 'SMTP_USERNAME')
                        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES ('SMTP_USERNAME', 'notification.libops@gmail.com', N'Tài khoản email hệ thống gửi thư cho độc giả');

                    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = 'SMTP_PASSWORD')
                        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES ('SMTP_PASSWORD', '', N'Mật khẩu ứng dụng (App Password 16 ký tự) của email hệ thống');

                    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = 'SMTP_FROM_NAME')
                        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES ('SMTP_FROM_NAME', N'Hệ Thống Thư Viện LibOps', N'Tên hiển thị người gửi email');

                    IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = 'SYSTEM_EMAIL_NOTIFICATION_ENABLED')
                        INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES ('SYSTEM_EMAIL_NOTIFICATION_ENABLED', 'true', N'Công tắc bật/tắt tính năng gửi email tự động toàn hệ thống');
                END
            ");

            // 4. Migration Vai Trò READER
            ExecuteSilentNonQuery(@"
                IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'READER')
                        INSERT INTO dbo.Roles (RoleName, Description) VALUES ('READER', N'Độc giả sử dụng cổng thông tin tự phục vụ');
                END
            ");

            // 5. Migration UserAccounts.MemberId
            ExecuteSilentNonQuery(@"
                IF OBJECT_ID(N'dbo.UserAccounts', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.UserAccounts', N'MemberId') IS NULL
                    BEGIN
                        ALTER TABLE dbo.UserAccounts ADD MemberId INT NULL;
                        IF OBJECT_ID(N'dbo.Members', N'U') IS NOT NULL
                        BEGIN
                            ALTER TABLE dbo.UserAccounts ADD CONSTRAINT FK_UserAccounts_Members FOREIGN KEY (MemberId) REFERENCES dbo.Members(MemberId);
                        END
                    END
                END
            ");

            // 6. Migration Bảng ReaderRequests
            ExecuteSilentNonQuery(@"
                IF OBJECT_ID(N'dbo.ReaderRequests', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ReaderRequests (
                        RequestId INT IDENTITY(1,1) NOT NULL,
                        RequestCode NVARCHAR(30) NOT NULL,
                        MemberId INT NOT NULL,
                        RequestType NVARCHAR(30) NOT NULL,
                        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_ReaderRequests_Status DEFAULT ('PENDING'),
                        Amount DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReaderRequests_Amount DEFAULT (0),
                        PayoutMethod NVARCHAR(30) NOT NULL CONSTRAINT DF_ReaderRequests_PayoutMethod DEFAULT ('BANK_TRANSFER'),
                        BankName NVARCHAR(100) NULL,
                        BankAccountNumber NVARCHAR(50) NULL,
                        BankAccountHolder NVARCHAR(100) NULL,
                        Reason NVARCHAR(250) NULL,
                        RequestDate DATETIME NOT NULL CONSTRAINT DF_ReaderRequests_RequestDate DEFAULT (GETDATE()),
                        ProcessedByUserId INT NULL,
                        ProcessedDate DATETIME NULL,
                        StaffNotes NVARCHAR(250) NULL,
                        CONSTRAINT PK_ReaderRequests PRIMARY KEY CLUSTERED (RequestId),
                        CONSTRAINT UQ_ReaderRequests_RequestCode UNIQUE (RequestCode),
                        CONSTRAINT FK_ReaderRequests_Members FOREIGN KEY (MemberId) REFERENCES dbo.Members(MemberId),
                        CONSTRAINT FK_ReaderRequests_UserAccounts FOREIGN KEY (ProcessedByUserId) REFERENCES dbo.UserAccounts(UserId)
                    );
                END
            ");

            // 6b. Migration Bảng ServiceFeeReceipts (Doanh thu phí dịch vụ không hoàn lại)
            ExecuteSilentNonQuery(@"
                IF OBJECT_ID(N'dbo.ServiceFeeReceipts', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ServiceFeeReceipts (
                        ReceiptId INT IDENTITY(1,1) NOT NULL,
                        ReceiptCode NVARCHAR(30) NOT NULL,
                        MemberId INT NOT NULL,
                        FeeType NVARCHAR(30) NOT NULL,
                        Amount DECIMAL(18,2) NOT NULL,
                        PaymentMethod NVARCHAR(30) NOT NULL CONSTRAINT DF_ServiceFeeReceipts_PaymentMethod DEFAULT ('CASH'),
                        PaymentDate DATETIME NOT NULL CONSTRAINT DF_ServiceFeeReceipts_PaymentDate DEFAULT (GETDATE()),
                        CollectedByUserId INT NOT NULL,
                        Notes NVARCHAR(250) NULL,
                        CONSTRAINT PK_ServiceFeeReceipts PRIMARY KEY CLUSTERED (ReceiptId),
                        CONSTRAINT UQ_ServiceFeeReceipts_ReceiptCode UNIQUE (ReceiptCode),
                        CONSTRAINT FK_ServiceFeeReceipts_Members FOREIGN KEY (MemberId) REFERENCES dbo.Members(MemberId),
                        CONSTRAINT FK_ServiceFeeReceipts_UserAccounts FOREIGN KEY (CollectedByUserId) REFERENCES dbo.UserAccounts(UserId)
                    );
                END
            ");

            // 7. Seed Vai Trò Chuẩn & Tài Khoản Mặc Định (Admin & Librarian)
            ExecuteSilentNonQuery(@"
                IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'ADMIN')
                        INSERT INTO dbo.Roles (RoleName, Description) VALUES ('ADMIN', N'Quản trị viên toàn quyền hệ thống');
                    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'LIBRARIAN')
                        INSERT INTO dbo.Roles (RoleName, Description) VALUES ('LIBRARIAN', N'Thủ thư / Cán bộ nghiệp vụ vận hành thư viện');
                    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'READER')
                        INSERT INTO dbo.Roles (RoleName, Description) VALUES ('READER', N'Độc giả sử dụng cổng thông tin tự phục vụ');
                END

                IF OBJECT_ID(N'dbo.UserAccounts', N'U') IS NOT NULL
                BEGIN
                    DECLARE @AdminRoleId INT = (SELECT TOP 1 RoleId FROM dbo.Roles WHERE RoleName = 'ADMIN');
                    DECLARE @LibrarianRoleId INT = (SELECT TOP 1 RoleId FROM dbo.Roles WHERE RoleName = 'LIBRARIAN');

                    -- 1. Tài khoản Quản trị viên: Username = 'Admin', Mật khẩu = 'Admin@libops'
                    IF @AdminRoleId IS NOT NULL
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM dbo.UserAccounts WHERE LOWER(Username) = 'admin')
                        BEGIN
                            INSERT INTO dbo.UserAccounts (Username, PasswordHash, PasswordSalt, FullName, Email, PhoneNumber, RoleId, MemberId, IsActive)
                            VALUES (N'Admin', N'A303B1934621E50C4D142F63871D1A402852394605022F9C128ECA1E0D58C24D', N'LIB_OPS_SALT_2026', N'Quản Trị Viên Hệ Thống', N'admin@libops.vn', N'0912000111', @AdminRoleId, NULL, 1);
                        END
                        ELSE
                        BEGIN
                            UPDATE dbo.UserAccounts
                            SET Username = N'Admin',
                                PasswordHash = N'A303B1934621E50C4D142F63871D1A402852394605022F9C128ECA1E0D58C24D',
                                PasswordSalt = N'LIB_OPS_SALT_2026',
                                RoleId = @AdminRoleId,
                                IsActive = 1
                            WHERE LOWER(Username) = 'admin';
                        END
                    END

                    -- 2. Tài khoản Thủ thư: Username = 'Librarian', Mật khẩu = 'Librarian@libops'
                    IF @LibrarianRoleId IS NOT NULL
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM dbo.UserAccounts WHERE LOWER(Username) = 'librarian')
                        BEGIN
                            INSERT INTO dbo.UserAccounts (Username, PasswordHash, PasswordSalt, FullName, Email, PhoneNumber, RoleId, MemberId, IsActive)
                            VALUES (N'Librarian', N'EA0A9503A235699897D0E19C74D5427934651F78E6BED78B4327E844BE6DEBD4', N'LIB_OPS_SALT_2026', N'Thủ Thư Thư Viện', N'librarian@libops.vn', N'0988111222', @LibrarianRoleId, NULL, 1);
                        END
                        ELSE
                        BEGIN
                            UPDATE dbo.UserAccounts
                            SET Username = N'Librarian',
                                PasswordHash = N'EA0A9503A235699897D0E19C74D5427934651F78E6BED78B4327E844BE6DEBD4',
                                PasswordSalt = N'LIB_OPS_SALT_2026',
                                RoleId = @LibrarianRoleId,
                                IsActive = 1
                            WHERE LOWER(Username) = 'librarian';
                        END
                    END
                END
            ");

            // 8. Migration Bảng Quan Hệ Sách - Nhiều Tác Giả (BookAuthors)
            ExecuteSilentNonQuery(@"
                IF OBJECT_ID(N'dbo.BookAuthors', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.BookAuthors (
                        BookId INT NOT NULL,
                        AuthorId INT NOT NULL,
                        CONSTRAINT PK_BookAuthors PRIMARY KEY CLUSTERED (BookId, AuthorId),
                        CONSTRAINT FK_BookAuthors_Books FOREIGN KEY (BookId) REFERENCES dbo.Books(BookId) ON DELETE CASCADE,
                        CONSTRAINT FK_BookAuthors_Authors FOREIGN KEY (AuthorId) REFERENCES dbo.Authors(AuthorId) ON DELETE CASCADE
                    );

                    -- Chuyển tiếp dữ liệu từ AuthorId hiện có của Books vào BookAuthors
                    IF OBJECT_ID(N'dbo.Books', N'U') IS NOT NULL
                    BEGIN
                        INSERT INTO dbo.BookAuthors (BookId, AuthorId)
                        SELECT b.BookId, b.AuthorId
                        FROM dbo.Books b
                        WHERE b.AuthorId IS NOT NULL AND b.AuthorId > 0
                          AND NOT EXISTS (
                              SELECT 1 FROM dbo.BookAuthors ba 
                              WHERE ba.BookId = b.BookId AND ba.AuthorId = b.AuthorId
                          );
                    END
                END
            ");
        }
    }
}
