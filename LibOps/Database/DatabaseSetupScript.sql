-- ============================================================================
-- HỆ THỐNG QUẢN LÝ THƯ VIỆN - LIBOPS
-- DATABASE SETUP & SEED DATA SCRIPT (MS SQL SERVER)
-- Mô tả: Khởi tạo toàn bộ 16 bảng CSDL, khóa chính, khóa ngoại, chỉ mục và dữ liệu mẫu (>= 5 bản ghi/bảng)
-- ============================================================================

-- 1. Tạo Cơ Sở Dữ Liệu nếu chưa tồn tại
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'LibOpsDb')
BEGIN
    CREATE DATABASE [LibOpsDb];
END
GO

USE [LibOpsDb];
GO

-- ============================================================================
-- 2. XÓA CÁC BẢNG NẾU ĐÃ TỒN TẠI (THEO THỨ TỰ RÀNG BUỘC KHÓA NGOẠI)
-- ============================================================================
IF OBJECT_ID(N'dbo.ServiceFeeReceipts', N'U') IS NOT NULL DROP TABLE dbo.ServiceFeeReceipts;
IF OBJECT_ID(N'dbo.ReaderRequests', N'U') IS NOT NULL DROP TABLE dbo.ReaderRequests;
IF OBJECT_ID(N'dbo.DepositTransactions', N'U') IS NOT NULL DROP TABLE dbo.DepositTransactions;
IF OBJECT_ID(N'dbo.FineReceipts', N'U') IS NOT NULL DROP TABLE dbo.FineReceipts;
IF OBJECT_ID(N'dbo.ReturnSlipDetails', N'U') IS NOT NULL DROP TABLE dbo.ReturnSlipDetails;
IF OBJECT_ID(N'dbo.BorrowSlipDetails', N'U') IS NOT NULL DROP TABLE dbo.BorrowSlipDetails;
IF OBJECT_ID(N'dbo.BorrowSlips', N'U') IS NOT NULL DROP TABLE dbo.BorrowSlips;
IF OBJECT_ID(N'dbo.BookCopies', N'U') IS NOT NULL DROP TABLE dbo.BookCopies;
IF OBJECT_ID(N'dbo.Books', N'U') IS NOT NULL DROP TABLE dbo.Books;
IF OBJECT_ID(N'dbo.Publishers', N'U') IS NOT NULL DROP TABLE dbo.Publishers;
IF OBJECT_ID(N'dbo.Authors', N'U') IS NOT NULL DROP TABLE dbo.Authors;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.UserAccounts', N'U') IS NOT NULL DROP TABLE dbo.UserAccounts;
IF OBJECT_ID(N'dbo.Members', N'U') IS NOT NULL DROP TABLE dbo.Members;
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NOT NULL DROP TABLE dbo.SystemSettings;
GO

-- ============================================================================
-- 3. KHỞI TẠO CÁC BẢNG DỮ LIỆU
-- ============================================================================

-- 3.1. Bảng Vai Trò Người Dùng (Roles)
CREATE TABLE dbo.Roles (
    RoleId INT IDENTITY(1,1) NOT NULL,
    RoleName NVARCHAR(50) NOT NULL,
    Description NVARCHAR(250) NULL,
    CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (RoleId),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);
GO

-- 3.2. Bảng Độc Giả / Thẻ Thư Viện (Members)
CREATE TABLE dbo.Members (
    MemberId INT IDENTITY(1,1) NOT NULL,
    MemberCardCode NVARCHAR(30) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    Email NVARCHAR(100) NULL,
    IdentityCardNumber NVARCHAR(20) NULL,
    Address NVARCHAR(250) NULL,
    DateOfBirth DATETIME NULL,
    DepositBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Members_DepositBalance DEFAULT (0),
    TotalDebt DECIMAL(18,2) NOT NULL CONSTRAINT DF_Members_TotalDebt DEFAULT (0),
    IssueDate DATETIME NOT NULL CONSTRAINT DF_Members_IssueDate DEFAULT (GETDATE()),
    ExpiryDate DATETIME NOT NULL,
    CardStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_Members_CardStatus DEFAULT ('ACTIVE'),
    Notes NVARCHAR(250) NULL,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_Members_CreatedAt DEFAULT (GETDATE()),
    CONSTRAINT PK_Members PRIMARY KEY CLUSTERED (MemberId),
    CONSTRAINT UQ_Members_MemberCardCode UNIQUE (MemberCardCode)
);
GO

-- 3.3. Bảng Tài Khoản Người Dùng (UserAccounts)
CREATE TABLE dbo.UserAccounts (
    UserId INT IDENTITY(1,1) NOT NULL,
    Username NVARCHAR(50) NOT NULL,
    PasswordHash NVARCHAR(256) NOT NULL,
    PasswordSalt NVARCHAR(128) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NULL,
    PhoneNumber NVARCHAR(20) NULL,
    RoleId INT NOT NULL,
    MemberId INT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_UserAccounts_IsActive DEFAULT (1),
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_UserAccounts_CreatedAt DEFAULT (GETDATE()),
    CONSTRAINT PK_UserAccounts PRIMARY KEY CLUSTERED (UserId),
    CONSTRAINT UQ_UserAccounts_Username UNIQUE (Username),
    CONSTRAINT FK_UserAccounts_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId),
    CONSTRAINT FK_UserAccounts_Members FOREIGN KEY (MemberId) REFERENCES dbo.Members(MemberId)
);
GO

-- 3.4. Bảng Thể Loại Sách (Categories)
CREATE TABLE dbo.Categories (
    CategoryId INT IDENTITY(1,1) NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(250) NULL,
    CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (CategoryId),
    CONSTRAINT UQ_Categories_CategoryName UNIQUE (CategoryName)
);
GO

-- 3.5. Bảng Tác Giả (Authors)
CREATE TABLE dbo.Authors (
    AuthorId INT IDENTITY(1,1) NOT NULL,
    AuthorName NVARCHAR(150) NOT NULL,
    Notes NVARCHAR(250) NULL,
    CONSTRAINT PK_Authors PRIMARY KEY CLUSTERED (AuthorId)
);
GO

-- 3.6. Bảng Nhà Xuất Bản (Publishers)
CREATE TABLE dbo.Publishers (
    PublisherId INT IDENTITY(1,1) NOT NULL,
    PublisherName NVARCHAR(150) NOT NULL,
    Address NVARCHAR(250) NULL,
    PhoneNumber NVARCHAR(20) NULL,
    CONSTRAINT PK_Publishers PRIMARY KEY CLUSTERED (PublisherId),
    CONSTRAINT UQ_Publishers_PublisherName UNIQUE (PublisherName)
);
GO

-- 3.7. Bảng Đầu Sách (Books)
CREATE TABLE dbo.Books (
    BookId INT IDENTITY(1,1) NOT NULL,
    ISBN NVARCHAR(30) NULL,
    Title NVARCHAR(250) NOT NULL,
    CategoryId INT NOT NULL,
    AuthorId INT NOT NULL,
    PublisherId INT NOT NULL,
    PublishYear INT NULL,
    Price DECIMAL(18,2) NOT NULL CONSTRAINT DF_Books_Price DEFAULT (0),
    ShelfLocation NVARCHAR(100) NULL,
    CoverImagePath NVARCHAR(500) NULL,
    Summary NVARCHAR(MAX) NULL,
    TotalQuantity INT NOT NULL CONSTRAINT DF_Books_TotalQuantity DEFAULT (0),
    AvailableQuantity INT NOT NULL CONSTRAINT DF_Books_AvailableQuantity DEFAULT (0),
    CONSTRAINT PK_Books PRIMARY KEY CLUSTERED (BookId),
    CONSTRAINT FK_Books_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId),
    CONSTRAINT FK_Books_Authors FOREIGN KEY (AuthorId) REFERENCES dbo.Authors(AuthorId),
    CONSTRAINT FK_Books_Publishers FOREIGN KEY (PublisherId) REFERENCES dbo.Publishers(PublisherId)
);
GO

-- 3.8. Bảng Bản Sao Cuốn Sách Cá Biệt (BookCopies)
CREATE TABLE dbo.BookCopies (
    CopyId INT IDENTITY(1,1) NOT NULL,
    BookId INT NOT NULL,
    Barcode NVARCHAR(50) NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_BookCopies_Status DEFAULT (N'AVAILABLE'),
    ConditionNote NVARCHAR(250) NULL,
    AddedDate DATETIME NOT NULL CONSTRAINT DF_BookCopies_AddedDate DEFAULT (GETDATE()),
    CONSTRAINT PK_BookCopies PRIMARY KEY CLUSTERED (CopyId),
    CONSTRAINT UQ_BookCopies_Barcode UNIQUE (Barcode),
    CONSTRAINT FK_BookCopies_Books FOREIGN KEY (BookId) REFERENCES dbo.Books(BookId) ON DELETE CASCADE
);
GO

-- 3.9. Bảng Lịch Sử Giao Dịch Tiền Cọc Thế Chân (DepositTransactions)
CREATE TABLE dbo.DepositTransactions (
    TransactionId INT IDENTITY(1,1) NOT NULL,
    MemberId INT NOT NULL,
    HandledByUserId INT NOT NULL,
    TransactionType NVARCHAR(30) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    BalanceAfter DECIMAL(18,2) NOT NULL,
    ReceiptCode NVARCHAR(30) NULL,
    TransactionDate DATETIME NOT NULL CONSTRAINT DF_DepositTransactions_TransactionDate DEFAULT (GETDATE()),
    Notes NVARCHAR(250) NULL,
    CONSTRAINT PK_DepositTransactions PRIMARY KEY CLUSTERED (TransactionId),
    CONSTRAINT FK_DepositTransactions_Members FOREIGN KEY (MemberId) REFERENCES dbo.Members(MemberId),
    CONSTRAINT FK_DepositTransactions_UserAccounts FOREIGN KEY (HandledByUserId) REFERENCES dbo.UserAccounts(UserId)
);
GO

-- 3.10. Bảng Phiếu Mượn Sách (BorrowSlips)
CREATE TABLE dbo.BorrowSlips (
    BorrowSlipId INT IDENTITY(1,1) NOT NULL,
    SlipCode NVARCHAR(30) NOT NULL,
    MemberId INT NOT NULL,
    CreatedByUserId INT NOT NULL,
    BorrowDate DATETIME NOT NULL CONSTRAINT DF_BorrowSlips_BorrowDate DEFAULT (GETDATE()),
    DueDate DATETIME NOT NULL,
    RenewalCount INT NOT NULL CONSTRAINT DF_BorrowSlips_RenewalCount DEFAULT (0),
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_BorrowSlips_Status DEFAULT (N'BORROWING'),
    Notes NVARCHAR(250) NULL,
    CONSTRAINT PK_BorrowSlips PRIMARY KEY CLUSTERED (BorrowSlipId),
    CONSTRAINT UQ_BorrowSlips_SlipCode UNIQUE (SlipCode),
    CONSTRAINT FK_BorrowSlips_Members FOREIGN KEY (MemberId) REFERENCES dbo.Members(MemberId),
    CONSTRAINT FK_BorrowSlips_UserAccounts FOREIGN KEY (CreatedByUserId) REFERENCES dbo.UserAccounts(UserId)
);
GO

-- 3.11. Bảng Chi Tiết Phiếu Mượn Sách (BorrowSlipDetails)
CREATE TABLE dbo.BorrowSlipDetails (
    DetailId INT IDENTITY(1,1) NOT NULL,
    BorrowSlipId INT NOT NULL,
    CopyId INT NOT NULL,
    BorrowConditionNote NVARCHAR(250) NULL,
    CONSTRAINT PK_BorrowSlipDetails PRIMARY KEY CLUSTERED (DetailId),
    CONSTRAINT FK_BorrowSlipDetails_BorrowSlips FOREIGN KEY (BorrowSlipId) REFERENCES dbo.BorrowSlips(BorrowSlipId) ON DELETE CASCADE,
    CONSTRAINT FK_BorrowSlipDetails_BookCopies FOREIGN KEY (CopyId) REFERENCES dbo.BookCopies(CopyId)
);
GO

-- 3.12. Bảng Chi Tiết Trả Sách (ReturnSlipDetails)
CREATE TABLE dbo.ReturnSlipDetails (
    ReturnDetailId INT IDENTITY(1,1) NOT NULL,
    BorrowSlipDetailId INT NOT NULL,
    ReceivedByUserId INT NOT NULL,
    ActualReturnDate DATETIME NOT NULL CONSTRAINT DF_ReturnSlipDetails_ActualReturnDate DEFAULT (GETDATE()),
    OverdueDays INT NOT NULL CONSTRAINT DF_ReturnSlipDetails_OverdueDays DEFAULT (0),
    FineAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReturnSlipDetails_FineAmount DEFAULT (0),
    ReturnConditionNote NVARCHAR(250) NULL,
    BookCopyStatusAfterReturn NVARCHAR(30) NOT NULL CONSTRAINT DF_ReturnSlipDetails_BookCopyStatus DEFAULT (N'AVAILABLE'),
    CONSTRAINT PK_ReturnSlipDetails PRIMARY KEY CLUSTERED (ReturnDetailId),
    CONSTRAINT FK_ReturnSlipDetails_BorrowSlipDetails FOREIGN KEY (BorrowSlipDetailId) REFERENCES dbo.BorrowSlipDetails(DetailId),
    CONSTRAINT FK_ReturnSlipDetails_UserAccounts FOREIGN KEY (ReceivedByUserId) REFERENCES dbo.UserAccounts(UserId)
);
GO

-- 3.13. Bảng Biên Lai Thu Tiền Phạt & Đền Bù (FineReceipts)
CREATE TABLE dbo.FineReceipts (
    ReceiptId INT IDENTITY(1,1) NOT NULL,
    ReceiptCode NVARCHAR(30) NOT NULL,
    MemberId INT NOT NULL,
    BorrowSlipId INT NULL,
    CollectedByUserId INT NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(30) NOT NULL CONSTRAINT DF_FineReceipts_PaymentMethod DEFAULT (N'DEPOSIT_DEDUCTION'),
    Reason NVARCHAR(250) NOT NULL,
    PaymentDate DATETIME NOT NULL CONSTRAINT DF_FineReceipts_PaymentDate DEFAULT (GETDATE()),
    Notes NVARCHAR(250) NULL,
    CONSTRAINT PK_FineReceipts PRIMARY KEY CLUSTERED (ReceiptId),
    CONSTRAINT UQ_FineReceipts_ReceiptCode UNIQUE (ReceiptCode),
    CONSTRAINT FK_FineReceipts_Members FOREIGN KEY (MemberId) REFERENCES dbo.Members(MemberId),
    CONSTRAINT FK_FineReceipts_BorrowSlips FOREIGN KEY (BorrowSlipId) REFERENCES dbo.BorrowSlips(BorrowSlipId),
    CONSTRAINT FK_FineReceipts_UserAccounts FOREIGN KEY (CollectedByUserId) REFERENCES dbo.UserAccounts(UserId)
);
GO

