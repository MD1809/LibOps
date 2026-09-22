using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using LibOps.BusinessLogicLayer.BusinessValidators;
using LibOps.CommonUtilities.Payment;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataAccessLayer.DataRepositories;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;

namespace LibOps.BusinessLogicLayer.BusinessServices
{
    /// <summary>
    /// Dịch vụ nghiệp vụ chính cho Cổng thông tin tự phục vụ của Độc giả (Reader Portal)
    /// </summary>
    public class ReaderPortalService
    {
        private readonly MemberRepository _memberRepo;
        private readonly BookRepository _bookRepo;
        private readonly BorrowSlipRepository _slipRepo;
        private readonly FineReceiptRepository _fineRepo;
        private readonly DepositRepository _depositRepo;
        private readonly ServiceFeeRepository _serviceFeeRepo;
        private readonly ReaderRequestRepository _requestRepo;
        private readonly SystemSettingRepository _settingRepo;
        private readonly ReaderActionValidator _validator;

        public const decimal DEFAULT_ANNUAL_FEE = 50000m;
        public const int DEFAULT_RENEWAL_DAYS = 7;
        public const int DEFAULT_MAX_RENEWAL = 1;

        public ReaderPortalService()
        {
            _memberRepo = new MemberRepository();
            _bookRepo = new BookRepository();
            _slipRepo = new BorrowSlipRepository();
            _fineRepo = new FineReceiptRepository();
            _depositRepo = new DepositRepository();
            _serviceFeeRepo = new ServiceFeeRepository();
            _requestRepo = new ReaderRequestRepository();
            _settingRepo = new SystemSettingRepository();
            _validator = new ReaderActionValidator(_memberRepo, _slipRepo, _requestRepo);
        }

        public ReaderPortalService(
            MemberRepository memberRepo,
            BookRepository bookRepo,
            BorrowSlipRepository slipRepo,
            FineReceiptRepository fineRepo,
            DepositRepository depositRepo,
            ReaderRequestRepository requestRepo,
            SystemSettingRepository settingRepo,
            ReaderActionValidator validator)
        {
            _memberRepo = memberRepo;
            _bookRepo = bookRepo;
            _slipRepo = slipRepo;
            _fineRepo = fineRepo;
            _depositRepo = depositRepo;
            _serviceFeeRepo = new ServiceFeeRepository();
            _requestRepo = requestRepo;
            _settingRepo = settingRepo;
            _validator = validator ?? new ReaderActionValidator(memberRepo, slipRepo, requestRepo);
        }

        #region 1. Tra cứu & Khám phá sách

        public virtual List<ReaderBookSearchDto> SearchBooks(string keyword = null, int? categoryId = null, bool onlyAvailable = false)
        {
            var list = new List<ReaderBookSearchDto>();
            string sql = @"
                SELECT b.BookId, b.Title, a.AuthorName, b.CategoryId, c.CategoryName, p.PublisherName, 
                       b.PublishYear, b.Isbn, b.Price, b.ShelfLocation, b.Summary, 
                       b.TotalQuantity, b.AvailableQuantity, b.CoverImagePath,
                       (SELECT COUNT(*) FROM dbo.BorrowSlipDetails d INNER JOIN dbo.BookCopies cp ON d.CopyId = cp.CopyId WHERE cp.BookId = b.BookId) AS BorrowCount
                FROM dbo.Books b
                LEFT JOIN dbo.Authors a ON b.AuthorId = a.AuthorId
                LEFT JOIN dbo.Categories c ON b.CategoryId = c.CategoryId
                LEFT JOIN dbo.Publishers p ON b.PublisherId = p.PublisherId
                WHERE (@Keyword IS NULL OR b.Title LIKE @SearchPattern OR a.AuthorName LIKE @SearchPattern OR b.Isbn LIKE @SearchPattern OR p.PublisherName LIKE @SearchPattern)
                  AND (@CategoryId IS NULL OR b.CategoryId = @CategoryId)
                  AND (@OnlyAvailable = 0 OR b.AvailableQuantity > 0)
                ORDER BY b.AvailableQuantity DESC, b.BookId DESC";

            var parameters = new[]
            {
                new SqlParameter("@Keyword", SqlDbType.NVarChar, 100) { Value = string.IsNullOrWhiteSpace(keyword) ? (object)DBNull.Value : keyword.Trim() },
                new SqlParameter("@SearchPattern", SqlDbType.NVarChar, 102) { Value = string.IsNullOrWhiteSpace(keyword) ? (object)DBNull.Value : "%" + keyword.Trim() + "%" },
                new SqlParameter("@CategoryId", SqlDbType.Int) { Value = categoryId.HasValue && categoryId.Value > 0 ? (object)categoryId.Value : DBNull.Value },
                new SqlParameter("@OnlyAvailable", SqlDbType.Bit) { Value = onlyAvailable }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new ReaderBookSearchDto
                {
                    BookId = Convert.ToInt32(row["BookId"]),
                    Title = row["Title"].ToString(),
                    AuthorName = row["AuthorName"] != DBNull.Value ? row["AuthorName"].ToString() : "Nhiều tác giả",
                    CategoryId = row["CategoryId"] != DBNull.Value ? Convert.ToInt32(row["CategoryId"]) : 0,
                    CategoryName = row["CategoryName"] != DBNull.Value ? row["CategoryName"].ToString() : "Chung",
                    PublisherName = row["PublisherName"] != DBNull.Value ? row["PublisherName"].ToString() : string.Empty,
                    PublishYear = row["PublishYear"] != DBNull.Value ? (int?)Convert.ToInt32(row["PublishYear"]) : null,
                    Isbn = row["Isbn"] != DBNull.Value ? row["Isbn"].ToString() : string.Empty,
                    Price = Convert.ToDecimal(row["Price"]),
                    ShelfLocation = row["ShelfLocation"] != DBNull.Value ? row["ShelfLocation"].ToString() : "Kệ chung",
                    Summary = row["Summary"] != DBNull.Value ? row["Summary"].ToString() : string.Empty,
                    TotalQuantity = Convert.ToInt32(row["TotalQuantity"]),
                    AvailableQuantity = Convert.ToInt32(row["AvailableQuantity"]),
                    CoverImagePath = row["CoverImagePath"] != DBNull.Value ? row["CoverImagePath"].ToString() : string.Empty,
                    BorrowCount = row["BorrowCount"] != DBNull.Value ? Convert.ToInt32(row["BorrowCount"]) : 0
                });
            }