-- 3.14. Bảng Tham Số Cấu Hình Hệ Thống Động (SystemSettings)
CREATE TABLE dbo.SystemSettings (
    SettingKey NVARCHAR(50) NOT NULL,
    SettingValue NVARCHAR(250) NOT NULL,
    Description NVARCHAR(250) NULL,
    LastModifiedAt DATETIME NOT NULL CONSTRAINT DF_SystemSettings_LastModifiedAt DEFAULT (GETDATE()),
    CONSTRAINT PK_SystemSettings PRIMARY KEY CLUSTERED (SettingKey)
);
GO

-- 3.15. Bảng Yêu Cầu Tự Phục Vụ Độc Giả (ReaderRequests)
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
GO

-- 3.16. Bảng Biên Lai Thu Phí Dịch Vụ Thư Viện (ServiceFeeReceipts)
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
GO

-- ============================================================================
-- 4. TẠO CÁC CHỈ MỤC TỐI ƯU HÓA HIỆU NĂNG TRUY VẤN (INDEXES)
-- ============================================================================
CREATE NONCLUSTERED INDEX IX_BookCopies_Barcode ON dbo.BookCopies (Barcode);
CREATE NONCLUSTERED INDEX IX_BookCopies_Status ON dbo.BookCopies (Status);
CREATE NONCLUSTERED INDEX IX_Members_MemberCardCode ON dbo.Members (MemberCardCode);
CREATE NONCLUSTERED INDEX IX_Members_PhoneNumber ON dbo.Members (PhoneNumber);
CREATE NONCLUSTERED INDEX IX_Members_IdentityCardNumber ON dbo.Members (IdentityCardNumber);
CREATE NONCLUSTERED INDEX IX_BorrowSlips_Status_DueDate ON dbo.BorrowSlips (Status, DueDate);
CREATE NONCLUSTERED INDEX IX_DepositTransactions_MemberId ON dbo.DepositTransactions (MemberId, TransactionDate);
CREATE NONCLUSTERED INDEX IX_ReaderRequests_MemberId ON dbo.ReaderRequests (MemberId, Status);
CREATE NONCLUSTERED INDEX IX_ServiceFeeReceipts_MemberId ON dbo.ServiceFeeReceipts (MemberId, PaymentDate);
GO

-- ============================================================================
-- 5. NẠP DỮ LIỆU NỀN TẢNG BẮT BUỘC (BASELINE SEED DATA)
-- ============================================================================

-- 5.1. Bảng Vai Trò (Roles) - 3 vai trò chuẩn hệ thống
INSERT INTO dbo.Roles (RoleName, Description) VALUES
(N'ADMIN', N'Quản trị viên toàn quyền hệ thống'),
(N'LIBRARIAN', N'Thủ thư / Cán bộ nghiệp vụ vận hành thư viện'),
(N'READER', N'Độc giả sử dụng cổng thông tin tự phục vụ');
GO

-- 5.2. Bảng Tham Số Cấu Hình Hệ Thống (SystemSettings) - 18 tham số chuẩn
INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description) VALUES
(N'CARD_ISSUANCE_FEE', N'50000', N'Phí dịch vụ phát hành thẻ độc giả mới (Không hoàn lại - VNĐ)'),
(N'MEMBER_CARD_DEFAULT_DEPOSIT', N'200000', N'Mức tiền cọc thế chân tối thiểu để mượn sách (VNĐ)'),
(N'MAX_BORROW_DAYS', N'14', N'Thời hạn tối đa cho một lượt mượn sách (Ngày)'),
(N'MAX_BOOKS_PER_MEMBER', N'5', N'Số lượng sách tối đa một độc giả được phép mượn đồng thời (Cuốn)'),
(N'FINE_PER_OVERDUE_DAY', N'5000', N'Số tiền phạt cho mỗi ngày trễ hạn (VNĐ / ngày / cuốn)'),
(N'LOST_BOOK_FINE_RATE', N'2.0', N'Hệ số bồi thường khi làm mất sách (200% giá bìa sách)'),
(N'DAMAGED_BOOK_FINE_RATE', N'0.5', N'Hệ số bồi thường khi làm hư hại sách (50% giá bìa sách)'),
(N'MEMBER_CARD_VALIDITY_DAYS', N'365', N'Thời hạn hiệu lực của thẻ độc giả kể từ ngày cấp (Ngày)'),
(N'MAX_RENEWAL_COUNT', N'1', N'Số lần gia hạn tối đa cho một lượt mượn sách (Lần)'),
(N'RENEWAL_DAYS', N'7', N'Số ngày gia hạn thêm mỗi lần gia hạn (Ngày)'),
(N'MEMBER_CARD_ANNUAL_FEE', N'50000', N'Phí thường niên khi gia hạn thẻ độc giả (VNĐ/năm)'),
(N'SMTP_HOST', N'smtp.gmail.com', N'Máy chủ SMTP gửi email thông báo hệ thống'),
(N'SMTP_PORT', N'587', N'Cổng kết nối máy chủ SMTP (587 TLS/STARTTLS)'),
(N'SMTP_ENABLE_SSL', N'true', N'Bật mã hóa SSL/TLS an toàn khi gửi email (true/false)'),
(N'SMTP_USERNAME', N'dung2k5k58lx@gmail.com', N'Tài khoản email hệ thống gửi thư cho độc giả'),
(N'SMTP_PASSWORD', N'citz voms bvei wuur', N'Mật khẩu ứng dụng của email hệ thống'),
(N'SMTP_FROM_NAME', N'LibOps - Thư Viện Hiện Đại', N'Tên hiển thị người gửi email'),
(N'SYSTEM_EMAIL_NOTIFICATION_ENABLED', N'true', N'Công tắc bật/tắt tính năng gửi email tự động toàn hệ thống');
GO

-- 5.3. Bảng Tài Khoản Người Dùng (UserAccounts) - Khởi tạo 2 tài khoản quản trị & nghiệp vụ hệ thống
-- 1) Admin     | Mật khẩu: Admin@libops     | Salt: LIB_OPS_SALT_2026 | Hash SHA-256: A303B1934621E50C4D142F63871D1A402852394605022F9C128ECA1E0D58C24D
-- 2) Librarian | Mật khẩu: Librarian@libops | Salt: LIB_OPS_SALT_2026 | Hash SHA-256: EA0A9503A235699897D0E19C74D5427934651F78E6BED78B4327E844BE6DEBD4
DECLARE @AdminRoleId INT = (SELECT TOP 1 RoleId FROM dbo.Roles WHERE RoleName = 'ADMIN');
DECLARE @LibrarianRoleId INT = (SELECT TOP 1 RoleId FROM dbo.Roles WHERE RoleName = 'LIBRARIAN');

IF @AdminRoleId IS NOT NULL
BEGIN
    INSERT INTO dbo.UserAccounts (Username, PasswordHash, PasswordSalt, FullName, Email, PhoneNumber, RoleId, MemberId, IsActive, CreatedAt)
    VALUES (N'Admin', N'A303B1934621E50C4D142F63871D1A402852394605022F9C128ECA1E0D58C24D', N'LIB_OPS_SALT_2026', N'Quản Trị Viên Hệ Thống', N'admin@libops.vn', N'0912000111', @AdminRoleId, NULL, 1, GETDATE());
END

IF @LibrarianRoleId IS NOT NULL
BEGIN
    INSERT INTO dbo.UserAccounts (Username, PasswordHash, PasswordSalt, FullName, Email, PhoneNumber, RoleId, MemberId, IsActive, CreatedAt)
    VALUES (N'Librarian', N'EA0A9503A235699897D0E19C74D5427934651F78E6BED78B4327E844BE6DEBD4', N'LIB_OPS_SALT_2026', N'Thủ Thư Thư Viện', N'librarian@libops.vn', N'0988111222', @LibrarianRoleId, NULL, 1, GETDATE());
END
GO

-- ============================================================================
-- 6. NẠP DỮ LIỆU MẪU THỰC TẾ (SEED DATA)
-- ============================================================================

-- 6.1. Chèn 10 Thể Loại (Categories: ID 1 -> 10)
INSERT INTO dbo.Categories (CategoryName, Description) VALUES
(N'Công Nghệ Thông Tin', N'Sách lập trình, thuật toán, AI, kiến trúc phần mềm, an toàn thông tin'),
(N'Kinh Tế & Đầu Tư', N'Quản trị kinh doanh, tài chính, chứng khoán, khởi nghiệp'),
(N'Tâm Lý & Kỹ Năng Sống', N'Phát triển bản thân, kỹ năng giao tiếp, tâm lý học ứng dụng'),
(N'Văn Học Việt Nam', N'Tiểu thuyết, truyện ngắn, tác phẩm văn học kinh điển Việt Nam'),
(N'Văn Học Nước Ngoài', N'Tiểu thuyết, trinh thám, văn học kinh điển thế giới'),
(N'Khoa Học & Thiên Văn', N'Vũ trụ học, vật lý, sinh học, khoa học thường thức'),
(N'Lịch Sử & Triết Học', N'Lịch sử văn minh nhân loại, lịch sử Việt Nam, tư tưởng triết học'),
(N'Ngoại Ngữ & Giáo Dục', N'Sách học tiếng Anh, phương pháp sư phạm, giáo dục tư duy'),
(N'Y Học & Sức Khỏe', N'Dinh dưỡng, y học thường thức, chăm sóc sức khỏe thể chất & tinh thần'),
(N'Nghệ Thuật & Thiết Kế', N'Thiết kế đồ họa, UI/UX, nhiếp ảnh, kiến trúc và mỹ thuật');
GO

-- 6.2. Chèn 25 Tác Giả (Authors: ID 1 -> 25)
INSERT INTO dbo.Authors (AuthorName, Notes) VALUES
(N'Robert C. Martin', N'Chuyên gia phần mềm quốc tế (Uncle Bob)'),
(N'Martin Fowler', N'Tác giả nổi tiếng về Kiến trúc phần mềm và Refactoring'),
(N'Andrew Hunt & David Thomas', N'Tác giả của cuốn The Pragmatic Programmer kinh điển'),
(N'Morgan Housel', N'Chuyên gia tài chính, cựu phóng viên The Wall Street Journal'),
(N'Benjamin Graham', N'Cha đẻ của trường phái đầu tư giá trị, thầy của Warren Buffett'),
(N'Peter Thiel', N'Doanh nhân khởi nghiệp huyền thoại thung lũng Silicon'),
(N'Dale Carnegie', N'Bậc thầy nghệ thuật giao tiếp và đắc nhân tâm'),
(N'James Clear', N'Chuyên gia nghiên cứu về thói quen và hiệu suất cá nhân'),
(N'Daniel Kahneman', N'Nhà tâm lý học đoạt giải Nobel Kinh tế 2002'),
(N'Nam Cao', N'Cây bút hiện thực xuất sắc của nền văn học Việt Nam'),
(N'Tô Hoài', N'Nhà văn lớn với kho tàng tác phẩm thiếu nhi phong phú'),
(N'Vũ Trọng Phụng', N'Ông vua phóng sự đất Bắc thời kỳ tiền chiến'),
(N'Nguyễn Nhật Ánh', N'Nhà văn được yêu thích nhất của nhiều thế hệ thanh thiếu niên'),
(N'Paulo Coelho', N'Tiểu thuyết gia người Brazil, tác giả Nhà Giả Kim'),
(N'George Orwell', N'Nhà văn, nhà báo người Anh với các tiểu thuyết giả tưởng chính trị'),
(N'Haruki Murakami', N'Tiểu thuyết gia đương đại nổi tiếng hàng đầu Nhật Bản'),
(N'Arthur Conan Doyle', N'Cha đẻ của thám tử lừng danh Sherlock Holmes'),
(N'J.K. Rowling', N'Tác giả bộ truyện phù thủy nổi tiếng toàn cầu Harry Potter'),
(N'Stephen Hawking', N'Nhà vật lý lý thuyết, vũ trụ học vĩ đại'),
(N'Carl Sagan', N'Nhà thiên văn học truyền cảm hứng nhất thế kỷ 20'),
(N'Yuval Noah Harari', N'Giáo sư lịch sử, tác giả bộ ba Sapiens, Homo Deus, 21 Bài học'),
(N'Will Durant', N'Nhà sử học, triết gia người Mỹ đoạt giải Pulitzer'),
(N'AJ Hoge', N'Người sáng lập hệ thống học tiếng Anh Effortless English'),
(N'Matthew Walker', N'Giáo sư khoa học thần kinh chuyên sâu về giấc ngủ'),
(N'Don Norman', N'Huyền thoại trong lĩnh vực Thiết kế lấy người dùng làm trung tâm (UI/UX)');
GO

-- 6.3. Chèn 10 Nhà Xuất Bản (Publishers: ID 1 -> 10)
INSERT INTO dbo.Publishers (PublisherName, Address, PhoneNumber) VALUES
(N'Prentice Hall / Pearson', N'Boston, Massachusetts, USA', N'+1-800-848-9500'),
(N'O''Reilly Media', N'Sebastopol, California, USA', N'+1-707-827-7000'),
(N'NXB Trẻ', N'161B Lý Chính Thắng, Quận 3, TP.HCM', N'028-39316289'),
(N'NXB Kim Đồng', N'55 Quang Trung, Hai Bà Trưng, Hà Nội', N'1900-571595'),
(N'NXB Hội Nhà Văn', N'65 Nguyễn Du, Hai Bà Trưng, Hà Nội', N'024-38222135'),
(N'NXB Tổng Hợp TP.HCM', N'62 Nguyễn Thị Minh Khai, Quận 1, TP.HCM', N'028-38225340'),
(N'NXB Thế Giới', N'46 Trần Hưng Đạo, Hoàn Kiếm, Hà Nội', N'024-38253841'),
(N'NXB Dân Trí', N'Số 9, Ngõ 26, Hoàng Cầu, Đống Đa, Hà Nội', N'024-35121804'),
(N'NXB Tri Thức', N'53 Nguyễn Du, Hai Bà Trưng, Hà Nội', N'024-39436154'),
(N'NXB Y Học', N'352 Đội Cấn, Ba Đình, Hà Nội', N'024-37625934');
GO

-- 6.4. Chèn 50 Đầu Sách (Books: ID 1 -> 50)
INSERT INTO dbo.Books 
(ISBN, Title, CategoryId, AuthorId, PublisherId, PublishYear, Price, ShelfLocation, CoverImagePath, Summary, TotalQuantity, AvailableQuantity) 
VALUES
-- [1-5]: Công nghệ thông tin
(N'978-0-13-235088-4', N'Clean Code: A Handbook of Agile Software Craftsmanship', 1, 1, 1, 2008, 450000, N'Kệ IT-01', N'Assets/BookCovers/Clean_Code.jpg', N'Cẩm nang kinh điển hướng dẫn viết mã nguồn sạch, dễ đọc và dễ bảo trì.', 4, 4),
(N'978-0-13-449416-6', N'Clean Architecture: A Craftsman''s Guide', 1, 1, 1, 2017, 520000, N'Kệ IT-01', N'Assets/BookCovers/Clean_Architecture.jpg', N'Nguyên tắc thiết kế kiến trúc phần mềm độc lập framework và dễ mở rộng.', 3, 3),
(N'978-0-13-475759-9', N'Refactoring: Improving the Design of Existing Code', 1, 2, 1, 2018, 480000, N'Kệ IT-02', N'Assets/BookCovers/Refactoring.jpg', N'Kỹ thuật tái cấu trúc mã nguồn giúp nâng cao chất lượng phần mềm.', 3, 3),
(N'978-0-20-161622-4', N'The Pragmatic Programmer', 1, 3, 1, 2019, 490000, N'Kệ IT-02', N'Assets/BookCovers/The_Pragmatic_Programmer.jpg', N'Hành trình từ thợ code trở thành một kỹ sư phần mềm thực thụ.', 4, 4),
(N'978-1-49-195035-7', N'Designing Data-Intensive Applications', 1, 2, 2, 2017, 650000, N'Kệ IT-03', N'Assets/BookCovers/Designing_Data-Intensive_Applications.jpg', N'Bách khoa toàn thư về thiết kế hệ thống dữ liệu lớn và phân tán.', 3, 3),

-- [6-10]: Kinh tế & Đầu tư
(N'978-604-77-8765-1', N'Tâm Lý Học Về Tiền (The Psychology of Money)', 2, 4, 8, 2021, 189000, N'Kệ KT-01', N'Assets/BookCovers/Tâm_Lý_Học_Về_Tiền.jpg', N'Những góc nhìn tâm lý thú vị về cách con người quản lý và đầu tư tiền bạc.', 5, 5),
(N'978-604-58-9012-3', N'Nhà Đầu Tư Thông Minh (The Intelligent Investor)', 2, 5, 3, 2020, 240000, N'Kệ KT-01', N'Assets/BookCovers/Nhà_Đầu_Tư_Thông_Minh.jpg', N'Cuốn sách gối đầu giường của mọi nhà đầu tư tài chính giá trị.', 4, 4),
(N'978-0-80-413929-7', N'Zero to One: Ghi Chú Về Khởi Nghiệp', 2, 6, 3, 2019, 145000, N'Kệ KT-02', N'Assets/BookCovers/Zero_to_One.jpg', N'Tư duy đột phá để xây dựng doanh nghiệp tạo ra giá trị hoàn toàn mới.', 5, 5),
(N'978-604-30-2211-1', N'Chiến Lược Đại Dương Xanh', 2, 6, 8, 2021, 175000, N'Kệ KT-02', N'Assets/BookCovers/Chiến_Lược_Đại_Dương_Xanh.jpg', N'Cách tạo ra khoảng trống thị trường không có cạnh tranh để bứt phá.', 3, 3),
(N'978-604-56-1122-3', N'Dạy Con Làm Giàu (Tập 1)', 2, 4, 3, 2018, 95000, N'Kệ KT-03', N'Assets/BookCovers/Dạy_Con_Làm_Giàu_(Tập_1).jpg', N'Bài học vỡ lòng về tư duy tài chính giữa người giàu và người nghèo.', 6, 6),

-- [11-15]: Tâm lý & Kỹ năng sống
(N'978-604-58-7693-9', N'Đắc Nhân Tâm (How to Win Friends)', 3, 7, 6, 2019, 86000, N'Kệ TL-01', N'Assets/BookCovers/Đắc_Nhân_Tâm.jpg', N'Nghệ thuật ứng xử thu phục lòng người kinh điển nhất mọi thời đại.', 6, 6),
(N'978-604-56-7890-3', N'Atomic Habits: Thay Đổi Tí Hon, Hiệu Quả Bất Ngờ', 3, 8, 7, 2019, 169000, N'Kệ TL-01', N'Assets/BookCovers/Atomic_Habits.jpg', N'Phương pháp xây dựng thói quen tốt và từ bỏ thói quen xấu khoa học.', 5, 5),
(N'978-604-77-3344-5', N'Tư Duy Nhanh Và Chậm (Thinking, Fast and Slow)', 3, 9, 7, 2020, 230000, N'Kệ TL-02', N'Assets/BookCovers/Tư_Duy_Nhanh_Và_Chậm.jpg', N'Khám phá hai hệ thống tư duy chi phối mọi quyết định của con người.', 4, 4),
(N'978-604-58-1234-9', N'Quẳng Gánh Lo Đi Và Vui Sống', 3, 7, 6, 2018, 78000, N'Kệ TL-02', N'Assets/BookCovers/Quẳng_Gánh_Lo_Đi_Và_Vui_Sống.jpg', N'Bí quyết vượt qua nỗi sợ hãi, áp lực để sống an nhiên và hạnh phúc.', 4, 4),
(N'978-604-30-5566-7', N'Hiệu Ứng Chim Mồi', 3, 9, 8, 2022, 115000, N'Kệ TL-03', N'Assets/BookCovers/Hiệu_Ứng_Chim_Mồi.jpg', N'Giải mã những đòn tâm lý thao túng hành vi người tiêu dùng.', 3, 3),

-- [16-20]: Văn học Việt Nam
(N'978-604-2-12970-0', N'Dế Mèn Phiêu Lưu Ký', 4, 11, 4, 2021, 55000, N'Kệ VN-01', N'Assets/BookCovers/Dế_Mèn_Phiêu_Lưu_Ký.jpg', N'Chuyến phiêu lưu học hỏi bài học đường đời ý nghĩa của Dế Mèn.', 8, 8),
(N'978-604-1-08999-1', N'Mắt Biếc', 4, 13, 3, 2020, 110000, N'Kệ VN-01', N'Assets/BookCovers/Mắt_Biếc.jpg', N'Chuyện tình đơn phương da diết của Ngạn dành cho cô bạn Hà Lan.', 5, 5),
(N'978-604-2-00112-2', N'Số Đỏ', 4, 12, 4, 2019, 65000, N'Kệ VN-02', N'Assets/BookCovers/Số_Đỏ.jpg', N'Kiệt tác trào phúng châm biếm xã hội tư sản thành thị Việt Nam.', 4, 4),
(N'978-604-56-4433-2', N'Chí Phèo & Tuyển Tập Truyện Ngắn Nam Cao', 4, 10, 5, 2018, 75000, N'Kệ VN-02', N'Assets/BookCovers/Chí_Phèo_&_Tuyển_Tập.jpg', N'Bi kịch tha hóa và khát vọng làm người lương thiện của người nông dân.', 5, 5),
(N'978-604-1-15544-3', N'Tôi Thấy Hoa Vàng Trên Cỏ Xanh', 4, 13, 3, 2022, 125000, N'Kệ VN-03', N'Assets/BookCovers/Tôi_Thấy_Hoa_Vàng_Trên_Cỏ_Xanh.jpg', N'Bức tranh tuổi thơ nông thôn trong trẻo, bình yên và đầy xúc cảm.', 6, 6),

-- [21-25]: Văn học nước ngoài
(N'978-604-383-186-2', N'Nhà Giả Kim (The Alchemist)', 5, 14, 5, 2020, 79000, N'Kệ QT-01', N'Assets/BookCovers/Nhà_Giả_Kim.jpg', N'Hành trình tìm kiếm kho báu và sứ mệnh cuộc đời của cậu bé Santiago.', 6, 6),
(N'978-604-58-2233-0', N'1984', 5, 15, 7, 2019, 95000, N'Kệ QT-01', N'Assets/BookCovers/1984.jpg', N'Tiểu thuyết viễn tưởng kinh điển cảnh báo về chế độ giám sát toàn diện.', 4, 4),
(N'978-604-56-9988-1', N'Rừng Na Uy (Norwegian Wood)', 5, 16, 5, 2021, 135000, N'Kệ QT-02', N'Assets/BookCovers/Rừng_Na_Uy.jpg', N'Nỗi cô đơn và sự trưởng thành của những người trẻ tuổi tại Tokyo.', 4, 4),
(N'978-604-2-05566-7', N'Sherlock Holmes Toàn Tập (Tập 1)', 5, 17, 4, 2020, 160000, N'Kệ QT-02', N'Assets/BookCovers/Sherlock_Holmes_Toàn_Tập_(Tập_1).jpg', N'Các vụ án phá án ly kỳ của thám tử bậc thầy Sherlock Holmes.', 5, 5),
(N'978-604-1-03456-7', N'Harry Potter và Hòn Đá Phù Thủy', 5, 18, 3, 2022, 130000, N'Kệ QT-03', N'Assets/BookCovers/Harry_Potter_và_Hòn_Đá_Phù_Thủy.jpg', N'Bước chân đầu tiên của cậu bé Harry vào thế giới phép thuật Hogwarts.', 7, 7),

-- [26-30]: Khoa học & Thiên văn
(N'978-604-77-5432-8', N'Lược Sử Thời Gian (A Brief History of Time)', 6, 19, 9, 2020, 155000, N'Kệ KH-01', N'Assets/BookCovers/Lược_Sử_Thời_Gian.jpg', N'Khám phá lỗ đen, Big Bang và nguồn gốc vũ trụ bao la.', 4, 4),
(N'978-604-77-1122-9', N'Vũ Trụ (Cosmos)', 6, 20, 9, 2021, 195000, N'Kệ KH-01', N'Assets/BookCovers/Vũ_Trụ_(Cosmos).jpg', N'Chuyến du hành ngoạn mục qua 15 tỷ năm tiến hóa của vũ trụ.', 3, 3),
(N'978-604-77-8899-0', N'Vũ Trụ Trong Vỏ Hạt Dẻ', 6, 19, 9, 2019, 140000, N'Kệ KH-02', N'Assets/BookCovers/Vũ_Trụ_Trong_Vỏ_Hạt_Dẻ.jpg', N'Những khám phá tiên phong nhất của vật lý lý thuyết hiện đại.', 3, 3),
(N'978-604-56-3344-1', N'Chết Bởi Lỗ Đen (Death by Black Hole)', 6, 20, 7, 2022, 165000, N'Kệ KH-02', N'Assets/BookCovers/Chết_Bởi_Lỗ_Đen.jpg', N'Giải thích các hiện tượng bí ẩn không gian qua góc nhìn hài hước.', 3, 3),
(N'978-604-77-6655-4', N'Nguồn Gốc Các Loài (On the Origin of Species)', 6, 20, 9, 2018, 210000, N'Kệ KH-03', N'Assets/BookCovers/Nguồn_Gốc_Các_Loài.jpg', N'Công trình đặt nền móng cho học thuyết tiến hóa sinh học.', 3, 3),

-- [31-35]: Lịch sử & Triết học
(N'978-604-2-23062-8', N'Sapiens: Lược Sử Loài Người', 7, 21, 9, 2018, 250000, N'Kệ LS-01', N'Assets/BookCovers/Sapiens_Lược_Sử_Loài_Người.jpg', N'Hành trình tiến hóa và thống trị hành tinh của loài người.', 5, 5),
(N'978-604-2-23063-5', N'Homo Deus: Lược Sử Tương Lai', 7, 21, 9, 2019, 260000, N'Kệ LS-01', N'Assets/BookCovers/Homo_Deus_Lược_Sử_Tương_Lai.jpg', N'Dự đoán về số phận và tương lai của loài người trong kỷ nguyên AI.', 4, 4),
(N'978-604-2-23064-2', N'21 Bài Học Cho Thế Kỷ 21', 7, 21, 9, 2020, 220000, N'Kệ LS-02', N'Assets/BookCovers/21_Bài_Học_Cho_Thế_Kỷ_21.jpg', N'Góc nhìn sâu sắc về những thách thức cấp bách của thời đại ngày nay.', 4, 4),
(N'978-604-58-7711-0', N'Lịch Sử Văn Minh Nhân Loại', 7, 22, 6, 2017, 350000, N'Kệ LS-02', N'Assets/BookCovers/Lịch_Sử_Văn_Minh_Nhân_Loại.jpg', N'Bức tranh toàn cảnh về sự hình thành các nền văn minh lớn.', 3, 3),
(N'978-604-56-8822-1', N'Đại Việt Sử Ký Toàn Thư', 7, 22, 5, 2021, 420000, N'Kệ LS-03', N'Assets/BookCovers/Đại_Việt_Sử_Ký_Toàn_Thư.jpg', N'Bộ chính sử vô giá ghi chép lịch sử nước Việt từ thời Hồng Bàng.', 3, 3),