            return list;
        }

        /// <summary>
        /// Lấy Top sách được mượn nhiều nhất cho Trang Chủ Discovery
        /// </summary>
        public virtual List<ReaderBookSearchDto> GetTopTrendingBooks(int top = 6)
        {
            var list = new List<ReaderBookSearchDto>();
            string sql = $@"
                SELECT TOP ({top})
                    b.BookId, b.Title, a.AuthorName, b.CategoryId, c.CategoryName, p.PublisherName, 
                    b.PublishYear, b.Isbn, b.Price, b.ShelfLocation, b.Summary, 
                    b.TotalQuantity, b.AvailableQuantity, b.CoverImagePath,
                    COUNT(d.DetailId) AS BorrowCount
                FROM dbo.Books b
                LEFT JOIN dbo.Authors a ON b.AuthorId = a.AuthorId
                LEFT JOIN dbo.Categories c ON b.CategoryId = c.CategoryId
                LEFT JOIN dbo.Publishers p ON b.PublisherId = p.PublisherId
                LEFT JOIN dbo.BookCopies cp ON b.BookId = cp.BookId
                LEFT JOIN dbo.BorrowSlipDetails d ON cp.CopyId = d.CopyId
                GROUP BY b.BookId, b.Title, a.AuthorName, b.CategoryId, c.CategoryName, p.PublisherName, 
                         b.PublishYear, b.Isbn, b.Price, b.ShelfLocation, b.Summary, 
                         b.TotalQuantity, b.AvailableQuantity, b.CoverImagePath
                ORDER BY BorrowCount DESC, b.AvailableQuantity DESC, b.BookId DESC";

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
            int rank = 1;
            foreach (DataRow row in table.Rows)
            {
                list.Add(new ReaderBookSearchDto
                {
                    Rank = rank++,
                    BookId = Convert.ToInt32(row["BookId"]),
                    Title = row["Title"].ToString(),
                    AuthorName = row["AuthorName"] != DBNull.Value ? row["AuthorName"].ToString() : "Nhiều tác giả",
                    CategoryId = row["CategoryId"] != DBNull.Value ? Convert.ToInt32(row["CategoryId"]) : 0,
                    CategoryName = row["CategoryName"] != DBNull.Value ? row["CategoryName"].ToString() : "Chung",
                    PublisherName = row["PublisherName"] != DBNull.Value ? row["PublisherName"].ToString() : string.Empty,
                    PublishYear = row["PublishYear"] != DBNull.Value ? (int?)Convert.ToInt32(row["PublishYear"]) : null,
                    Isbn = row["Isbn"] != DBNull.Value ? row["Isbn"].ToString() : string.Empty,
                    Price = Convert.ToDecimal(row["Price"]),
                    ShelfLocation = row["ShelfLocation"] != DBNull.Value ? row["ShelfLocation"].ToString() : "Kệ chung",
                    Summary = row["Summary"] != DBNull.Value ? row["Summary"].ToString() : string.Empty,
                    TotalQuantity = Convert.ToInt32(row["TotalQuantity"]),
                    AvailableQuantity = Convert.ToInt32(row["AvailableQuantity"]),
                    CoverImagePath = row["CoverImagePath"] != DBNull.Value ? row["CoverImagePath"].ToString() : string.Empty,
                    BorrowCount = row["BorrowCount"] != DBNull.Value ? Convert.ToInt32(row["BorrowCount"]) : 0
                });
            }
            return list;
        }