-- [36-40]: Ngoại ngữ & Giáo dục
(N'978-604-58-1199-1', N'Luyện Siêu Trí Nhớ Từ Vựng Tiếng Anh', 8, 23, 8, 2020, 180000, N'Kệ NN-01', N'Assets/BookCovers/Luyện_Siêu_Trí_Nhớ_Từ_Vựng_Tiếng_Anh.jpg', N'Phương pháp ghi nhớ từ vựng tiếng Anh theo tư duy của người Do Thái.', 6, 6),
(N'978-604-58-2200-2', N'Effortless English: Tự Tin Giao Tiếp Sau 6 Tháng', 8, 23, 8, 2021, 160000, N'Kệ NN-01', N'Assets/BookCovers/Effortless_English.jpg', N'Phương pháp học phản xạ nói tiếng Anh tự nhiên không cần học vẹt ngữ pháp.', 5, 5),
(N'978-604-56-3311-3', N'Ngữ Pháp Tiếng Anh Giải Thích Chi Tiết', 8, 23, 7, 2019, 140000, N'Kệ NN-02', N'Assets/BookCovers/Ngữ_Pháp_Tiếng_Anh_Giải_Thích_Chi_Tiết.jpg', N'Hệ thống toàn bộ ngữ pháp chuẩn từ cơ bản đến nâng cao.', 5, 5),
(N'978-604-30-4422-4', N'Hacking Your English Speaking', 8, 23, 8, 2022, 175000, N'Kệ NN-02', N'Assets/BookCovers/Hacking_Your_English_Speaking.jpg', N'Bí kíp phát âm chuẩn bản xứ và luyện tư duy giao tiếp lưu loát.', 4, 4),
(N'978-604-58-5533-5', N'IELTS Simon: Writing Task 2 Masterclass', 8, 23, 7, 2021, 210000, N'Kệ NN-03', N'Assets/BookCovers/IELTS_Simon_Writing_Task_2_Masterclass.jpg', N'Bí quyết đạt điểm cao trong bài thi viết học thuật IELTS.', 4, 4),

-- [41-45]: Y học & Sức khỏe
(N'978-604-56-6611-0', N'Sao Chúng Ta Lại Ngủ (Why We Sleep)', 9, 24, 10, 2020, 190000, N'Kệ YH-01', N'Assets/BookCovers/Sao_Chúng_Ta_Lại_Ngủ.jpg', N'Khám phá sức mạnh kỳ diệu của giấc ngủ và tác hại của việc thức khuya.', 5, 5),
(N'978-604-56-7722-1', N'Nhân Tố Enzyme: Phương Pháp Sống Lành Mạnh', 9, 24, 10, 2019, 115000, N'Kệ YH-01', N'Assets/BookCovers/Nhân_Tố_Enzyme.jpg', N'Chế độ dinh dưỡng và lối sống lành mạnh giúp phòng ngừa bệnh tật.', 5, 5),
(N'978-604-56-8833-2', N'Cơ Thể Tự Chữa Lành: Thải Độc Để Khỏe Mạnh', 9, 24, 10, 2021, 230000, N'Kệ YH-02', N'Assets/BookCovers/Cơ_Thể_Tự_Chữa_Lành.jpg', N'Khai mở khả năng phục hồi tự nhiên của cơ thể qua dinh dưỡng thảo mộc.', 4, 4),
(N'978-604-56-9944-3', N'Ăn Gì Để Không Chết Vì Tim Mạch', 9, 24, 10, 2020, 260000, N'Kệ YH-02', N'Assets/BookCovers/Ăn_Gì_Để_Không_Chết_Vì_Tim_Mạch.jpg', N'Các bằng chứng khoa học chứng minh lợi ích của chế độ ăn toàn phần.', 3, 3),
(N'978-604-56-0055-4', N'Sơ Cứu Ban Đầu Và Xử Trí Khẩn Cấp', 9, 24, 10, 2022, 135000, N'Kệ YH-03', N'Assets/BookCovers/Sơ_Cứu_Ban_Đầu_Và_Xử_Trí_Khẩn_Cấp.jpg', N'Kỹ năng sơ cứu thiết yếu bảo vệ bản thân và gia đình trong tai nạn.', 5, 5),

-- [46-50]: Nghệ thuật & Thiết kế
(N'978-0-46-505065-9', N'Thiết Kế Cho Cuộc Sống (The Design of Everyday Things)', 10, 25, 7, 2019, 220000, N'Kệ TK-01', N'Assets/BookCovers/Thiết_Kế_Cho_Cuộc_Sống.jpg', N'Cuốn sách kinh điển về trải nghiệm người dùng và tư duy thiết kế.', 4, 4),
(N'978-0-32-196551-6', N'Đừng Bắt Tôi Phải Nghĩ (Don''t Make Me Think)', 10, 25, 8, 2020, 185000, N'Kệ TK-01', N'Assets/BookCovers/Đừng_Bắt_Tôi_Phải_Nghĩ.jpg', N'Nguyên lý vàng trong thiết kế giao diện web và ứng dụng thân thiện.', 4, 4),
(N'978-604-30-7788-9', N'Nghệ Thuật Phối Màu Trong Thiết Kế Đồ Họa', 10, 25, 6, 2021, 245000, N'Kệ TK-02', N'Assets/BookCovers/Nghệ_Thuật_Phối_Màu_Trong_Thiết_Kế.jpg', N'Bí quyết sử dụng bánh xe màu sắc và cảm xúc màu trong nhận diện thương hiệu.', 3, 3),
(N'978-604-56-8899-3', N'Nhiếp Ảnh Cơ Bản: Nắm Vững Ánh Sáng Và Bố Cục', 10, 25, 7, 2021, 270000, N'Kệ TK-02', N'Assets/BookCovers/Nhiếp_Ảnh_Cơ_Bản.jpg', N'Kỹ thuật làm chủ máy ảnh và tư duy bố cục thị giác chuyên nghiệp.', 3, 3),
(N'978-604-30-9900-1', N'Tư Duy Thiết Kế (Design Thinking) Cho Người Bắt Đầu', 10, 25, 8, 2022, 195000, N'Kệ TK-03', N'Assets/BookCovers/Tư_Duy_Thiết_Kế_Cho_Người_Mới.jpg', N'Quy trình 5 bước giải quyết vấn đề sáng tạo và tạo ra sản phẩm đột phá.', 4, 4);
GO

-- 6.5. Chèn 100 Bản Sao Sách (BookCopies - 2 cuốn/đầu sách)
INSERT INTO dbo.BookCopies (BookId, Barcode, Status, ConditionNote, AddedDate) VALUES
-- Sách CNTT (1 -> 5)
(1, N'BC-IT-001', N'AVAILABLE', N'Mới 100%', GETDATE()), (1, N'BC-IT-002', N'AVAILABLE', N'Mới 100%', GETDATE()),
(2, N'BC-IT-003', N'AVAILABLE', N'Mới 100%', GETDATE()), (2, N'BC-IT-004', N'AVAILABLE', N'Mới 100%', GETDATE()),
(3, N'BC-IT-005', N'AVAILABLE', N'Mới 100%', GETDATE()), (3, N'BC-IT-006', N'AVAILABLE', N'Mới 100%', GETDATE()),
(4, N'BC-IT-007', N'AVAILABLE', N'Mới 100%', GETDATE()), (4, N'BC-IT-008', N'AVAILABLE', N'Mới 100%', GETDATE()),
(5, N'BC-IT-009', N'AVAILABLE', N'Mới 100%', GETDATE()), (5, N'BC-IT-010', N'AVAILABLE', N'Mới 100%', GETDATE()),

-- Sách Kinh tế (6 -> 10)
(6, N'BC-KT-001', N'AVAILABLE', N'Mới 100%', GETDATE()), (6, N'BC-KT-002', N'AVAILABLE', N'Mới 100%', GETDATE()),
(7, N'BC-KT-003', N'AVAILABLE', N'Mới 100%', GETDATE()), (7, N'BC-KT-004', N'AVAILABLE', N'Mới 100%', GETDATE()),
(8, N'BC-KT-005', N'AVAILABLE', N'Mới 100%', GETDATE()), (8, N'BC-KT-006', N'AVAILABLE', N'Mới 100%', GETDATE()),
(9, N'BC-KT-007', N'AVAILABLE', N'Mới 100%', GETDATE()), (9, N'BC-KT-008', N'AVAILABLE', N'Mới 100%', GETDATE()),
(10, N'BC-KT-009', N'AVAILABLE', N'Mới 100%', GETDATE()), (10, N'BC-KT-010', N'AVAILABLE', N'Mới 100%', GETDATE()),

-- Sách Tâm lý (11 -> 15)
(11, N'BC-TL-001', N'AVAILABLE', N'Mới 100%', GETDATE()), (11, N'BC-TL-002', N'AVAILABLE', N'Mới 100%', GETDATE()),
(12, N'BC-TL-003', N'AVAILABLE', N'Mới 100%', GETDATE()), (12, N'BC-TL-004', N'AVAILABLE', N'Mới 100%', GETDATE()),
(13, N'BC-TL-005', N'AVAILABLE', N'Mới 100%', GETDATE()), (13, N'BC-TL-006', N'AVAILABLE', N'Mới 100%', GETDATE()),
(14, N'BC-TL-007', N'AVAILABLE', N'Mới 100%', GETDATE()), (14, N'BC-TL-008', N'AVAILABLE', N'Mới 100%', GETDATE()),
(15, N'BC-TL-009', N'AVAILABLE', N'Mới 100%', GETDATE()), (15, N'BC-TL-010', N'AVAILABLE', N'Mới 100%', GETDATE()),

-- Sách Văn học VN (16 -> 20)
(16, N'BC-VN-001', N'AVAILABLE', N'Mới 100%', GETDATE()), (16, N'BC-VN-002', N'AVAILABLE', N'Mới 100%', GETDATE()),
(17, N'BC-VN-003', N'AVAILABLE', N'Mới 100%', GETDATE()), (17, N'BC-VN-004', N'AVAILABLE', N'Mới 100%', GETDATE()),
(18, N'BC-VN-005', N'AVAILABLE', N'Mới 100%', GETDATE()), (18, N'BC-VN-006', N'AVAILABLE', N'Mới 100%', GETDATE()),
(19, N'BC-VN-007', N'AVAILABLE', N'Mới 100%', GETDATE()), (19, N'BC-VN-008', N'AVAILABLE', N'Mới 100%', GETDATE()),
(20, N'BC-VN-009', N'AVAILABLE', N'Mới 100%', GETDATE()), (20, N'BC-VN-010', N'AVAILABLE', N'Mới 100%', GETDATE()),

-- Sách Văn học QT (21 -> 25)
(21, N'BC-QT-001', N'AVAILABLE', N'Mới 100%', GETDATE()), (21, N'BC-QT-002', N'AVAILABLE', N'Mới 100%', GETDATE()),
(22, N'BC-QT-003', N'AVAILABLE', N'Mới 100%', GETDATE()), (22, N'BC-QT-004', N'AVAILABLE', N'Mới 100%', GETDATE()),
(23, N'BC-QT-005', N'AVAILABLE', N'Mới 100%', GETDATE()), (23, N'BC-QT-006', N'AVAILABLE', N'Mới 100%', GETDATE()),
(24, N'BC-QT-007', N'AVAILABLE', N'Mới 100%', GETDATE()), (24, N'BC-QT-008', N'AVAILABLE', N'Mới 100%', GETDATE()),
(25, N'BC-QT-009', N'AVAILABLE', N'Mới 100%', GETDATE()), (25, N'BC-QT-010', N'AVAILABLE', N'Mới 100%', GETDATE()),

-- Sách Khoa học (26 -> 30)
(26, N'BC-KH-001', N'AVAILABLE', N'Mới 100%', GETDATE()), (26, N'BC-KH-002', N'AVAILABLE', N'Mới 100%', GETDATE()),
(27, N'BC-KH-003', N'AVAILABLE', N'Mới 100%', GETDATE()), (27, N'BC-KH-004', N'AVAILABLE', N'Mới 100%', GETDATE()),
(28, N'BC-KH-005', N'AVAILABLE', N'Mới 100%', GETDATE()), (28, N'BC-KH-006', N'AVAILABLE', N'Mới 100%', GETDATE()),
(29, N'BC-KH-007', N'AVAILABLE', N'Mới 100%', GETDATE()), (29, N'BC-KH-008', N'AVAILABLE', N'Mới 100%', GETDATE()),
(30, N'BC-KH-009', N'AVAILABLE', N'Mới 100%', GETDATE()), (30, N'BC-KH-010', N'AVAILABLE', N'Mới 100%', GETDATE()),

-- Sách Lịch sử (31 -> 35)
(31, N'BC-LS-001', N'AVAILABLE', N'Mới 100%', GETDATE()), (31, N'BC-LS-002', N'AVAILABLE', N'Mới 100%', GETDATE()),
(32, N'BC-LS-003', N'AVAILABLE', N'Mới 100%', GETDATE()), (32, N'BC-LS-004', N'AVAILABLE', N'Mới 100%', GETDATE()),
(33, N'BC-LS-005', N'AVAILABLE', N'Mới 100%', GETDATE()), (33, N'BC-LS-006', N'AVAILABLE', N'Mới 100%', GETDATE()),
(34, N'BC-LS-007', N'AVAILABLE', N'Mới 100%', GETDATE()), (34, N'BC-LS-008', N'AVAILABLE', N'Mới 100%', GETDATE()),
(35, N'BC-LS-009', N'AVAILABLE', N'Mới 100%', GETDATE()), (35, N'BC-LS-010', N'AVAILABLE', N'Mới 100%', GETDATE()),

-- Sách Ngoại ngữ (36 -> 40)
(36, N'BC-NN-001', N'AVAILABLE', N'Mới 100%', GETDATE()), (36, N'BC-NN-002', N'AVAILABLE', N'Mới 100%', GETDATE()),
(37, N'BC-NN-003', N'AVAILABLE', N'Mới 100%', GETDATE()), (37, N'BC-NN-004', N'AVAILABLE', N'Mới 100%', GETDATE()),
(38, N'BC-NN-005', N'AVAILABLE', N'Mới 100%', GETDATE()), (38, N'BC-NN-006', N'AVAILABLE', N'Mới 100%', GETDATE()),
(39, N'BC-NN-007', N'AVAILABLE', N'Mới 100%', GETDATE()), (39, N'BC-NN-008', N'AVAILABLE', N'Mới 100%', GETDATE()),
(40, N'BC-NN-009', N'AVAILABLE', N'Mới 100%', GETDATE()), (40, N'BC-NN-010', N'AVAILABLE', N'Mới 100%', GETDATE()),

-- Sách Y học (41 -> 45)
(41, N'BC-YH-001', N'AVAILABLE', N'Mới 100%', GETDATE()), (41, N'BC-YH-002', N'AVAILABLE', N'Mới 100%', GETDATE()),
(42, N'BC-YH-003', N'AVAILABLE', N'Mới 100%', GETDATE()), (42, N'BC-YH-004', N'AVAILABLE', N'Mới 100%', GETDATE()),
(43, N'BC-YH-005', N'AVAILABLE', N'Mới 100%', GETDATE()), (43, N'BC-YH-006', N'AVAILABLE', N'Mới 100%', GETDATE()),
(44, N'BC-YH-007', N'AVAILABLE', N'Mới 100%', GETDATE()), (44, N'BC-YH-008', N'AVAILABLE', N'Mới 100%', GETDATE()),
(45, N'BC-YH-009', N'AVAILABLE', N'Mới 100%', GETDATE()), (45, N'BC-YH-010', N'AVAILABLE', N'Mới 100%', GETDATE()),

-- Sách Thiết kế (46 -> 50)
(46, N'BC-TK-001', N'AVAILABLE', N'Mới 100%', GETDATE()), (46, N'BC-TK-002', N'AVAILABLE', N'Mới 100%', GETDATE()),
(47, N'BC-TK-003', N'AVAILABLE', N'Mới 100%', GETDATE()), (47, N'BC-TK-004', N'AVAILABLE', N'Mới 100%', GETDATE()),
(48, N'BC-TK-005', N'AVAILABLE', N'Mới 100%', GETDATE()), (48, N'BC-TK-006', N'AVAILABLE', N'Mới 100%', GETDATE()),
(49, N'BC-TK-007', N'AVAILABLE', N'Mới 100%', GETDATE()), (49, N'BC-TK-008', N'AVAILABLE', N'Mới 100%', GETDATE()),
(50, N'BC-TK-009', N'AVAILABLE', N'Mới 100%', GETDATE()), (50, N'BC-TK-010', N'AVAILABLE', N'Mới 100%', GETDATE());
GO

-- 6.6. Chèn Danh Sách 30 Thành Viên (Members: ID 1 -> 30)
INSERT INTO dbo.Members 
(MemberCardCode, FullName, PhoneNumber, Email, IdentityCardNumber, Address, DateOfBirth, DepositBalance, TotalDebt, IssueDate, ExpiryDate, CardStatus, Notes, CreatedAt) 
VALUES
-- Nhóm 1: Thành viên đang hoạt động mượn sách tích cực (1 -> 10)
(N'DG20260001', N'Nguyễn Hoàng Long', N'0912345601', N'long.nh@gmail.com', N'001203000001', N'128 Hoàng Mai, Hoàng Mai, Hà Nội', '1996-05-15', 200000, 0, DATEADD(day, -120, GETDATE()), DATEADD(day, 245, GETDATE()), N'ACTIVE', N'Kỹ sư Phần mềm Senior - Thành viên thư viện', DATEADD(day, -120, GETDATE())),
(N'DG20260002', N'Trần Thị Mai Anh', N'0987654302', N'maianh.tran@gmail.com', N'001203000002', N'45 Nguyễn Trãi, Thanh Xuân, Hà Nội', '1994-08-20', 500000, 0, DATEADD(day, -90, GETDATE()), DATEADD(day, 275, GETDATE()), N'ACTIVE', N'Chuyên viên Phân tích Đầu tư Tài chính - Thành viên thư viện', DATEADD(day, -90, GETDATE())),
(N'DG20260003', N'Lê Minh Khang', N'0903112203', N'khang.lm@gmail.com', N'001203000003', N'88 Cầu Giấy, Cầu Giấy, Hà Nội', '1993-11-10', 200000, 15000, DATEADD(day, -80, GETDATE()), DATEADD(day, 285, GETDATE()), N'ACTIVE', N'Kỹ sư Tự động hóa & IoT - Thành viên thư viện (Có nợ phạt trễ hạn 15.000đ)', DATEADD(day, -80, GETDATE())),
(N'DG20260004', N'Phạm Quỳnh Nga', N'0945223304', N'nga.pq@gmail.com', N'001203000004', N'12 Tôn Thất Tùng, Đống Đa, Hà Nội', '1995-02-28', 300000, 0, DATEADD(day, -75, GETDATE()), DATEADD(day, 290, GETDATE()), N'ACTIVE', N'Bác sĩ Đa khoa - Thành viên thư viện', DATEADD(day, -75, GETDATE())),
(N'DG20260005', N'Vũ Đức Huy', N'0978334405', N'huy.vd@gmail.com', N'001203000005', N'204 Trần Phú, Hà Đông, Hà Nội', '1991-09-14', 400000, 0, DATEADD(day, -60, GETDATE()), DATEADD(day, 305, GETDATE()), N'ACTIVE', N'Kiến trúc sư Giải pháp Đám mây - Thành viên thư viện', DATEADD(day, -60, GETDATE())),
(N'DG20260006', N'Đặng Thu Hà', N'0934445506', N'ha.dt@gmail.com', N'001203000006', N'56 Lê Văn Lương, Cầu Giấy, Hà Nội', '1997-12-05', 400000, 0, DATEADD(day, -55, GETDATE()), DATEADD(day, 310, GETDATE()), N'ACTIVE', N'Biên dịch viên Tự do - Thành viên thư viện', DATEADD(day, -55, GETDATE())),
(N'DG20260007', N'Bùi Gia Bảo', N'0967556607', N'bao.bg@gmail.com', N'001203000007', N'77 Bạch Mai, Hai Bà Trưng, Hà Nội', '1996-07-22', 200000, 0, DATEADD(day, -50, GETDATE()), DATEADD(day, 315, GETDATE()), N'ACTIVE', N'Chuyên viên Quản lý Quỹ Đầu tư - Thành viên thư viện', DATEADD(day, -50, GETDATE())),
(N'DG20260008', N'Đỗ Phương Thảo', N'0923667708', N'thao.dp@gmail.com', N'001203000008', N'19 Tràng Thi, Hoàn Kiếm, Hà Nội', '1992-04-18', 200000, 0, DATEADD(day, -45, GETDATE()), DATEADD(day, 320, GETDATE()), N'ACTIVE', N'Nhà nghiên cứu Lịch sử & Xuất bản - Thành viên thư viện', DATEADD(day, -45, GETDATE())),
(N'DG20260009', N'Hoàng Văn Tuấn', N'0911778809', N'tuan.hv@gmail.com', N'001203000009', N'102 Giải Phóng, Hai Bà Trưng, Hà Nội', '1988-01-30', 0, 0, DATEADD(day, -400, GETDATE()), DATEADD(day, -35, GETDATE()), N'CLOSED', N'Thành viên chuyển nơi công tác - Thẻ đã hủy & hoàn trả 100% cọc', DATEADD(day, -400, GETDATE())),
(N'DG20260010', N'Nguyễn Bảo Ngọc', N'0989889910', N'ngoc.nb@gmail.com', N'001203000010', N'34 Phạm Văn Đồng, Bắc Từ Liêm, Hà Nội', '1998-03-12', 200000, 0, DATEADD(day, -40, GETDATE()), DATEADD(day, 325, GETDATE()), N'LOCKED', N'Thẻ tạm thời bị khóa do thành viên báo rơi ví thẻ', DATEADD(day, -40, GETDATE())),

-- Nhóm 2: Chuyên gia, Nghiên cứu viên & Thành viên thường xuyên (11 -> 20)
(N'DG20260011', N'TS. Lê Thành Nam', N'0904123411', N'nam.le@vittech.vn', N'001203000011', N'Tòa nhà A2, KĐT An Hưng, Hà Đông, Hà Nội', '1985-06-18', 1000000, 0, DATEADD(day, -150, GETDATE()), DATEADD(day, 215, GETDATE()), N'ACTIVE', N'Chuyên gia Tư vấn Chuyển đổi số - Thành viên thư viện', DATEADD(day, -150, GETDATE())),
(N'DG20260012', N'ThS. Phạm Thu Trang', N'0918456712', N'trang.pham@globalcorp.vn', N'001203000012', N'CT4 The Pride, Tố Hữu, Hà Đông, Hà Nội', '1990-11-25', 600000, 0, DATEADD(day, -140, GETDATE()), DATEADD(day, 225, GETDATE()), N'ACTIVE', N'Giám đốc Phát triển Nhân lực - Thành viên thư viện', DATEADD(day, -140, GETDATE())),
(N'DG20260013', N'Đinh Quốc Huy', N'0972334413', N'huy.dq@gmail.com', N'001203000013', N'15 Mễ Trì Hạ, Nam Từ Liêm, Hà Nội', '1995-09-08', 200000, 0, DATEADD(day, -35, GETDATE()), DATEADD(day, 330, GETDATE()), N'ACTIVE', N'Kỹ sư Dữ liệu lớn - Thành viên thư viện', DATEADD(day, -35, GETDATE())),
(N'DG20260014', N'Võ Hoài An', N'0936556614', N'an.vh@gmail.com', N'001203000014', N'89 Xuân Thủy, Cầu Giấy, Hà Nội', '1997-01-14', 300000, 0, DATEADD(day, -30, GETDATE()), DATEADD(day, 335, GETDATE()), N'ACTIVE', N'Nhà phát triển Ứng dụng AI - Thành viên thư viện', DATEADD(day, -30, GETDATE())),
(N'DG20260015', N'Trịnh Quang Hưng', N'0981778815', N'hung.tq@gmail.com', N'001203000015', N'22 Tây Sơn, Đống Đa, Hà Nội', '1992-04-22', 200000, 25000, DATEADD(day, -380, GETDATE()), DATEADD(day, -15, GETDATE()), N'EXPIRED', N'Thẻ thành viên hết hạn 1 năm - Còn khoản phạt chậm trả 25.000đ', DATEADD(day, -380, GETDATE())),
(N'DG20260016', N'Lương Hải Yến', N'0908990016', N'yen.lh@gmail.com', N'001203000016', N'63 Nguyễn Khánh Toàn, Cầu Giấy, Hà Nội', '1994-10-30', 300000, 0, DATEADD(day, -28, GETDATE()), DATEADD(day, 337, GETDATE()), N'ACTIVE', N'Dược sĩ Nghiên cứu Lâm sàng - Thành viên thư viện', DATEADD(day, -28, GETDATE())),
(N'DG20260017', N'Hà Anh Tuấn', N'0942113317', N'tuan.ha@gmail.com', N'001203000017', N'112 Phố Huế, Hai Bà Trưng, Hà Nội', '1993-08-12', 200000, 0, DATEADD(day, -25, GETDATE()), DATEADD(day, 340, GETDATE()), N'ACTIVE', N'Kỹ sư Thiết kế Cơ khí - Thành viên thư viện', DATEADD(day, -25, GETDATE())),
(N'DG20260018', N'Mai Trọng Hiếu', N'0964224418', N'hieu.mt@gmail.com', N'001203000018', N'90 Láng Hạ, Đống Đa, Hà Nội', '1995-03-05', 200000, 0, DATEADD(day, -22, GETDATE()), DATEADD(day, 343, GETDATE()), N'ACTIVE', N'Quản lý Chuỗi Cung ứng - Thành viên thư viện', DATEADD(day, -22, GETDATE())),
(N'DG20260019', N'Lý Khánh Linh', N'0979335519', N'linh.lk@gmail.com', N'001203000019', N'37 Hồ Tùng Mậu, Cầu Giấy, Hà Nội', '1998-07-19', 400000, 0, DATEADD(day, -20, GETDATE()), DATEADD(day, 345, GETDATE()), N'ACTIVE', N'Senior UI/UX Designer - Thành viên thư viện', DATEADD(day, -20, GETDATE())),
(N'DG20260020', N'Phan Thanh Bình', N'0915446620', N'binh.pt@gmail.com', N'001203000020', N'180 Kim Mã, Ba Đình, Hà Nội', '1989-12-01', 0, 0, DATEADD(day, -420, GETDATE()), DATEADD(day, -55, GETDATE()), N'CLOSED', N'Thành viên hoàn tất dự án nghiên cứu & đóng thẻ rút cọc', DATEADD(day, -420, GETDATE())),

-- Nhóm 3: Thành viên mới đăng ký & Tự do (21 -> 30)
(N'DG20260021', N'Dương Gia Huy', N'0986557721', N'huy.dg@gmail.com', N'001203000021', N'52 Lạc Long Quân, Tây Hồ, Hà Nội', '1999-05-10', 200000, 0, DATEADD(day, -18, GETDATE()), DATEADD(day, 347, GETDATE()), N'ACTIVE', N'Lập trình viên Backend / DevOps - Thành viên mới', DATEADD(day, -18, GETDATE())),
(N'DG20260022', N'Tạ Thị Mỹ Duyên', N'0906668822', N'duyen.ttm@gmail.com', N'001203000022', N'28 Nguyễn Huy Tưởng, Thanh Xuân, Hà Nội', '1996-11-17', 200000, 0, DATEADD(day, -15, GETDATE()), DATEADD(day, 350, GETDATE()), N'ACTIVE', N'Quản lý Khách sạn & Du lịch - Thành viên mới', DATEADD(day, -15, GETDATE())),
(N'DG20260023', N'Cao Văn Sơn', N'0938779923', N'son.cv@gmail.com', N'001203000023', N'144 Đội Cấn, Ba Đình, Hà Nội', '1994-03-24', 200000, 0, DATEADD(day, -14, GETDATE()), DATEADD(day, 351, GETDATE()), N'ACTIVE', N'Kỹ sư Robot & Tự động hóa - Thành viên mới', DATEADD(day, -14, GETDATE())),
(N'DG20260024', N'Chu Kim Ngân', N'0971880024', N'ngan.ck@gmail.com', N'001203000024', N'83 Nguyễn Chí Thanh, Đống Đa, Hà Nội', '1995-06-30', 500000, 0, DATEADD(day, -12, GETDATE()), DATEADD(day, 353, GETDATE()), N'ACTIVE', N'Chuyên viên Tư vấn Rủi ro Tài chính - Thành viên mới', DATEADD(day, -12, GETDATE())),
(N'DG20260025', N'Trần Quốc Bảo', N'0983991125', N'bao.tq@gmail.com', N'001203000025', N'67 Xã Đàn, Đống Đa, Hà Nội', '1993-09-15', 200000, 50000, DATEADD(day, -70, GETDATE()), DATEADD(day, 295, GETDATE()), N'LOCKED', N'Thẻ tạm khóa tự động do quá hạn sách > 20 ngày chưa hoàn trả', DATEADD(day, -70, GETDATE())),
(N'DG20260026', N'Nghiêm Minh Nhật', N'0917112226', N'nhat.nm@gmail.com', N'001203000026', N'109 Định Công, Hoàng Mai, Hà Nội', '1998-02-14', 300000, 0, DATEADD(day, -8, GETDATE()), DATEADD(day, 357, GETDATE()), N'ACTIVE', N'Chuyên viên An toàn Thông tin - Thành viên mới', DATEADD(day, -8, GETDATE())),
(N'DG20260027', N'Lâm Bích Trâm', N'0948223327', N'tram.lb@gmail.com', N'001203000027', N'40 Hàng Bông, Hoàn Kiếm, Hà Nội', '1996-12-08', 200000, 0, DATEADD(day, -6, GETDATE()), DATEADD(day, 359, GETDATE()), N'ACTIVE', N'Nghiên cứu viên Công nghệ Sinh học - Thành viên mới', DATEADD(day, -6, GETDATE())),
(N'DG20260028', N'Đoàn Văn Hậu', N'0969334428', N'hau.dv@gmail.com', N'001203000028', N'195 Khâm Thiên, Đống Đa, Hà Nội', '1991-04-19', 200000, 0, DATEADD(day, -370, GETDATE()), DATEADD(day, -5, GETDATE()), N'EXPIRED', N'Hết hạn thẻ mượn sách, thành viên chưa làm thủ tục gia hạn', DATEADD(day, -370, GETDATE())),
(N'DG20260029', N'La Ngọc Diệp', N'0975445529', N'diep.ln@gmail.com', N'001203000029', N'11 Thụy Khuê, Tây Hồ, Hà Nội', '1999-08-27', 200000, 0, DATEADD(day, -3, GETDATE()), DATEADD(day, 362, GETDATE()), N'ACTIVE', N'Quản lý Vận hành E-Commerce - Thành viên mới', DATEADD(day, -3, GETDATE())),
(N'DG20260030', N'Phùng Thế Vinh', N'0902556630', N'vinh.pt@gmail.com', N'001203000030', N'75 Vũ Trọng Phụng, Thanh Xuân, Hà Nội', '1997-10-03', 200000, 0, DATEADD(day, -1, GETDATE()), DATEADD(day, 364, GETDATE()), N'ACTIVE', N'Thành viên mới kích hoạt tài khoản Cổng dịch vụ đọc');
GO