        /// <summary>
        /// Lấy danh sách sách mới nhập về kho
        /// </summary>
        public virtual List<ReaderBookSearchDto> GetNewArrivalBooks(int top = 6)
        {
            var list = new List<ReaderBookSearchDto>();
            string sql = $@"
                SELECT TOP ({top})
                    b.BookId, b.Title, a.AuthorName, b.CategoryId, c.CategoryName, p.PublisherName, 
                    b.PublishYear, b.Isbn, b.Price, b.ShelfLocation, b.Summary, 
                    b.TotalQuantity, b.AvailableQuantity, b.CoverImagePath,
                    (SELECT COUNT(*) FROM dbo.BorrowSlipDetails d INNER JOIN dbo.BookCopies cp ON d.CopyId = cp.CopyId WHERE cp.BookId = b.BookId) AS BorrowCount
                FROM dbo.Books b
                LEFT JOIN dbo.Authors a ON b.AuthorId = a.AuthorId
                LEFT JOIN dbo.Categories c ON b.CategoryId = c.CategoryId
                LEFT JOIN dbo.Publishers p ON b.PublisherId = p.PublisherId
                ORDER BY b.BookId DESC";

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new ReaderBookSearchDto
                {
                    BookId = Convert.ToInt32(row["BookId"]),
                    Title = row["Title"].ToString(),
                    AuthorName = row["AuthorName"] != DBNull.Value ? row["AuthorName"].ToString() : "Nhiều tác giả",
                    CategoryId = row["CategoryId"] != DBNull.Value ? Convert.ToInt32(row["CategoryId"]) : 0,
                    CategoryName = row["CategoryName"] != DBNull.Value ? row["CategoryName"].ToString() : "Chung",
                    PublisherName = row["PublisherName"] != DBNull.Value ? row["PublisherName"].ToString() : string.Empty,
                    PublishYear = row["PublishYear"] != DBNull.Value ? (int?)Convert.ToInt32(row["PublishYear"]) : null,
                    Isbn = row["Isbn"] != DBNull.Value ? row["Isbn"].ToString() : string.Empty,
                    Price = Convert.ToDecimal(row["Price"]),
                    ShelfLocation = row["ShelfLocation"] != DBNull.Value ? row["ShelfLocation"].ToString() : "Kệ chung",
                    Summary = row["Summary"] != DBNull.Value ? row["Summary"].ToString() : string.Empty,
                    TotalQuantity = Convert.ToInt32(row["TotalQuantity"]),
                    AvailableQuantity = Convert.ToInt32(row["AvailableQuantity"]),
                    CoverImagePath = row["CoverImagePath"] != DBNull.Value ? row["CoverImagePath"].ToString() : string.Empty,
                    BorrowCount = row["BorrowCount"] != DBNull.Value ? Convert.ToInt32(row["BorrowCount"]) : 0
                });
            }
            return list;
        }

        /// <summary>
        /// Lấy danh sách thể loại kèm số lượng đầu sách thực tế
        /// </summary>
        public virtual List<CategoryCountDto> GetCategoryWithCounts()
        {
            var list = new List<CategoryCountDto>();
            string sql = @"
                SELECT c.CategoryId, c.CategoryName, COUNT(b.BookId) AS BookCount
                FROM dbo.Categories c
                LEFT JOIN dbo.Books b ON c.CategoryId = b.CategoryId
                GROUP BY c.CategoryId, c.CategoryName
                ORDER BY BookCount DESC, c.CategoryName ASC";

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new CategoryCountDto
                {
                    CategoryId = Convert.ToInt32(row["CategoryId"]),
                    CategoryName = row["CategoryName"].ToString(),
                    BookCount = Convert.ToInt32(row["BookCount"])
                });
            }
            return list;
        }

        #endregion

        #region 2. Sách đang mượn & Tự gia hạn mượn

        public virtual List<ReaderLoanDisplayDto> GetActiveLoans(int memberId)
        {
            var list = new List<ReaderLoanDisplayDto>();
            int maxRenewal = GetMaxRenewalCount();

            string sql = @"
                SELECT s.BorrowSlipId, d.DetailId, s.SlipCode, c.Barcode, b.Title AS BookTitle, 
                       cat.CategoryName, b.ShelfLocation, s.BorrowDate, s.DueDate, ISNULL(s.RenewalCount, 0) AS RenewalCount
                FROM dbo.BorrowSlipDetails d
                INNER JOIN dbo.BorrowSlips s ON d.BorrowSlipId = s.BorrowSlipId
                INNER JOIN dbo.BookCopies c ON d.CopyId = c.CopyId
                INNER JOIN dbo.Books b ON c.BookId = b.BookId
                LEFT JOIN dbo.Categories cat ON b.CategoryId = cat.CategoryId
                WHERE s.MemberId = @MemberId 
                  AND NOT EXISTS (SELECT 1 FROM dbo.ReturnSlipDetails r WHERE r.BorrowSlipDetailId = d.DetailId)
                ORDER BY s.DueDate ASC";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            DateTime today = DateTime.Today;

            foreach (DataRow row in table.Rows)
            {
                DateTime dueDate = Convert.ToDateTime(row["DueDate"]);
                int daysRemaining = (dueDate.Date - today).Days;
                bool isOverdue = daysRemaining < 0;

                list.Add(new ReaderLoanDisplayDto
                {
                    BorrowSlipId = Convert.ToInt32(row["BorrowSlipId"]),
                    DetailId = Convert.ToInt32(row["DetailId"]),
                    SlipCode = row["SlipCode"].ToString(),
                    Barcode = row["Barcode"].ToString(),
                    BookTitle = row["BookTitle"].ToString(),
                    CategoryName = row["CategoryName"] != DBNull.Value ? row["CategoryName"].ToString() : string.Empty,
                    ShelfLocation = row["ShelfLocation"] != DBNull.Value ? row["ShelfLocation"].ToString() : string.Empty,
                    BorrowDate = Convert.ToDateTime(row["BorrowDate"]),
                    DueDate = dueDate,
                    RenewalCount = Convert.ToInt32(row["RenewalCount"]),
                    MaxRenewalCount = maxRenewal,
                    DaysRemaining = daysRemaining,
                    IsOverdue = isOverdue
                });
            }

            return list;
        }

        public virtual bool RenewLoan(int borrowSlipId, int memberId, out DateTime newDueDate, out string errorMessage)
        {
            newDueDate = DateTime.MinValue;
            errorMessage = string.Empty;

            int maxRenewal = GetMaxRenewalCount();
            int renewalDays = GetRenewalDays();

            if (!_validator.ValidateLoanRenewal(borrowSlipId, memberId, maxRenewal, out errorMessage))
            {
                return false;
            }

            var slip = _slipRepo.GetBorrowSlipById(borrowSlipId);
            if (slip == null)
            {
                errorMessage = "Không tìm thấy phiếu mượn.";
                return false;
            }

            DateTime calculatedDueDate = slip.DueDate.AddDays(renewalDays);
            int newRenewalCount = slip.RenewalCount + 1;
            string updatedNotes = string.IsNullOrWhiteSpace(slip.Notes) 
                ? $"[Độc giả tự gia hạn lần {newRenewalCount} lúc {DateTime.Now:dd/MM/yyyy HH:mm} +{renewalDays} ngày]"
                : $"{slip.Notes} | [Gia hạn L{newRenewalCount} lúc {DateTime.Now:dd/MM/yyyy HH:mm}]";

            string sql = @"
                UPDATE dbo.BorrowSlips 
                SET DueDate = @DueDate, RenewalCount = @RenewalCount, Notes = @Notes
                WHERE BorrowSlipId = @BorrowSlipId";

            var parameters = new[]
            {
                new SqlParameter("@DueDate", SqlDbType.DateTime) { Value = calculatedDueDate },
                new SqlParameter("@RenewalCount", SqlDbType.Int) { Value = newRenewalCount },
                new SqlParameter("@Notes", SqlDbType.NVarChar, 250) { Value = updatedNotes },
                new SqlParameter("@BorrowSlipId", SqlDbType.Int) { Value = borrowSlipId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters);
            if (rows > 0)
            {
                newDueDate = calculatedDueDate;
                return true;
            }

            errorMessage = "Cập nhật dữ liệu gia hạn thất bại.";
            return false;
        }

        #endregion

        #region 3. Lịch sử mượn trả & Vi phạm phạt

        public virtual List<ReaderBorrowHistoryDto> GetCompletedReturnHistory(int memberId)
        {
            var list = new List<ReaderBorrowHistoryDto>();
            try
            {
                string sql = @"
                    SELECT d.DetailId, s.SlipCode, s.BorrowDate, s.DueDate, c.Barcode, b.Title AS BookTitle,
                           b.Price, r.ActualReturnDate, r.OverdueDays, r.FineAmount, r.BookCopyStatusAfterReturn,
                           u.FullName AS ReceivedByStaffName, r.ReturnConditionNote
                    FROM dbo.ReturnSlipDetails r
                    INNER JOIN dbo.BorrowSlipDetails d ON r.BorrowSlipDetailId = d.DetailId
                    INNER JOIN dbo.BorrowSlips s ON d.BorrowSlipId = s.BorrowSlipId
                    INNER JOIN dbo.BookCopies c ON d.CopyId = c.CopyId
                    INNER JOIN dbo.Books b ON c.BookId = b.BookId
                    LEFT JOIN dbo.UserAccounts u ON r.ReceivedByUserId = u.UserId
                    WHERE s.MemberId = @MemberId
                    ORDER BY r.ActualReturnDate DESC";

                var parameters = new[]
                {
                    new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
                };

                DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
                if (table != null)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        list.Add(new ReaderBorrowHistoryDto
                        {
                            DetailId = row["DetailId"] != DBNull.Value ? Convert.ToInt32(row["DetailId"]) : 0,
                            SlipCode = row["SlipCode"] != DBNull.Value ? row["SlipCode"].ToString() : string.Empty,
                            BorrowDate = row["BorrowDate"] != DBNull.Value ? Convert.ToDateTime(row["BorrowDate"]) : DateTime.MinValue,
                            DueDate = row["DueDate"] != DBNull.Value ? Convert.ToDateTime(row["DueDate"]) : DateTime.MinValue,
                            Barcode = row["Barcode"] != DBNull.Value ? row["Barcode"].ToString() : string.Empty,
                            BookTitle = row["BookTitle"] != DBNull.Value ? row["BookTitle"].ToString() : string.Empty,
                            Price = row["Price"] != DBNull.Value ? Convert.ToDecimal(row["Price"]) : 0m,
                            ActualReturnDate = row["ActualReturnDate"] != DBNull.Value ? Convert.ToDateTime(row["ActualReturnDate"]) : DateTime.Now,
                            OverdueDays = row["OverdueDays"] != DBNull.Value ? Convert.ToInt32(row["OverdueDays"]) : 0,
                            FineAmount = row["FineAmount"] != DBNull.Value ? Convert.ToDecimal(row["FineAmount"]) : 0m,
                            BookCopyStatusAfterReturn = row["BookCopyStatusAfterReturn"] != DBNull.Value ? row["BookCopyStatusAfterReturn"].ToString() : "AVAILABLE",
                            ReceivedByStaffName = row["ReceivedByStaffName"] != DBNull.Value ? row["ReceivedByStaffName"].ToString() : "Thủ thư",
                            ReturnConditionNote = row["ReturnConditionNote"] != DBNull.Value ? row["ReturnConditionNote"].ToString() : string.Empty
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetCompletedReturnHistory Error]: {ex.Message}");
            }

            return list;
        }

        public virtual List<ReaderFineHistoryDto> GetFineHistory(int memberId)
        {
            var list = new List<ReaderFineHistoryDto>();
            string sql = @"
                SELECT f.ReceiptId, f.ReceiptCode, ISNULL(s.SlipCode, N'--') AS SlipCode, 
                       f.PaymentDate, f.TotalAmount, f.PaymentMethod, f.Reason, 
                       u.FullName AS StaffName, f.Notes
                FROM dbo.FineReceipts f
                LEFT JOIN dbo.BorrowSlips s ON f.BorrowSlipId = s.BorrowSlipId
                LEFT JOIN dbo.UserAccounts u ON f.CollectedByUserId = u.UserId
                WHERE f.MemberId = @MemberId
                ORDER BY f.PaymentDate DESC";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new ReaderFineHistoryDto
                {
                    ReceiptId = Convert.ToInt32(row["ReceiptId"]),
                    ReceiptCode = row["ReceiptCode"].ToString(),
                    SlipCode = row["SlipCode"].ToString(),
                    PaymentDate = Convert.ToDateTime(row["PaymentDate"]),
                    TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                    PaymentMethod = row["PaymentMethod"].ToString(),
                    Reason = row["Reason"].ToString(),
                    CollectedByStaffName = row["StaffName"] != DBNull.Value ? row["StaffName"].ToString() : "Thủ thư",
                    Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
                });
            }

            return list;
        }

        /// <summary>
        /// Lấy toàn bộ lịch sử biên lai dòng tiền của độc giả (Nạp tiền, Phí gia hạn, Nộp phạt, Nợ, Hoàn cọc)
        /// </summary>
        public virtual List<ReaderCashFlowDisplayDto> GetMemberAllCashFlowHistory(int memberId)
        {
            var list = new List<ReaderCashFlowDisplayDto>();
            try
            {
                string sqlDeposit = @"
                    SELECT t.TransactionId, t.ReceiptCode, t.TransactionType, t.Amount, t.BalanceAfter, 
                           t.TransactionDate, ISNULL(u.FullName, N'Hệ thống') AS StaffName, t.Notes
                    FROM dbo.DepositTransactions t
                    LEFT JOIN dbo.UserAccounts u ON t.HandledByUserId = u.UserId
                    WHERE t.MemberId = @MemberId";

                string sqlCashFine = @"
                    SELECT f.ReceiptId AS TransactionId, f.ReceiptCode, 'CASH_FINE' AS TransactionType, 
                           f.TotalAmount AS Amount, NULL AS BalanceAfter, f.PaymentDate AS TransactionDate, 
                           ISNULL(u.FullName, N'Thủ thư') AS StaffName, f.Reason AS Notes
                    FROM dbo.FineReceipts f
                    LEFT JOIN dbo.UserAccounts u ON f.CollectedByUserId = u.UserId
                    WHERE f.MemberId = @MemberId AND f.PaymentMethod = 'CASH'";

                string sqlServiceFee = @"
                    SELECT s.ReceiptId AS TransactionId, s.ReceiptCode, 
                           CASE WHEN s.FeeType = 'CARD_ISSUANCE' THEN 'CARD_ISSUANCE_FEE' ELSE 'ANNUAL_FEE' END AS TransactionType, 
                           s.Amount AS Amount, NULL AS BalanceAfter, s.PaymentDate AS TransactionDate, 
                           ISNULL(u.FullName, N'Thủ thư') AS StaffName, ISNULL(s.Notes, N'Phí dịch vụ') AS Notes
                    FROM dbo.ServiceFeeReceipts s
                    LEFT JOIN dbo.UserAccounts u ON s.CollectedByUserId = u.UserId
                    WHERE s.MemberId = @MemberId AND s.PaymentMethod != 'DEPOSIT_DEDUCTION'";

                string sql = $"{sqlDeposit} UNION ALL {sqlCashFine} UNION ALL {sqlServiceFee} ORDER BY TransactionDate DESC, TransactionId DESC";

                var parameters = new[]
                {
                    new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
                };

                DataTable dt = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string transType = row["TransactionType"].ToString();
                        decimal rawAmount = Convert.ToDecimal(row["Amount"]);
                        decimal? balAfter = row["BalanceAfter"] != DBNull.Value ? (decimal?)Convert.ToDecimal(row["BalanceAfter"]) : null;

                        string displayType;
                        bool isInflow;
                        decimal finalAmount;

                        switch (transType)
                        {
                            case "INITIAL_DEPOSIT":
                                displayType = "Nộp cọc mở thẻ";
                                isInflow = true;
                                finalAmount = Math.Abs(rawAmount);
                                break;
                            case "TOP_UP":
                            case "TOP_UP_DEPOSIT":
                                displayType = "Nạp tiền vào thẻ (QR)";
                                isInflow = true;
                                finalAmount = Math.Abs(rawAmount);
                                break;
                            case "DEBT_PAYMENT":
                                displayType = "Nạp thanh toán nợ";
                                isInflow = true;
                                finalAmount = Math.Abs(rawAmount);
                                break;
                            case "FINE_DEDUCTION":
                                displayType = "Khấu trừ nộp phạt";
                                isInflow = false;
                                finalAmount = -Math.Abs(rawAmount);
                                break;
                            case "FINE_DEBT":
                                displayType = "Ghi nhận nợ phạt";
                                isInflow = false;
                                finalAmount = -Math.Abs(rawAmount);
                                break;
                            case "ANNUAL_FEE_DEDUCTION":
                                displayType = "Phí gia hạn thẻ";
                                isInflow = false;
                                finalAmount = -Math.Abs(rawAmount);
                                break;
                            case "CARD_CANCEL_REFUND":
                            case "REFUND":
                                displayType = "Hoàn trả tiền cọc";
                                isInflow = false;
                                finalAmount = -Math.Abs(rawAmount);
                                break;
                            case "CASH_FINE":
                                displayType = "Nộp phạt trực tiếp";
                                isInflow = false;
                                finalAmount = -Math.Abs(rawAmount);
                                break;
                            default:
                                displayType = transType;
                                isInflow = rawAmount >= 0;
                                finalAmount = rawAmount;
                                break;
                        }

                        list.Add(new ReaderCashFlowDisplayDto
                        {
                            TransactionId = Convert.ToInt32(row["TransactionId"]),
                            ReceiptCode = row["ReceiptCode"] != DBNull.Value ? row["ReceiptCode"].ToString() : "--",
                            TransactionDate = Convert.ToDateTime(row["TransactionDate"]),
                            TransactionType = transType,
                            TransactionTypeDisplay = displayType,
                            Amount = finalAmount,
                            IsInflow = isInflow,
                            BalanceAfter = balAfter,
                            PerformedByStaffName = row["StaffName"].ToString(),
                            Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetMemberAllCashFlowHistory Error]: {ex.Message}");
            }

            return list;
        }

        #endregion


        #region 4. Hồ sơ thẻ, Tài chính & Yêu cầu tự phục vụ

        public virtual ReaderCardSummaryDto GetCardSummary(int memberId)
        {
            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                return null;
            }

            int activeBorrow = _memberRepo.GetCurrentlyBorrowingCount(memberId);
            int overdueBorrow = _slipRepo.GetOverdueBorrowCountForMember(memberId);
            bool hasPending = _requestRepo.HasPendingRequest(memberId);

            string pendingType = string.Empty;
            if (hasPending)
            {
                var reqs = _requestRepo.GetRequestsByMemberId(memberId);
                if (reqs.Count > 0 && reqs[0].Status == "PENDING")
                {
                    pendingType = reqs[0].RequestType;
                }
            }

            return new ReaderCardSummaryDto
            {
                MemberId = member.MemberId,
                MemberCardCode = member.MemberCardCode,
                FullName = member.FullName,
                PhoneNumber = member.PhoneNumber,
                Email = member.Email,
                IdentityCardNumber = member.IdentityCardNumber,
                Address = member.Address,
                DateOfBirth = member.DateOfBirth,
                DepositBalance = member.DepositBalance,
                TotalDebt = member.TotalDebt,
                IssueDate = member.IssueDate,
                ExpiryDate = member.ExpiryDate,
                CardStatus = member.CardStatus,
                ActiveBorrowCount = activeBorrow,
                OverdueBorrowCount = overdueBorrow,
                HasPendingRequest = hasPending,
                PendingRequestType = pendingType
            };
        }

        public virtual decimal GetAnnualFee()
        {
            string val = _settingRepo.GetSettingValue("MEMBER_CARD_ANNUAL_FEE");
            if (decimal.TryParse(val, out decimal fee) && fee > 0)
            {
                return fee;
            }
            return DEFAULT_ANNUAL_FEE;
        }

        public virtual Bitmap GenerateTopUpVietQR(int memberId, decimal amount, out string transferContent)
        {
            var member = _memberRepo.GetMemberById(memberId);
            string cardCode = member != null ? member.MemberCardCode : "DG0000";
            string name = member != null ? member.FullName : "Độc giả";
            transferContent = VietQRPaymentSimulatorUtility.GeneratePaymentTransferContent(cardCode, "NAPTIEN");

            return VietQRPaymentSimulatorUtility.GenerateVietQRBitmap(
                cardCode, name, amount, transferContent, "NẠP TIỀN CỌC THẺ THƯ VIỆN"
            );
        }

        public virtual Bitmap GenerateRenewalVietQR(int memberId, decimal feeAmount, out string transferContent)
        {
            var member = _memberRepo.GetMemberById(memberId);
            string cardCode = member != null ? member.MemberCardCode : "DG0000";
            string name = member != null ? member.FullName : "Độc giả";
            transferContent = VietQRPaymentSimulatorUtility.GeneratePaymentTransferContent(cardCode, "GIAHAN");

            return VietQRPaymentSimulatorUtility.GenerateVietQRBitmap(
                cardCode, name, feeAmount, transferContent, "GIA HẠN THẺ THƯ VIỆN (PHÍ THƯỜNG NIÊN)"
            );
        }

        public virtual bool TopUpBalance(int memberId, decimal amount, string transactionNotes, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (amount <= 0)
            {
                errorMessage = "Số tiền nạp phải lớn hơn 0 VNĐ.";
                return false;
            }

            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Không tìm thấy hồ sơ độc giả.";
                return false;
            }

            // Thuật toán nạp tiền thác nước (Waterfall Top-Up):
            decimal currentDebt = member.TotalDebt;
            decimal newDebt = currentDebt;
            decimal newBalance = member.DepositBalance;
            decimal debtPaid = 0;
            decimal depositAdded = 0;

            if (currentDebt > 0)
            {
                if (amount <= currentDebt)
                {
                    debtPaid = amount;
                    newDebt = currentDebt - amount;
                }
                else
                {
                    debtPaid = currentDebt;
                    depositAdded = amount - currentDebt;
                    newDebt = 0;
                    newBalance = member.DepositBalance + depositAdded;
                }
            }
            else
            {
                depositAdded = amount;
                newBalance = member.DepositBalance + amount;
            }

            string receiptCode = $"TOP{DateTime.Now:yyyyMMddHHmmss}";
            string noteDetail;
            if (debtPaid > 0 && depositAdded > 0)
            {
                noteDetail = $"Độc giả nạp VietQR {amount:N0} đ (Khấu trừ hết nợ {debtPaid:N0} đ, nạp {depositAdded:N0} đ vào cọc)";
            }
            else if (debtPaid > 0)
            {
                noteDetail = $"Độc giả nạp VietQR {amount:N0} đ trừ nợ phạt (Nợ còn lại: {newDebt:N0} đ)";
            }
            else
            {
                noteDetail = string.IsNullOrWhiteSpace(transactionNotes) ? "Độc giả nạp tiền qua VietQR" : transactionNotes.Trim();
            }

            if (!string.IsNullOrWhiteSpace(transactionNotes) && debtPaid > 0)
            {
                noteDetail += $" | {transactionNotes.Trim()}";
            }

            using (var conn = DatabaseConnectionHelper.CreateAndOpenConnection())
            using (var trans = conn.BeginTransaction())
            {
                try
                {
                    _memberRepo.UpdateDepositAndDebt(memberId, newBalance, newDebt, trans);

                    var tx = new DepositTransactionEntity
                    {
                        MemberId = memberId,
                        HandledByUserId = 1, // System / Admin Default
                        TransactionType = debtPaid > 0 && depositAdded == 0 ? "DEBT_PAYMENT" : "TOP_UP",
                        Amount = amount,
                        BalanceAfter = newBalance,
                        ReceiptCode = receiptCode,
                        TransactionDate = DateTime.Now,
                        Notes = noteDetail
                    };
                    _depositRepo.InsertTransaction(tx, trans);

                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    errorMessage = "Lỗi khi cập nhật số dư: " + ex.Message;
                    return false;
                }
            }
        }

        public virtual bool RenewCardWithDepositDeduction(int memberId, decimal annualFee, out DateTime newExpiryDate, out string errorMessage)
        {
            newExpiryDate = DateTime.MinValue;
            errorMessage = string.Empty;

            if (!_validator.ValidateCardRenewalEligibility(memberId, annualFee, true, out errorMessage))
            {
                return false;
            }

            var member = _memberRepo.GetMemberById(memberId);
            decimal newBalance = member.DepositBalance - annualFee;
            DateTime baseDate = member.ExpiryDate < DateTime.Today ? DateTime.Today : member.ExpiryDate;
            DateTime calculatedExpiry = baseDate.AddDays(365);
            string receiptCode = $"REN{DateTime.Now:yyyyMMddHHmmss}";

            using (var conn = DatabaseConnectionHelper.CreateAndOpenConnection())
            using (var trans = conn.BeginTransaction())
            {
                try
                {
                    _memberRepo.UpdateDepositBalance(memberId, newBalance, trans);
                    _memberRepo.UpdateExpiryDate(memberId, calculatedExpiry, trans);
                    if (member.CardStatus != "ACTIVE")
                    {
                        _memberRepo.UpdateCardStatus(memberId, "ACTIVE", trans);
                    }

                    var tx = new DepositTransactionEntity
                    {
                        MemberId = memberId,
                        HandledByUserId = 1,
                        TransactionType = "ANNUAL_FEE_DEDUCTION",
                        Amount = -annualFee,
                        BalanceAfter = newBalance,
                        ReceiptCode = receiptCode,
                        TransactionDate = DateTime.Now,
                        Notes = $"Thu phí thường niên gia hạn thẻ (+365 ngày) đến {calculatedExpiry:dd/MM/yyyy}"
                    };
                    _depositRepo.InsertTransaction(tx, trans);

                    var feeReceipt = new ServiceFeeReceiptEntity
                    {
                        MemberId = memberId,
                        ReceiptCode = receiptCode,
                        FeeType = "ANNUAL_RENEWAL",
                        Amount = annualFee,
                        PaymentMethod = "DEPOSIT_DEDUCTION",
                        PaymentDate = DateTime.Now,
                        CollectedByUserId = 1,
                        Notes = $"Thu phí thường niên gia hạn thẻ (+365 ngày) đến {calculatedExpiry:dd/MM/yyyy} qua trừ ví cọc"
                    };
                    _serviceFeeRepo.InsertReceipt(feeReceipt, trans);

                    trans.Commit();
                    newExpiryDate = calculatedExpiry;
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    errorMessage = "Lỗi khi gia hạn thẻ: " + ex.Message;
                    return false;
                }
            }
        }

        public virtual bool RenewCardWithOnlinePayment(int memberId, decimal feeAmount, out DateTime newExpiryDate, out string errorMessage)
        {
            newExpiryDate = DateTime.MinValue;
            errorMessage = string.Empty;

            if (!_validator.ValidateCardRenewalEligibility(memberId, feeAmount, false, out errorMessage))
            {
                return false;
            }

            var member = _memberRepo.GetMemberById(memberId);
            DateTime baseDate = member.ExpiryDate < DateTime.Today ? DateTime.Today : member.ExpiryDate;
            DateTime calculatedExpiry = baseDate.AddDays(365);
            string receiptCode = $"REN-QR{DateTime.Now:yyyyMMddHHmmss}";

            using (var conn = DatabaseConnectionHelper.CreateAndOpenConnection())
            using (var trans = conn.BeginTransaction())
            {
                try
                {
                    _memberRepo.UpdateExpiryDate(memberId, calculatedExpiry, trans);
                    if (member.CardStatus != "ACTIVE")
                    {
                        _memberRepo.UpdateCardStatus(memberId, "ACTIVE", trans);
                    }

                    var feeReceipt = new ServiceFeeReceiptEntity
                    {
                        MemberId = memberId,
                        ReceiptCode = receiptCode,
                        FeeType = "ANNUAL_RENEWAL",
                        Amount = feeAmount,
                        PaymentMethod = "VIETQR",
                        PaymentDate = DateTime.Now,
                        CollectedByUserId = 1,
                        Notes = $"Thu phí thường niên gia hạn thẻ (+365 ngày) đến {calculatedExpiry:dd/MM/yyyy} qua thanh toán VietQR"
                    };
                    _serviceFeeRepo.InsertReceipt(feeReceipt, trans);

                    trans.Commit();
                    newExpiryDate = calculatedExpiry;
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    errorMessage = "Lỗi khi cập nhật hạn thẻ: " + ex.Message;
                    return false;
                }
            }
        }

        public virtual bool SubmitRefundRequest(
            int memberId, 
            decimal amount, 
            string payoutMethod, 
            string bankName, 
            string bankAccountNo, 
            string bankAccountHolder, 
            string reason, 
            out string requestCode, 
            out string errorMessage)
        {
            requestCode = string.Empty;
            errorMessage = "Hệ thống không hỗ trợ chức năng rút bớt tiền cọc. Tiền cọc chỉ được hoàn trả khi độc giả làm thủ tục Thanh lý & Đóng thẻ thư viện.";
            return false;
        }

        public virtual bool LockCardByReader(int memberId, out string errorMessage)
        {
            errorMessage = string.Empty;
            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Không tìm thấy hồ sơ độc giả.";
                return false;
            }

            if (member.CardStatus == "LOCKED")
            {
                errorMessage = "Thẻ của bạn hiện đã ở trạng thái Tạm khóa.";
                return false;
            }

            if (member.CardStatus == "CLOSED")
            {
                errorMessage = "Thẻ của bạn đã bị Đóng/Hủy, không thể thao tác.";
                return false;
            }

            bool ok = _memberRepo.UpdateCardStatus(memberId, "LOCKED");
            if (ok)
            {
                return true;
            }

            errorMessage = "Tạm khóa thẻ thất bại. Vui lòng thử lại sau.";
            return false;
        }

        public virtual bool SubmitClosureRequest(
            int memberId, 
            string reason, 
            out string requestCode, 
            out string errorMessage)
        {
            return SubmitClosureRequest(memberId, "NONE", string.Empty, string.Empty, string.Empty, reason, out requestCode, out errorMessage);
        }

        public virtual bool SubmitClosureRequest(
            int memberId, 
            string payoutMethod, 
            string bankName, 
            string bankAccountNo, 
            string bankAccountHolder, 
            string reason, 
            out string requestCode, 
            out string errorMessage)
        {
            requestCode = string.Empty;
            errorMessage = string.Empty;

            if (!_validator.ValidateClosureRequestEligibility(memberId, out errorMessage))
            {
                return false;
            }

            var member = _memberRepo.GetMemberById(memberId);
            decimal refundAmount = Math.Max(0, member.DepositBalance);

            int year = DateTime.Today.Year;
            int seq = _requestRepo.GetNextRequestSequenceForYear(year, "CANCEL_CARD");
            requestCode = $"REQ-CLS{year}{seq:D4}";

            var request = new ReaderRequestEntity
            {
                RequestCode = requestCode,
                MemberId = memberId,
                RequestType = "CANCEL_CARD",
                Status = "PENDING",
                Amount = refundAmount,
                PayoutMethod = string.IsNullOrWhiteSpace(payoutMethod) ? "BANK_TRANSFER" : payoutMethod,
                BankName = bankName,
                BankAccountNumber = bankAccountNo,
                BankAccountHolder = bankAccountHolder,
                Reason = reason,
                RequestDate = DateTime.Now
            };

            int id = _requestRepo.InsertRequest(request);
            if (id > 0)
            {
                return true;
            }

            errorMessage = "Gửi yêu cầu hủy thẻ thất bại.";
            return false;
        }

        public virtual bool SubmitReissueCardRequest(
            int memberId, 
            string reason, 
            out string requestCode, 
            out string errorMessage)
        {
            requestCode = string.Empty;
            errorMessage = string.Empty;

            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Không tìm thấy thông tin độc giả.";
                return false;
            }

            if (_requestRepo.HasPendingRequest(memberId, "REISSUE_CARD"))
            {
                errorMessage = "Bạn đang có một yêu cầu cấp lại thẻ đang chờ xét duyệt.";
                return false;
            }

            // Tự động tạm khóa thẻ ngay lập tức để phòng ngừa kẻ gian sử dụng
            _memberRepo.UpdateCardStatus(memberId, "LOCKED");

            int year = DateTime.Today.Year;
            int seq = _requestRepo.GetNextRequestSequenceForYear(year, "REISSUE_CARD");
            requestCode = $"REQ-RIS{year}{seq:D4}";

            var request = new ReaderRequestEntity
            {
                RequestCode = requestCode,
                MemberId = memberId,
                RequestType = "REISSUE_CARD",
                Status = "PENDING",
                Amount = 0,
                PayoutMethod = "NONE",
                Reason = string.IsNullOrWhiteSpace(reason) ? "Báo mất thẻ và xin cấp lại thẻ mới" : reason.Trim(),
                RequestDate = DateTime.Now
            };

            int id = _requestRepo.InsertRequest(request);
            if (id > 0)
            {
                return true;
            }

            errorMessage = "Gửi yêu cầu cấp lại thẻ thất bại.";
            return false;
        }

        public virtual bool SubmitUpdateInfoRequest(
            int memberId, 
            string newPhone, 
            string newEmail, 
            string newAddress, 
            string reason, 
            out string requestCode, 
            out string errorMessage)
        {
            requestCode = string.Empty;
            errorMessage = string.Empty;

            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Không tìm thấy thông tin độc giả.";
                return false;
            }

            if (_requestRepo.HasPendingRequest(memberId, "UPDATE_INFO"))
            {
                errorMessage = "Bạn đang có một yêu cầu cập nhật thông tin đang chờ xét duyệt.";
                return false;
            }

            int year = DateTime.Today.Year;
            int seq = _requestRepo.GetNextRequestSequenceForYear(year, "UPDATE_INFO");
            requestCode = $"REQ-UPD{year}{seq:D4}";

            string fullReason = $"[CẬP NHẬT] SĐT: {newPhone} | Email: {newEmail} | Địa chỉ: {newAddress}\r\nLý do: {reason}";

            var request = new ReaderRequestEntity
            {
                RequestCode = requestCode,
                MemberId = memberId,
                RequestType = "UPDATE_INFO",
                Status = "PENDING",
                Amount = 0,
                PayoutMethod = "NONE",
                Reason = fullReason,
                RequestDate = DateTime.Now
            };

            int id = _requestRepo.InsertRequest(request);
            if (id > 0)
            {
                return true;
            }

            errorMessage = "Gửi yêu cầu cập nhật thông tin thất bại.";
            return false;
        }

        public virtual bool SubmitFeedbackRequest(
            int memberId, 
            string subject, 
            string content, 
            out string requestCode, 
            out string errorMessage)
        {
            requestCode = string.Empty;
            errorMessage = string.Empty;

            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Không tìm thấy thông tin độc giả.";
                return false;
            }

            int year = DateTime.Today.Year;
            int seq = _requestRepo.GetNextRequestSequenceForYear(year, "FEEDBACK_INQUIRY");
            requestCode = $"REQ-FDB{year}{seq:D4}";

            string fullReason = $"[GÓP Ý/HỖ TRỢ] {subject}\r\nNội dung: {content}";

            var request = new ReaderRequestEntity
            {
                RequestCode = requestCode,
                MemberId = memberId,
                RequestType = "FEEDBACK_INQUIRY",
                Status = "PENDING",
                Amount = 0,
                PayoutMethod = "NONE",
                Reason = fullReason,
                RequestDate = DateTime.Now
            };

            int id = _requestRepo.InsertRequest(request);
            if (id > 0)
            {
                return true;
            }

            errorMessage = "Gửi góp ý/yêu cầu hỗ trợ thất bại.";
            return false;
        }

        public virtual bool CancelRequestByReader(int requestId, int memberId, out string errorMessage)
        {
            errorMessage = string.Empty;
            var req = _requestRepo.GetRequestById(requestId);
            if (req == null || req.MemberId != memberId)
            {
                errorMessage = "Không tìm thấy yêu cầu tương ứng của bạn.";
                return false;
            }

            if (req.Status != "PENDING")
            {
                errorMessage = $"Yêu cầu đã ở trạng thái '{req.Status}'. Không thể hủy!";
                return false;
            }

            bool ok = _requestRepo.CancelRequestByMember(requestId, memberId);
            if (ok)
            {
                return true;
            }

            errorMessage = "Hủy yêu cầu thất bại.";
            return false;
        }

        public virtual List<ReaderRequestDisplayDto> GetMyRequests(int memberId)
        {
            return _requestRepo.GetRequestsByMemberId(memberId);
        }

        #endregion

        #region Helpers

        private int GetMaxRenewalCount()
        {
            string val = _settingRepo.GetSettingValue("MAX_RENEWAL_COUNT");
            if (int.TryParse(val, out int count) && count > 0)
            {
                return count;
            }
            return DEFAULT_MAX_RENEWAL;
        }

        private int GetRenewalDays()
        {
            string val = _settingRepo.GetSettingValue("RENEWAL_DAYS");
            if (int.TryParse(val, out int days) && days > 0)
            {
                return days;
            }
            return DEFAULT_RENEWAL_DAYS;
        }

        #endregion
    }
}