-- 6.7. Chèn 30 Tài Khoản Cổng Thành Viên (UserAccounts: Reader Portal)
-- Tên đăng nhập = Mã thẻ hội viên (DG20260001 -> DG20260030)
-- Mật khẩu mặc định: LibOps@123 | Salt: LIB_OPS_SALT_2026 | Hash SHA-256: 72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638
DECLARE @ReaderRoleId INT = (SELECT TOP 1 RoleId FROM dbo.Roles WHERE RoleName = 'READER');

INSERT INTO dbo.UserAccounts 
(Username, PasswordHash, PasswordSalt, FullName, Email, PhoneNumber, RoleId, MemberId, IsActive, CreatedAt) 
VALUES
(N'DG20260001', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Nguyễn Hoàng Long', N'long.nh@gmail.com', N'0912345601', @ReaderRoleId, 1, 1, DATEADD(day, -120, GETDATE())),
(N'DG20260002', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Trần Thị Mai Anh', N'maianh.tran@gmail.com', N'0987654302', @ReaderRoleId, 2, 1, DATEADD(day, -90, GETDATE())),
(N'DG20260003', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Lê Minh Khang', N'khang.lm@gmail.com', N'0903112203', @ReaderRoleId, 3, 1, DATEADD(day, -80, GETDATE())),
(N'DG20260004', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Phạm Quỳnh Nga', N'nga.pq@gmail.com', N'0945223304', @ReaderRoleId, 4, 1, DATEADD(day, -75, GETDATE())),
(N'DG20260005', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Vũ Đức Huy', N'huy.vd@gmail.com', N'0978334405', @ReaderRoleId, 5, 1, DATEADD(day, -60, GETDATE())),
(N'DG20260006', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Đặng Thu Hà', N'ha.dt@gmail.com', N'0934445506', @ReaderRoleId, 6, 1, DATEADD(day, -55, GETDATE())),
(N'DG20260007', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Bùi Gia Bảo', N'bao.bg@gmail.com', N'0967556607', @ReaderRoleId, 7, 1, DATEADD(day, -50, GETDATE())),
(N'DG20260008', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Đỗ Phương Thảo', N'thao.dp@gmail.com', N'0923667708', @ReaderRoleId, 8, 1, DATEADD(day, -45, GETDATE())),
(N'DG20260009', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Hoàng Văn Tuấn', N'tuan.hv@gmail.com', N'0911778809', @ReaderRoleId, 9, 0, DATEADD(day, -400, GETDATE())),
(N'DG20260010', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Nguyễn Bảo Ngọc', N'ngoc.nb@gmail.com', N'0989889910', @ReaderRoleId, 10, 0, DATEADD(day, -40, GETDATE())),
(N'DG20260011', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'TS. Lê Thành Nam', N'nam.le@vittech.vn', N'0904123411', @ReaderRoleId, 11, 1, DATEADD(day, -150, GETDATE())),
(N'DG20260012', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'ThS. Phạm Thu Trang', N'trang.pham@globalcorp.vn', N'0918456712', @ReaderRoleId, 12, 1, DATEADD(day, -140, GETDATE())),
(N'DG20260013', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Đinh Quốc Huy', N'huy.dq@gmail.com', N'0972334413', @ReaderRoleId, 13, 1, DATEADD(day, -35, GETDATE())),
(N'DG20260014', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Võ Hoài An', N'an.vh@gmail.com', N'0936556614', @ReaderRoleId, 14, 1, DATEADD(day, -30, GETDATE())),
(N'DG20260015', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Trịnh Quang Hưng', N'hung.tq@gmail.com', N'0981778815', @ReaderRoleId, 15, 1, DATEADD(day, -380, GETDATE())),
(N'DG20260016', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Lương Hải Yến', N'yen.lh@gmail.com', N'0908990016', @ReaderRoleId, 16, 1, DATEADD(day, -28, GETDATE())),
(N'DG20260017', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Hà Anh Tuấn', N'tuan.ha@gmail.com', N'0942113317', @ReaderRoleId, 17, 1, DATEADD(day, -25, GETDATE())),
(N'DG20260018', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Mai Trọng Hiếu', N'hieu.mt@gmail.com', N'0964224418', @ReaderRoleId, 18, 1, DATEADD(day, -22, GETDATE())),
(N'DG20260019', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Lý Khánh Linh', N'linh.lk@gmail.com', N'0979335519', @ReaderRoleId, 19, 1, DATEADD(day, -20, GETDATE())),
(N'DG20260020', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Phan Thanh Bình', N'binh.pt@gmail.com', N'0915446620', @ReaderRoleId, 20, 0, DATEADD(day, -420, GETDATE())),
(N'DG20260021', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Dương Gia Huy', N'huy.dg@gmail.com', N'0986557721', @ReaderRoleId, 21, 1, DATEADD(day, -18, GETDATE())),
(N'DG20260022', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Tạ Thị Mỹ Duyên', N'duyen.ttm@gmail.com', N'0906668822', @ReaderRoleId, 22, 1, DATEADD(day, -15, GETDATE())),
(N'DG20260023', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Cao Văn Sơn', N'son.cv@gmail.com', N'0938779923', @ReaderRoleId, 23, 1, DATEADD(day, -14, GETDATE())),
(N'DG20260024', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Chu Kim Ngân', N'ngan.ck@gmail.com', N'0971880024', @ReaderRoleId, 24, 1, DATEADD(day, -12, GETDATE())),
(N'DG20260025', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Trần Quốc Bảo', N'bao.tq@gmail.com', N'0983991125', @ReaderRoleId, 25, 0, DATEADD(day, -70, GETDATE())),
(N'DG20260026', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Nghiêm Minh Nhật', N'nhat.nm@gmail.com', N'0917112226', @ReaderRoleId, 26, 1, DATEADD(day, -8, GETDATE())),
(N'DG20260027', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Lâm Bích Trâm', N'tram.lb@gmail.com', N'0948223327', @ReaderRoleId, 27, 1, DATEADD(day, -6, GETDATE())),
(N'DG20260028', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Đoàn Văn Hậu', N'hau.dv@gmail.com', N'0969334428', @ReaderRoleId, 28, 1, DATEADD(day, -370, GETDATE())),
(N'DG20260029', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'La Ngọc Diệp', N'diep.ln@gmail.com', N'0975445529', @ReaderRoleId, 29, 1, DATEADD(day, -3, GETDATE())),
(N'DG20260030', N'72D69FE48D119E721A5C26712BF734151AF9B70AD0419BC6480BD55344B6B638', N'LIB_OPS_SALT_2026', N'Phùng Thế Vinh', N'vinh.pt@gmail.com', N'0902556630', @ReaderRoleId, 30, 1, DATEADD(day, -1, GETDATE()));
GO

-- 6.8. Ghi Sổ Cái Giao Dịch Tiền Cọc Thế Chân (DepositTransactions)
INSERT INTO dbo.DepositTransactions 
(MemberId, HandledByUserId, TransactionType, Amount, BalanceAfter, ReceiptCode, TransactionDate, Notes) 
VALUES
-- Nộp cọc mở thẻ ban đầu (200.000đ mỗi thành viên)
(1, 1, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260520-001', DATEADD(day, -120, GETDATE()), N'Nộp tiền cọc thế chân mở thẻ mới tại quầy'),
(2, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260620-002', DATEADD(day, -90, GETDATE()), N'Nộp tiền cọc thế chân mở thẻ mới tại quầy'),
(3, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260630-003', DATEADD(day, -80, GETDATE()), N'Nộp tiền cọc thế chân mở thẻ mới tại quầy'),
(4, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260705-004', DATEADD(day, -75, GETDATE()), N'Nộp tiền cọc thế chân mở thẻ mới tại quầy'),
(5, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260720-005', DATEADD(day, -60, GETDATE()), N'Nộp tiền cọc thế chân mở thẻ mới tại quầy'),
(6, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260725-006', DATEADD(day, -55, GETDATE()), N'Nộp tiền cọc thế chân mở thẻ mới tại quầy'),
(7, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260730-007', DATEADD(day, -50, GETDATE()), N'Nộp tiền cọc thế chân mở thẻ mới tại quầy'),
(8, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260805-008', DATEADD(day, -45, GETDATE()), N'Nộp tiền cọc thế chân mở thẻ mới tại quầy'),
(9, 1, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20250810-009', DATEADD(day, -400, GETDATE()), N'Nộp tiền cọc mở thẻ ban đầu'),
(10, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260810-010', DATEADD(day, -40, GETDATE()), N'Nộp tiền cọc thế chân mở thẻ mới tại quầy'),
(11, 1, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260420-011', DATEADD(day, -150, GETDATE()), N'Nộp tiền cọc ban đầu mở thẻ thành viên'),
(12, 1, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260502-012', DATEADD(day, -140, GETDATE()), N'Nộp tiền cọc ban đầu mở thẻ thành viên'),
(13, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260815-013', DATEADD(day, -35, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(14, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260820-014', DATEADD(day, -30, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(15, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20250901-015', DATEADD(day, -380, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(16, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260822-016', DATEADD(day, -28, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(17, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260825-017', DATEADD(day, -25, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(18, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260828-018', DATEADD(day, -22, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(19, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260830-019', DATEADD(day, -20, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(20, 1, N'INITIAL_DEPOSIT', 500000, 500000, N'REC-DEP-20250715-020', DATEADD(day, -420, GETDATE()), N'Nộp tiền cọc mở thẻ thành viên'),
(21, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260901-021', DATEADD(day, -18, GETDATE()), N'Nộp tiền cọc thành viên mới'),
(22, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260904-022', DATEADD(day, -15, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(23, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260905-023', DATEADD(day, -14, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(24, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260907-024', DATEADD(day, -12, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(25, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260710-025', DATEADD(day, -70, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(26, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260911-026', DATEADD(day, -8, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(27, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260913-027', DATEADD(day, -6, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(28, 1, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20250910-028', DATEADD(day, -370, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(29, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260916-029', DATEADD(day, -3, GETDATE()), N'Nộp tiền cọc mở thẻ'),
(30, 2, N'INITIAL_DEPOSIT', 200000, 200000, N'REC-DEP-20260918-030', DATEADD(day, -1, GETDATE()), N'Nộp tiền cọc mở thẻ');

-- Giao dịch nạp thêm cọc (TOPUP_DEPOSIT) nâng hạn mức mượn qua VietQR PayOS & Quầy
INSERT INTO dbo.DepositTransactions 
(MemberId, HandledByUserId, TransactionType, Amount, BalanceAfter, ReceiptCode, TransactionDate, Notes) 
VALUES
(2, 1, N'TOPUP_DEPOSIT', 300000, 500000, N'REC-DEP-20260715-031', DATEADD(day, -65, GETDATE()), N'Nạp thêm tiền cọc qua cổng VietQR PayOS để mượn đồng thời 5 cuốn'),
(4, 2, N'TOPUP_DEPOSIT', 100000, 300000, N'REC-DEP-20260810-032', DATEADD(day, -40, GETDATE()), N'Nộp tiền mặt tại quầy nâng hạn mức mượn thêm sách chuyên khảo'),
(5, 1, N'TOPUP_DEPOSIT', 200000, 400000, N'REC-DEP-20260818-033', DATEADD(day, -32, GETDATE()), N'Nạp thêm tiền cọc chuyển khoản VietQR'),
(6, 2, N'TOPUP_DEPOSIT', 200000, 400000, N'REC-DEP-20260825-034', DATEADD(day, -25, GETDATE()), N'Nộp bổ sung tiền cọc tại quầy'),
(11, 1, N'TOPUP_DEPOSIT', 800000, 1000000, N'REC-DEP-20260510-035', DATEADD(day, -130, GETDATE()), N'Nâng hạn mức cọc để mượn thêm tài liệu nghiên cứu'),
(12, 1, N'TOPUP_DEPOSIT', 400000, 600000, N'REC-DEP-20260601-036', DATEADD(day, -110, GETDATE()), N'Nâng hạn mức cọc mượn tài liệu chuyên ngành'),
(14, 2, N'TOPUP_DEPOSIT', 100000, 300000, N'REC-DEP-20260902-037', DATEADD(day, -17, GETDATE()), N'Nạp thêm tiền cọc tại quầy'),
(16, 2, N'TOPUP_DEPOSIT', 100000, 300000, N'REC-DEP-20260908-038', DATEADD(day, -11, GETDATE()), N'Nạp thêm cọc chuyển khoản ngân hàng'),
(19, 1, N'TOPUP_DEPOSIT', 200000, 400000, N'REC-DEP-20260912-039', DATEADD(day, -7, GETDATE()), N'Nạp cọc trực tuyến qua VietQR PayOS'),
(24, 2, N'TOPUP_DEPOSIT', 300000, 500000, N'REC-DEP-20260915-040', DATEADD(day, -4, GETDATE()), N'Nộp tiền mặt nâng hạn mức cọc mượn sách');

-- Giao dịch hoàn trả cọc (REFUND_DEPOSIT) khi độc giả đóng thẻ
INSERT INTO dbo.DepositTransactions 
(MemberId, HandledByUserId, TransactionType, Amount, BalanceAfter, ReceiptCode, TransactionDate, Notes) 
VALUES
(9, 1, N'REFUND_DEPOSIT', 200000, 0, N'REC-DEP-20260815-041', DATEADD(day, -35, GETDATE()), N'Tất toán hoàn trả 100% tiền cọc khi thành viên đóng thẻ'),
(20, 1, N'REFUND_DEPOSIT', 500000, 0, N'REC-DEP-20260725-042', DATEADD(day, -55, GETDATE()), N'Tất toán hoàn cọc chuyển khoản khi thành viên thanh lý hợp đồng thẻ');
GO

-- 6.9. Ghi Biên Lai Thu Phí Dịch Vụ (ServiceFeeReceipts)
INSERT INTO dbo.ServiceFeeReceipts 
(ReceiptCode, MemberId, FeeType, Amount, PaymentMethod, PaymentDate, CollectedByUserId, Notes) 
VALUES
-- Thu phí phát hành thẻ mới cho 30 thành viên (50.000đ/thẻ)
(N'FEE-20260520-001', 1, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -120, GETDATE()), 1, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260620-002', 2, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -90, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260630-003', 3, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -80, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260705-004', 4, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -75, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260720-005', 5, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -60, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260725-006', 6, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -55, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260730-007', 7, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -50, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260805-008', 8, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -45, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20250810-009', 9, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -400, GETDATE()), 1, N'Thu phí phát hành thẻ thành viên'),
(N'FEE-20260810-010', 10, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -40, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260420-011', 11, N'CARD_ISSUANCE', 50000, N'BANK_TRANSFER', DATEADD(day, -150, GETDATE()), 1, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260502-012', 12, N'CARD_ISSUANCE', 50000, N'BANK_TRANSFER', DATEADD(day, -140, GETDATE()), 1, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260815-013', 13, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -35, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260820-014', 14, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -30, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20250901-015', 15, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -380, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên'),
(N'FEE-20260822-016', 16, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -28, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260825-017', 17, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -25, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260828-018', 18, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -22, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260830-019', 19, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -20, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20250715-020', 20, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -420, GETDATE()), 1, N'Thu phí phát hành thẻ thành viên'),
(N'FEE-20260901-021', 21, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -18, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260904-022', 22, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -15, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260905-023', 23, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -14, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260907-024', 24, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -12, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260710-025', 25, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -70, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260911-026', 26, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -8, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260913-027', 27, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -6, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20250910-028', 28, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -370, GETDATE()), 1, N'Thu phí phát hành thẻ thành viên'),
(N'FEE-20260916-029', 29, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -3, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới'),
(N'FEE-20260918-030', 30, N'CARD_ISSUANCE', 50000, N'CASH', DATEADD(day, -1, GETDATE()), 2, N'Thu phí phát hành thẻ thành viên mới');

-- Thu phí dịch vụ gia hạn thẻ thường niên & cấp lại thẻ (CARD_RENEWAL / CARD_REISSUE)
INSERT INTO dbo.ServiceFeeReceipts 
(ReceiptCode, MemberId, FeeType, Amount, PaymentMethod, PaymentDate, CollectedByUserId, Notes) 
VALUES
(N'FEE-20260810-031', 1, N'CARD_RENEWAL', 50000, N'BANK_TRANSFER', DATEADD(day, -42, GETDATE()), 1, N'Thu phí gia hạn gói thẻ thành viên thường niên năm thứ 2'),
(N'FEE-20260820-032', 5, N'CARD_RENEWAL', 50000, N'CASH', DATEADD(day, -30, GETDATE()), 2, N'Thu phí gia hạn gói thẻ thành viên thường niên'),
(N'FEE-20260905-033', 11, N'CARD_RENEWAL', 50000, N'BANK_TRANSFER', DATEADD(day, -14, GETDATE()), 1, N'Thu phí gia hạn gói thẻ thành viên thường niên'),
(N'FEE-20260912-034', 10, N'CARD_REISSUE', 50000, N'CASH', DATEADD(day, -7, GETDATE()), 2, N'Thu phí cấp lại thẻ mới do làm mất thẻ thư viện cũ'),
(N'FEE-20260915-035', 4, N'CARD_REISSUE', 50000, N'CASH', DATEADD(day, -4, GETDATE()), 2, N'Thu phí cấp lại phôi thẻ từ mới');
GO

-- 6.10. Chèn Danh Sách 35 Phiếu Mượn Sách (BorrowSlips: ID 1 -> 35)
INSERT INTO dbo.BorrowSlips 
(SlipCode, MemberId, CreatedByUserId, BorrowDate, DueDate, RenewalCount, Status, Notes) 
VALUES
-- [1-15]: Các phiếu mượn lịch sử đã hoàn trả xong (RETURNED)
(N'PM202607-001', 1, 2, DATEADD(day, -50, GETDATE()), DATEADD(day, -36, GETDATE()), 0, N'RETURNED', N'Mượn sách kỹ thuật lập trình nâng cao'),
(N'PM202607-002', 2, 2, DATEADD(day, -45, GETDATE()), DATEADD(day, -31, GETDATE()), 0, N'RETURNED', N'Mượn sách kinh tế tài chính'),
(N'PM202608-003', 4, 1, DATEADD(day, -40, GETDATE()), DATEADD(day, -26, GETDATE()), 0, N'RETURNED', N'Mượn sách chuyên khảo y học'),
(N'PM202608-004', 5, 2, DATEADD(day, -35, GETDATE()), DATEADD(day, -21, GETDATE()), 0, N'RETURNED', N'Mượn sách kiến trúc hệ thống dữ liệu'),
(N'PM202608-005', 6, 2, DATEADD(day, -30, GETDATE()), DATEADD(day, -16, GETDATE()), 0, N'RETURNED', N'Mượn sách văn học nước ngoài'),
(N'PM202608-006', 7, 2, DATEADD(day, -28, GETDATE()), DATEADD(day, -14, GETDATE()), 0, N'RETURNED', N'Mượn sách kỹ năng mềm'),
(N'PM202608-007', 8, 2, DATEADD(day, -25, GETDATE()), DATEADD(day, -11, GETDATE()), 0, N'RETURNED', N'Mượn sách lịch sử & triết học'),
(N'PM202608-008', 11, 1, DATEADD(day, -24, GETDATE()), DATEADD(day, -10, GETDATE()), 0, N'RETURNED', N'Thành viên mượn tài liệu nghiên cứu chuyên đề'),
(N'PM202608-009', 12, 1, DATEADD(day, -22, GETDATE()), DATEADD(day, -8, GETDATE()), 0, N'RETURNED', N'Thành viên mượn sách kinh tế đầu tư chiến lược'),
(N'PM202608-010', 13, 2, DATEADD(day, -20, GETDATE()), DATEADD(day, -6, GETDATE()), 0, N'RETURNED', N'Mượn sách khoa học máy tính'),
(N'PM202608-011', 14, 2, DATEADD(day, -18, GETDATE()), DATEADD(day, -4, GETDATE()), 0, N'RETURNED', N'Mượn sách thiết kế UI/UX'),
(N'PM202609-012', 16, 2, DATEADD(day, -16, GETDATE()), DATEADD(day, -2, GETDATE()), 0, N'RETURNED', N'Mượn sách y học thường thức'),
(N'PM202609-013', 17, 2, DATEADD(day, -15, GETDATE()), DATEADD(day, -1, GETDATE()), 0, N'RETURNED', N'Mượn sách khoa học thiên văn'),
(N'PM202609-014', 18, 2, DATEADD(day, -14, GETDATE()), GETDATE(), 0, N'RETURNED', N'Đã hoàn trả trong sáng hôm nay'),
(N'PM202609-015', 19, 2, DATEADD(day, -12, GETDATE()), DATEADD(day, 2, GETDATE()), 0, N'RETURNED', N'Thành viên trả sớm trước hạn 2 ngày'),

-- [16-29]: Các phiếu mượn hiện hành đang hoạt động (BORROWING)
(N'PM202609-016', 1, 2, DATEADD(day, -10, GETDATE()), DATEADD(day, 4, GETDATE()), 0, N'BORROWING', N'Mượn sách Clean Architecture phục vụ dự án công nghệ'),
(N'PM202609-017', 2, 2, DATEADD(day, -8, GETDATE()), DATEADD(day, 6, GETDATE()), 0, N'BORROWING', N'Mượn sách Tâm lý học về tiền'),
(N'PM202609-018', 4, 1, DATEADD(day, -7, GETDATE()), DATEADD(day, 7, GETDATE()), 0, N'BORROWING', N'Mượn sách Sao chúng ta lại ngủ & Cơ thể tự chữa lành'),
(N'PM202609-019', 5, 2, DATEADD(day, -6, GETDATE()), DATEADD(day, 8, GETDATE()), 0, N'BORROWING', N'Mượn sách Designing Data-Intensive Applications'),
(N'PM202609-020', 6, 2, DATEADD(day, -5, GETDATE()), DATEADD(day, 9, GETDATE()), 0, N'BORROWING', N'Mượn sách Luyện siêu trí nhớ từ vựng tiếng Anh'),
(N'PM202609-021', 7, 2, DATEADD(day, -5, GETDATE()), DATEADD(day, 9, GETDATE()), 0, N'BORROWING', N'Mượn sách Chiến lược đại dương xanh'),
(N'PM202609-022', 8, 2, DATEADD(day, -4, GETDATE()), DATEADD(day, 10, GETDATE()), 0, N'BORROWING', N'Mượn tiểu thuyết Rừng Na Uy'),
(N'PM202609-023', 11, 1, DATEADD(day, -4, GETDATE()), DATEADD(day, 17, GETDATE()), 1, N'BORROWING', N'Thành viên đã gia hạn thêm 7 ngày (RenewalCount=1)'),
(N'PM202609-024', 13, 2, DATEADD(day, -3, GETDATE()), DATEADD(day, 11, GETDATE()), 0, N'BORROWING', N'Mượn sách The Pragmatic Programmer'),
(N'PM202609-025', 14, 2, DATEADD(day, -3, GETDATE()), DATEADD(day, 11, GETDATE()), 0, N'BORROWING', N'Mượn sách Thiết kế cho cuộc sống & Đừng bắt tôi phải nghĩ'),
(N'PM202609-026', 16, 2, DATEADD(day, -2, GETDATE()), DATEADD(day, 12, GETDATE()), 0, N'BORROWING', N'Mượn sách Nhân tố Enzyme'),
(N'PM202609-027', 21, 2, DATEADD(day, -2, GETDATE()), DATEADD(day, 12, GETDATE()), 0, N'BORROWING', N'Thành viên mượn sách Clean Code'),
(N'PM202609-028', 22, 2, DATEADD(day, -1, GETDATE()), DATEADD(day, 13, GETDATE()), 0, N'BORROWING', N'Mượn sách Đắc nhân tâm'),
(N'PM202609-029', 24, 2, GETDATE(), DATEADD(day, 14, GETDATE()), 0, N'BORROWING', N'Mượn sách Nhà đầu tư thông minh & Zero to One'),

-- [30-35]: Các phiếu mượn trễ hạn cần thu hồi (OVERDUE)
(N'PM202608-030', 3, 2, DATEADD(day, -25, GETDATE()), DATEADD(day, -11, GETDATE()), 0, N'OVERDUE', N'Quá hạn 11 ngày - Đã gửi email thông báo tự động lần 2'),
(N'PM202608-031', 15, 2, DATEADD(day, -28, GETDATE()), DATEADD(day, -14, GETDATE()), 0, N'OVERDUE', N'Quá hạn 14 ngày - Thủ thư đã gọi điện đôn đốc'),
(N'PM202608-032', 25, 2, DATEADD(day, -35, GETDATE()), DATEADD(day, -21, GETDATE()), 0, N'OVERDUE', N'Quá hạn 21 ngày - Thẻ đã bị hệ thống tự động khóa'),
(N'PM202609-033', 17, 2, DATEADD(day, -18, GETDATE()), DATEADD(day, -4, GETDATE()), 0, N'OVERDUE', N'Quá hạn 4 ngày - Phí phạt tích lũy 20.000đ'),
(N'PM202609-034', 18, 2, DATEADD(day, -16, GETDATE()), DATEADD(day, -2, GETDATE()), 0, N'OVERDUE', N'Quá hạn 2 ngày - Thành viên xin trả vào cuối tuần'),
(N'PM202609-035', 23, 2, DATEADD(day, -15, GETDATE()), DATEADD(day, -1, GETDATE()), 0, N'OVERDUE', N'Quá hạn 1 ngày');
GO

-- 6.11. Chèn Chi Tiết Phiếu Mượn Sách (BorrowSlipDetails: ID 1 -> 58)
INSERT INTO dbo.BorrowSlipDetails 
(BorrowSlipId, CopyId, BorrowConditionNote) 
VALUES
-- Phiếu 1 -> 15 (Lịch sử đã trả)
(1, 1, N'Sách mới 100%, nguyên tem niêm phong'),
(1, 3, N'Bìa sách thẳng đẹp'),
(2, 11, N'Sách nguyên vẹn'),
(3, 81, N'Sách mới 100%'),
(4, 9, N'Bìa và ruột sách tốt'),
(5, 41, N'Nguyên vẹn'),
(5, 43, N'Mới 100%'),
(6, 21, N'Sách đẹp'),
(7, 61, N'Nguyên vẹn'),
(8, 5, N'Tài liệu nghiên cứu chất lượng tốt'),
(8, 7, N'Nguyên vẹn'),
(9, 13, N'Sách mới'),
(10, 8, N'Nguyên vẹn'),
(11, 91, N'Bìa tốt'),
(12, 83, N'Sách mới'),
(13, 51, N'Nguyên vẹn'),
(14, 31, N'Sách tốt'),
(14, 33, N'Nguyên vẹn'),
(15, 23, N'Sách đẹp'),

-- Phiếu 16 -> 29 (Đang mượn - Chiếm các bản sao CopyId thực tế)
(16, 4, N'Sách mới 100%'),
(17, 12, N'Nguyên vẹn'),
(18, 82, N'Bìa tốt, gáy thẳng'),
(18, 85, N'Sách nguyên vẹn'),
(19, 10, N'Sách chuyên khảo mới'),
(20, 71, N'Kèm đĩa CD luyện nghe tiếng Anh'),
(21, 15, N'Nguyên vẹn'),
(22, 45, N'Sách mới 100%'),
(23, 6, N'Tài liệu nghiên cứu chuyên sâu'),
(23, 14, N'Bìa sách tốt'),
(24, 8, N'Nguyên vẹn'),
(25, 92, N'Bìa đẹp, không gấp mép'),
(25, 94, N'Sách mới 100%'),
(26, 84, N'Nguyên vẹn'),
(27, 2, N'Sách nguyên vẹn'),
(28, 22, N'Sách đẹp'),
(29, 16, N'Bìa sách tốt'),
(29, 18, N'Nguyên vẹn'),

-- Phiếu 30 -> 35 (Quá hạn - Chiếm các bản sao)
(30, 25, N'Sách có vết gấp nhẹ góc bìa'),
(30, 27, N'Nguyên vẹn'),
(31, 53, N'Sách mới'),
(32, 63, N'Bìa tốt'),
(33, 55, N'Sách nguyên vẹn'),
(34, 73, N'Kèm tài liệu bài tập'),
(35, 35, N'Sách nguyên vẹn');
GO

-- 6.12. Chèn Chi Tiết Trả Sách Thực Tế (ReturnSlipDetails)
INSERT INTO dbo.ReturnSlipDetails 
(BorrowSlipDetailId, ReceivedByUserId, ActualReturnDate, OverdueDays, FineAmount, ReturnConditionNote, BookCopyStatusAfterReturn) 
VALUES
(1, 2, DATEADD(day, -38, GETDATE()), 0, 0, N'Sách trả đúng hạn, nguyên vẹn 100%', N'AVAILABLE'),
(2, 2, DATEADD(day, -38, GETDATE()), 0, 0, N'Sách trả đúng hạn, bìa tốt', N'AVAILABLE'),
(3, 2, DATEADD(day, -32, GETDATE()), 0, 0, N'Sách trả đúng hạn, không hư hại', N'AVAILABLE'),
(4, 1, DATEADD(day, -27, GETDATE()), 0, 0, N'Sách y học nguyên vẹn', N'AVAILABLE'),
(5, 2, DATEADD(day, -22, GETDATE()), 0, 0, N'Sách trả đúng hạn, tình trạng hoàn hảo', N'AVAILABLE'),
(6, 2, DATEADD(day, -17, GETDATE()), 0, 0, N'Sách nguyên vẹn', N'AVAILABLE'),
(7, 2, DATEADD(day, -17, GETDATE()), 0, 0, N'Sách nguyên vẹn', N'AVAILABLE'),
(8, 2, DATEADD(day, -15, GETDATE()), 0, 0, N'Sách trả đúng hạn', N'AVAILABLE'),
(9, 2, DATEADD(day, -12, GETDATE()), 0, 0, N'Sách nguyên vẹn', N'AVAILABLE'),
(10, 1, DATEADD(day, -11, GETDATE()), 0, 0, N'Thành viên trả đúng hạn', N'AVAILABLE'),
(11, 1, DATEADD(day, -11, GETDATE()), 0, 0, N'Thành viên trả đúng hạn', N'AVAILABLE'),
(12, 1, DATEADD(day, -9, GETDATE()), 0, 0, N'Sách kinh tế trả đúng hạn', N'AVAILABLE'),
(13, 2, DATEADD(day, -7, GETDATE()), 0, 0, N'Sách CNTT nguyên vẹn', N'AVAILABLE'),
(14, 2, DATEADD(day, -5, GETDATE()), 0, 0, N'Sách thiết kế nguyên vẹn', N'AVAILABLE'),
(15, 2, DATEADD(day, -3, GETDATE()), 0, 0, N'Sách y học nguyên vẹn', N'AVAILABLE'),
(16, 2, DATEADD(day, -2, GETDATE()), 0, 0, N'Sách vũ trụ học nguyên vẹn', N'AVAILABLE'),
(17, 2, GETDATE(), 0, 0, N'Sách văn học trả đúng hạn trong ngày', N'AVAILABLE'),
(18, 2, GETDATE(), 0, 0, N'Sách văn học trả đúng hạn trong ngày', N'AVAILABLE'),
(19, 2, DATEADD(day, -1, GETDATE()), 0, 0, N'Thành viên trả trước hạn 2 ngày, sách rất mới', N'AVAILABLE');
GO

-- 6.13. Chèn Biên Lai Thu Tiền Phạt Xử Lý Vi Phạm (FineReceipts)
INSERT INTO dbo.FineReceipts 
(ReceiptCode, MemberId, BorrowSlipId, CollectedByUserId, TotalAmount, PaymentMethod, Reason, PaymentDate, Notes) 
VALUES
(N'REC-FINE-20260715-001', 3, 1, 2, 15000, N'DEPOSIT_DEDUCTION', N'Phạt quá hạn 3 ngày (5.000đ/ngày)', DATEADD(day, -35, GETDATE()), N'Đã cấn trừ trực tiếp vào tiền cọc thành viên'),
(N'REC-FINE-20260802-002', 15, 3, 2, 25000, N'DEPOSIT_DEDUCTION', N'Phạt quá hạn 5 ngày (5.000đ/ngày)', DATEADD(day, -20, GETDATE()), N'Khấu trừ vào số dư cọc của thẻ'),
(N'REC-FINE-20260818-003', 1, NULL, 1, 40000, N'CASH', N'Phạt làm rách nhẹ trang bìa sách Văn học Việt Nam', DATEADD(day, -15, GETDATE()), N'Thành viên đã nộp tiền mặt khắc phục tại quầy'),
(N'REC-FINE-20260825-004', 8, NULL, 2, 20000, N'CASH', N'Phạt viết bút chì ghi chú vào ruột sách Lịch sử', DATEADD(day, -10, GETDATE()), N'Thành viên đã tẩy xóa và nộp phạt tại quầy'),
(N'REC-FINE-20260905-005', 25, 32, 2, 50000, N'DEPOSIT_DEDUCTION', N'Phạt quá hạn 10 ngày mượn sách Triết học', DATEADD(day, -5, GETDATE()), N'Ghi nhận công nợ và tạm khóa thẻ'),
(N'REC-FINE-20260912-006', 7, NULL, 2, 30000, N'CASH', N'Phạt làm bẩn ố nhẹ mép sách Kỹ năng mềm', DATEADD(day, -2, GETDATE()), N'Thành viên nộp tiền mặt');
GO

-- 6.14. Chèn Danh Sách Yêu Cầu Cổng Thành Viên Tự Phục Vụ (ReaderRequests)
INSERT INTO dbo.ReaderRequests 
(RequestCode, MemberId, RequestType, Status, Amount, PayoutMethod, BankName, BankAccountNumber, BankAccountHolder, Reason, RequestDate, ProcessedByUserId, ProcessedDate, StaffNotes) 
VALUES
-- 1. Yêu cầu hủy thẻ & rút cọc (CANCEL_CARD)
(N'RQ202609-001', 2, N'CANCEL_CARD', N'PENDING', 500000, N'BANK_TRANSFER', N'Vietcombank', N'0011004567890', N'TRAN THI MAI ANH', N'Tôi chuẩn bị chuyển công tác sang TP.HCM nên xin đóng thẻ và rút lại toàn bộ số dư tiền cọc.', DATEADD(hour, -6, GETDATE()), NULL, NULL, NULL),
(N'RQ202608-002', 9, N'CANCEL_CARD', N'APPROVED', 200000, N'BANK_TRANSFER', N'MB Bank', N'0911778809001', N'HOANG VAN TUAN', N'Thành viên chuyển nơi định cư, xin thanh lý thẻ và nhận lại tiền cọc.', DATEADD(day, -36, GETDATE()), 1, DATEADD(day, -35, GETDATE()), N'Đã đối soát 0 nợ, 0 sách mượn. Chuyển khoản hoàn cọc thành công.'),
(N'RQ202607-003', 20, N'CANCEL_CARD', N'APPROVED', 500000, N'BANK_TRANSFER', N'Techcombank', N'19034567890012', N'PHAN THANH BINH', N'Đã hoàn tất dự án nghiên cứu thị trường độc lập, xin tất toán hợp đồng và rút cọc.', DATEADD(day, -56, GETDATE()), 1, DATEADD(day, -55, GETDATE()), N'Đã hoàn cọc chuyển khoản qua Techcombank.'),

-- 2. Yêu cầu báo mất thẻ & xin cấp lại phôi thẻ mới (REISSUE_CARD)
(N'RQ202609-004', 10, N'REISSUE_CARD', N'PENDING', 50000, N'CASH', NULL, NULL, NULL, N'Tôi để quên thẻ thành viên trên xe taxi, xin cấp lại phôi thẻ mới và bảo lưu thông tin tài khoản.', DATEADD(hour, -3, GETDATE()), NULL, NULL, NULL),
(N'RQ202609-005', 4, N'REISSUE_CARD', N'APPROVED', 50000, N'CASH', NULL, NULL, NULL, N'Thẻ bị gãy chip từ không quét được qua cổng kiểm soát ra vào.', DATEADD(day, -5, GETDATE()), 2, DATEADD(day, -4, GETDATE()), N'Đã in phôi thẻ mới và trao tại quầy dịch vụ thư viện.'),

-- 3. Yêu cầu cập nhật thông tin cá nhân & CCCD (UPDATE_INFO)
(N'RQ202609-006', 1, N'UPDATE_INFO', N'APPROVED', 0, N'BANK_TRANSFER', NULL, NULL, NULL, N'Cập nhật địa chỉ thường trú mới tại Hoàng Mai và số điện thoại liên lạc bổ sung.', DATEADD(day, -3, GETDATE()), 2, DATEADD(day, -2, GETDATE()), N'Đã xác minh và cập nhật vào hồ sơ thành viên.'),
(N'RQ202609-007', 13, N'UPDATE_INFO', N'PENDING', 0, N'BANK_TRANSFER', NULL, NULL, NULL, N'Tôi vừa đổi sang thẻ Căn cước mới gắn chip, xin cập nhật số định danh.', DATEADD(hour, -12, GETDATE()), NULL, NULL, NULL),

-- 4. Yêu cầu xin gia hạn lượt mượn sách trực tuyến (BORROW_EXTEND)
(N'RQ202609-008', 11, N'BORROW_EXTEND', N'APPROVED', 0, N'BANK_TRANSFER', NULL, NULL, NULL, N'Thành viên xin gia hạn thêm 7 ngày cuốn Clean Architecture để hoàn thiện tài liệu dự án.', DATEADD(day, -5, GETDATE()), 1, DATEADD(day, -4, GETDATE()), N'Duyệt gia hạn thành công thêm 7 ngày (Phiếu PM202609-023).'),
(N'RQ202609-009', 3, N'BORROW_EXTEND', N'REJECTED', 0, N'BANK_TRANSFER', NULL, NULL, NULL, N'Xin gia hạn phiếu mượn sách quá hạn.', DATEADD(day, -6, GETDATE()), 2, DATEADD(day, -5, GETDATE()), N'Từ chối do phiếu mượn đã quá hạn quy định. Thành viên vui lòng mang sách tới quầy để trả và nộp phạt trễ hạn.'),

-- 5. Yêu cầu góp ý & Đề xuất bổ sung đầu sách mới (FEEDBACK_INQUIRY)
(N'RQ202609-010', 5, N'FEEDBACK_INQUIRY', N'APPROVED', 0, N'BANK_TRANSFER', NULL, NULL, NULL, N'Kính đề xuất không gian thư viện bổ sung thêm đầu sách chuyên môn: Kubernetes in Action và Microservices Patterns.', DATEADD(day, -8, GETDATE()), 1, DATEADD(day, -6, GETDATE()), N'Đã chuyển danh mục đề xuất sách cho Ban Thư viện phê duyệt mua sắm Quý 4.'),
(N'RQ202609-011', 19, N'FEEDBACK_INQUIRY', N'PENDING', 0, N'BANK_TRANSFER', NULL, NULL, NULL, N'Đề xuất thư viện bổ sung thêm ổ cắm điện và khu vực co-working yên tĩnh tại Tầng 3.', DATEADD(day, -1, GETDATE()), NULL, NULL, NULL);
GO

-- 6.15. Cập Nhật Đồng Bộ Trạng Thái BookCopies và Số Lượng Sách (Books)
-- 1. Chuyển các bản sao đang nằm trong phiếu mượn hoạt động (BORROWING / OVERDUE) thành 'BORROWED'
UPDATE bc
SET bc.Status = N'BORROWED'
FROM dbo.BookCopies bc
INNER JOIN dbo.BorrowSlipDetails bsd ON bc.CopyId = bsd.CopyId
INNER JOIN dbo.BorrowSlips bs ON bsd.BorrowSlipId = bs.BorrowSlipId
WHERE bs.Status IN (N'BORROWING', N'OVERDUE');

-- 2. Đảm bảo các bản sao không bị mượn giữ trạng thái 'AVAILABLE'
UPDATE bc
SET bc.Status = N'AVAILABLE'
FROM dbo.BookCopies bc
WHERE bc.CopyId NOT IN (
    SELECT bsd.CopyId 
    FROM dbo.BorrowSlipDetails bsd
    INNER JOIN dbo.BorrowSlips bs ON bsd.BorrowSlipId = bs.BorrowSlipId
    WHERE bs.Status IN (N'BORROWING', N'OVERDUE')
);

-- 3. Cập nhật số lượng sẵn có AvailableQuantity thực tế trong bảng Books
UPDATE b
SET b.AvailableQuantity = (
    SELECT COUNT(*) 
    FROM dbo.BookCopies bc 
    WHERE bc.BookId = b.BookId AND bc.Status = N'AVAILABLE'
)
FROM dbo.Books b;
GO


