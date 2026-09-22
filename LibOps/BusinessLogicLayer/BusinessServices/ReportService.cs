using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataModels.DataTransferObjects;

namespace LibOps.BusinessLogicLayer.BusinessServices
{
    /// <summary>
    /// Dịch vụ tính toán báo cáo thống kê, các chỉ số KPI Dashboard và dữ liệu biểu đồ
    /// </summary>
    public class ReportService
    {
        /// <summary>
        /// Lấy toàn bộ 6 chỉ số KPI chính hiển thị trên Main Dashboard
        /// </summary>
        public virtual DashboardKpiSummaryDto GetDashboardKpiSummary()
        {
            var summary = new DashboardKpiSummaryDto();

            try
            {
                string sql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM dbo.Books) AS TotalBookTitles,
                        (SELECT COUNT(*) FROM dbo.BookCopies) AS TotalBookCopies,
                        (SELECT COUNT(*) FROM dbo.BookCopies WHERE Status = 'AVAILABLE') AS AvailableBooks,
                        (SELECT COUNT(*) FROM dbo.BookCopies WHERE Status = 'BORROWED') AS BorrowingBooks,
                        (SELECT COUNT(*) FROM dbo.BorrowSlips WHERE Status = 'BORROWING' AND DueDate < CAST(GETDATE() AS DATE)) AS OverdueBooks,
                        (SELECT COUNT(*) FROM dbo.Members WHERE CardStatus = 'ACTIVE') AS TotalMembers,
                        (SELECT ISNULL(SUM(DepositBalance), 0) FROM dbo.Members) AS TotalDepositBalance;";

                DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
                if (table.Rows.Count > 0)
                {
                    DataRow row = table.Rows[0];
                    summary.TotalBookTitles = Convert.ToInt32(row["TotalBookTitles"]);
                    summary.TotalBookCopies = Convert.ToInt32(row["TotalBookCopies"]);
                    summary.AvailableBooks = Convert.ToInt32(row["AvailableBooks"]);
                    summary.BorrowingBooks = Convert.ToInt32(row["BorrowingBooks"]);
                    summary.OverdueBooks = Convert.ToInt32(row["OverdueBooks"]);
                    summary.TotalMembers = Convert.ToInt32(row["TotalMembers"]);
                    summary.TotalDepositBalance = Convert.ToDecimal(row["TotalDepositBalance"]);
                }
            }
            catch
            {
                // Trả về dữ liệu mặc định 0 nếu CSDL chưa kết nối hoặc đang khởi tạo
            }

            return summary;
        }

        /// <summary>
        /// Lấy dữ liệu Xu hướng Mượn - Trả theo 7 ngày gần nhất
        /// </summary>
        public virtual List<BorrowReturnTrendItemDto> GetBorrowReturnTrend(int days = 7)
        {
            var list = new List<BorrowReturnTrendItemDto>();
            try
            {
                DateTime today = DateTime.Today;
                DateTime startDate = today.AddDays(-(days - 1));

                string sql = @"
                    SELECT 
                        CAST(s.BorrowDate AS DATE) AS LogDate,
                        COUNT(DISTINCT d.DetailId) AS BorrowCount
                    FROM dbo.BorrowSlips s
                    INNER JOIN dbo.BorrowSlipDetails d ON s.BorrowSlipId = d.BorrowSlipId
                    WHERE s.BorrowDate >= @StartDate
                    GROUP BY CAST(s.BorrowDate AS DATE)";

                string sqlReturn = @"
                    SELECT 
                        CAST(r.ActualReturnDate AS DATE) AS LogDate,
                        COUNT(DISTINCT r.ReturnDetailId) AS ReturnCount
                    FROM dbo.ReturnSlipDetails r
                    WHERE r.ActualReturnDate >= @StartDate
                    GROUP BY CAST(r.ActualReturnDate AS DATE)";

                var prms = new[] { new SqlParameter("@StartDate", SqlDbType.DateTime) { Value = startDate } };
                DataTable tblBorrow = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, prms);

                var prms2 = new[] { new SqlParameter("@StartDate", SqlDbType.DateTime) { Value = startDate } };
                DataTable tblReturn = DatabaseConnectionHelper.ExecuteQueryToDataTable(sqlReturn, prms2);

                var borrowDict = new Dictionary<DateTime, int>();
                foreach (DataRow r in tblBorrow.Rows)
                {
                    borrowDict[Convert.ToDateTime(r["LogDate"])] = Convert.ToInt32(r["BorrowCount"]);
                }

                var returnDict = new Dictionary<DateTime, int>();
                foreach (DataRow r in tblReturn.Rows)
                {
                    returnDict[Convert.ToDateTime(r["LogDate"])] = Convert.ToInt32(r["ReturnCount"]);
                }

                for (int i = 0; i < days; i++)
                {
                    DateTime dt = startDate.AddDays(i);
                    int borrows = borrowDict.ContainsKey(dt) ? borrowDict[dt] : 0;
                    int returns = returnDict.ContainsKey(dt) ? returnDict[dt] : 0;

                    list.Add(new BorrowReturnTrendItemDto
                    {
                        Date = dt,
                        DayLabel = dt.ToString("dd/MM"),
                        BorrowCount = borrows,
                        ReturnCount = returns
                    });
                }
            }
            catch
            {
                // Fallback dữ liệu rỗng
            }
            return list;
        }

        /// <summary>
        /// Lấy Top 5 đầu sách được mượn nhiều nhất
        /// </summary>
        public virtual List<TopBorrowedBookItemDto> GetTopBorrowedBooks(int top = 5)
        {
            var list = new List<TopBorrowedBookItemDto>();
            try
            {
                string sql = $@"
                    SELECT TOP ({top})
                        b.Title AS BookTitle,
                        ISNULL(c.CategoryName, N'Khác') AS CategoryName,
                        COUNT(d.DetailId) AS BorrowCount
                    FROM dbo.Books b
                    LEFT JOIN dbo.Categories c ON b.CategoryId = c.CategoryId
                    INNER JOIN dbo.BookCopies cp ON b.BookId = cp.BookId
                    INNER JOIN dbo.BorrowSlipDetails d ON cp.CopyId = d.CopyId
                    GROUP BY b.BookId, b.Title, c.CategoryName
                    ORDER BY BorrowCount DESC";

                DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
                int rank = 1;
                foreach (DataRow row in table.Rows)
                {
                    list.Add(new TopBorrowedBookItemDto
                    {
                        Rank = rank++,
                        BookTitle = row["BookTitle"].ToString(),
                        CategoryName = row["CategoryName"].ToString(),
                        BorrowCount = Convert.ToInt32(row["BorrowCount"])
                    });
                }
            }
            catch
            {
                // Fallback
            }
            return list;
        }

        /// <summary>
        /// Lấy danh sách các phiếu/cuốn sách quá hạn cần thu hồi gấp
        /// </summary>
        public virtual List<OverdueBorrowItemDto> GetOverdueBorrowList(int top = 8)
        {
            var list = new List<OverdueBorrowItemDto>();
            try
            {
                string sql = $@"
                    SELECT TOP ({top})
                        s.BorrowSlipId AS SlipId,
                        s.SlipCode,
                        m.MemberCardCode,
                        m.FullName AS MemberFullName,
                        b.Title AS BookTitle,
                        s.DueDate,
                        DATEDIFF(DAY, s.DueDate, GETDATE()) AS OverdueDays,
                        ISNULL(m.PhoneNumber, '') AS PhoneNumber
                    FROM dbo.BorrowSlips s
                    INNER JOIN dbo.Members m ON s.MemberId = m.MemberId
                    INNER JOIN dbo.BorrowSlipDetails d ON s.BorrowSlipId = d.BorrowSlipId
                    INNER JOIN dbo.BookCopies cp ON d.CopyId = cp.CopyId
                    INNER JOIN dbo.Books b ON cp.BookId = b.BookId
                    LEFT JOIN dbo.ReturnSlipDetails r ON d.DetailId = r.BorrowSlipDetailId
                    WHERE (s.Status = 'OVERDUE' OR (s.Status = 'BORROWING' AND s.DueDate < CAST(GETDATE() AS DATE))) 
                      AND r.ReturnDetailId IS NULL
                    ORDER BY OverdueDays DESC, s.DueDate ASC";

                DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
                decimal finePerDay = 5000; // Tham số phạt trễ hạn mặc định

                foreach (DataRow row in table.Rows)
                {
                    int overdueDays = Convert.ToInt32(row["OverdueDays"]);
                    list.Add(new OverdueBorrowItemDto
                    {
                        SlipId = Convert.ToInt32(row["SlipId"]),
                        SlipCode = row["SlipCode"].ToString(),
                        MemberCardCode = row["MemberCardCode"].ToString(),
                        MemberFullName = row["MemberFullName"].ToString(),
                        BookTitle = row["BookTitle"].ToString(),
                        DueDate = Convert.ToDateTime(row["DueDate"]),
                        OverdueDays = overdueDays,
                        EstimatedFine = overdueDays * finePerDay,
                        PhoneNumber = row["PhoneNumber"].ToString()
                    });
                }
            }
            catch
            {
                // Fallback
            }
            return list;
        }

        /// <summary>
        /// Lấy danh sách yêu cầu chờ duyệt từ độc giả
        /// </summary>
        public virtual List<PendingReaderRequestItemDto> GetPendingReaderRequests(int top = 5)
        {
            var list = new List<PendingReaderRequestItemDto>();
            try
            {
                string sql = $@"
                    SELECT TOP ({top})
                        r.RequestId,
                        m.MemberCardCode,
                        m.FullName AS MemberFullName,
                        r.RequestType,
                        r.RequestDate AS CreatedAt,
                        r.Status
                    FROM dbo.ReaderRequests r
                    INNER JOIN dbo.Members m ON r.MemberId = m.MemberId
                    WHERE r.Status = 'PENDING'
                    ORDER BY r.RequestDate ASC";

                DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
                foreach (DataRow row in table.Rows)
                {
                    string reqType = row["RequestType"]?.ToString() ?? string.Empty;
                    string status = row["Status"]?.ToString() ?? string.Empty;

                    list.Add(new PendingReaderRequestItemDto
                    {
                        RequestId = Convert.ToInt32(row["RequestId"]),
                        MemberCardCode = row["MemberCardCode"]?.ToString() ?? string.Empty,
                        MemberFullName = row["MemberFullName"]?.ToString() ?? string.Empty,
                        RequestType = reqType,
                        CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                        Status = status
                    });
                }
            }
            catch
            {
                // Fallback
            }
            return list;
        }

        /// <summary>
        /// Lấy nhật ký hoạt động mượn/trả vừa diễn ra (Live feed)
        /// </summary>
        public virtual List<RecentActivityLogItemDto> GetRecentActivityLog(int top = 8)
        {
            var list = new List<RecentActivityLogItemDto>();
            try
            {
                string sql = $@"
                    SELECT TOP ({top})
                        ActivityTime,
                        ActivityType,
                        ActivityIcon,
                        MemberName,
                        BookTitle,
                        HandledBy
                    FROM (
                        SELECT 
                            s.BorrowDate AS ActivityTime,
                            N'Mượn sách' AS ActivityType,
                            N'' AS ActivityIcon,
                            m.FullName AS MemberName,
                            b.Title AS BookTitle,
                            ISNULL(u.FullName, N'Thủ thư') AS HandledBy
                        FROM dbo.BorrowSlips s
                        INNER JOIN dbo.Members m ON s.MemberId = m.MemberId
                        INNER JOIN dbo.BorrowSlipDetails d ON s.BorrowSlipId = d.BorrowSlipId
                        INNER JOIN dbo.BookCopies cp ON d.CopyId = cp.CopyId
                        INNER JOIN dbo.Books b ON cp.BookId = b.BookId
                        LEFT JOIN dbo.UserAccounts u ON s.CreatedByUserId = u.UserId

                        UNION ALL

                        SELECT 
                            r.ActualReturnDate AS ActivityTime,
                            N'Trả sách' AS ActivityType,
                            N'' AS ActivityIcon,
                            m.FullName AS MemberName,
                            b.Title AS BookTitle,
                            ISNULL(u.FullName, N'Thủ thư') AS HandledBy
                        FROM dbo.ReturnSlipDetails r
                        INNER JOIN dbo.BorrowSlipDetails d ON r.BorrowSlipDetailId = d.DetailId
                        INNER JOIN dbo.BorrowSlips s ON d.BorrowSlipId = s.BorrowSlipId
                        INNER JOIN dbo.Members m ON s.MemberId = m.MemberId
                        INNER JOIN dbo.BookCopies cp ON d.CopyId = cp.CopyId
                        INNER JOIN dbo.Books b ON cp.BookId = b.BookId
                        LEFT JOIN dbo.UserAccounts u ON r.ReceivedByUserId = u.UserId
                    ) AS CombinedActivities
                    ORDER BY ActivityTime DESC";

                DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
                foreach (DataRow row in table.Rows)
                {
                    DateTime actTime = Convert.ToDateTime(row["ActivityTime"]);
                    list.Add(new RecentActivityLogItemDto
                    {
                        Timestamp = actTime,
                        FormattedTime = actTime.ToString("HH:mm dd/MM"),
                        ActivityType = row["ActivityType"].ToString(),
                        ActivityIcon = row["ActivityIcon"].ToString(),
                        MemberName = row["MemberName"].ToString(),
                        BookTitle = row["BookTitle"].ToString(),
                        HandledBy = row["HandledBy"].ToString()
                    });
                }
            }
            catch
            {
                // Fallback
            }
            return list;
        }

        /// <summary>
        /// Lấy Top 5 độc giả mượn sách nhiều nhất thư viện
        /// </summary>
        public virtual List<TopActiveMemberItemDto> GetTopActiveMembers(int top = 5)
        {
            var list = new List<TopActiveMemberItemDto>();
            try
            {
                string sql = $@"
                    SELECT TOP ({top})
                        m.MemberId,
                        m.MemberCardCode,
                        m.FullName,
                        ISNULL(m.Email, ISNULL(m.PhoneNumber, N'Chưa cập nhật')) AS EmailOrPhone,
                        COUNT(d.DetailId) AS TotalBorrowCount,
                        SUM(CASE WHEN r.ReturnDetailId IS NULL THEN 1 ELSE 0 END) AS ActiveBorrowCount
                    FROM dbo.Members m
                    INNER JOIN dbo.BorrowSlips s ON m.MemberId = s.MemberId
                    INNER JOIN dbo.BorrowSlipDetails d ON s.BorrowSlipId = d.BorrowSlipId
                    LEFT JOIN dbo.ReturnSlipDetails r ON d.DetailId = r.BorrowSlipDetailId
                    GROUP BY m.MemberId, m.MemberCardCode, m.FullName, m.Email, m.PhoneNumber
                    ORDER BY TotalBorrowCount DESC, ActiveBorrowCount DESC";

                DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
                int rank = 1;
                foreach (DataRow row in table.Rows)
                {
                    string badge = $"#{rank}";
                    list.Add(new TopActiveMemberItemDto
                    {
                        Rank = rank++,
                        RankIcon = badge,
                        MemberCardCode = row["MemberCardCode"].ToString(),
                        FullName = row["FullName"].ToString(),
                        EmailOrPhone = row["EmailOrPhone"].ToString(),
                        TotalBorrowCount = Convert.ToInt32(row["TotalBorrowCount"]),
                        ActiveBorrowCount = Convert.ToInt32(row["ActiveBorrowCount"])
                    });
                }
            }
            catch
            {
                // Fallback
            }
            return list;
        }

        /// <summary>
        /// Lấy dữ liệu Báo cáo Tồn kho Và Lưu thông
        /// </summary>
        public virtual List<InventoryReportDto> GetInventoryReport(int? categoryId = null, string shelfLocation = null)
        {
            var list = new List<InventoryReportDto>();

            string sql = @"
                SELECT 
                    b.BookId, b.ISBN, b.Title, c.CategoryName, a.AuthorName, p.PublisherName,
                    b.Price, b.ShelfLocation, b.TotalQuantity, b.AvailableQuantity,
                    (SELECT COUNT(*) FROM dbo.BookCopies cp WHERE cp.BookId = b.BookId AND cp.Status = 'BORROWED') AS BorrowedQuantity,
                    (SELECT COUNT(*) FROM dbo.BookCopies cp WHERE cp.BookId = b.BookId AND cp.Status = 'DAMAGED') AS DamagedQuantity,
                    (SELECT COUNT(*) FROM dbo.BookCopies cp WHERE cp.BookId = b.BookId AND cp.Status = 'LOST') AS LostQuantity
                FROM dbo.Books b
                LEFT JOIN dbo.Categories c ON b.CategoryId = c.CategoryId
                LEFT JOIN dbo.Authors a ON b.AuthorId = a.AuthorId
                LEFT JOIN dbo.Publishers p ON b.PublisherId = p.PublisherId
                WHERE (1=1)";

            var parameters = new List<SqlParameter>();
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                sql += " AND b.CategoryId = @CategoryId";
                parameters.Add(new SqlParameter("@CategoryId", SqlDbType.Int) { Value = categoryId.Value });
            }

            if (!string.IsNullOrWhiteSpace(shelfLocation))
            {
                sql += " AND b.ShelfLocation LIKE @ShelfLocation";
                parameters.Add(new SqlParameter("@ShelfLocation", SqlDbType.NVarChar, 100) { Value = "%" + shelfLocation.Trim() + "%" });
            }

            sql += " ORDER BY b.BookId ASC";

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters.ToArray());
            int seq = 1;
            foreach (DataRow row in table.Rows)
            {
                list.Add(new InventoryReportDto
                {
                    SequenceNumber = seq++,
                    BookId = Convert.ToInt32(row["BookId"]),
                    ISBN = row["ISBN"] != DBNull.Value ? row["ISBN"].ToString() : string.Empty,
                    Title = row["Title"].ToString(),
                    CategoryName = row["CategoryName"] != DBNull.Value ? row["CategoryName"].ToString() : "Chung",
                    AuthorName = row["AuthorName"] != DBNull.Value ? row["AuthorName"].ToString() : "Nhiều tác giả",
                    PublisherName = row["PublisherName"] != DBNull.Value ? row["PublisherName"].ToString() : "NXB",
                    Price = Convert.ToDecimal(row["Price"]),
                    ShelfLocation = row["ShelfLocation"] != DBNull.Value ? row["ShelfLocation"].ToString() : "Chưa xếp",
                    TotalQuantity = Convert.ToInt32(row["TotalQuantity"]),
                    AvailableQuantity = Convert.ToInt32(row["AvailableQuantity"]),
                    BorrowedQuantity = Convert.ToInt32(row["BorrowedQuantity"]),
                    DamagedQuantity = Convert.ToInt32(row["DamagedQuantity"]),
                    LostQuantity = Convert.ToInt32(row["LostQuantity"])
                });
            }

            return list;
        }

        /// <summary>
        /// Lấy dữ liệu Báo cáo Độc giả Mượn Sách Quá Hạn
        /// </summary>
        public virtual List<OverdueReportDto> GetOverdueReport()
        {
            var list = new List<OverdueReportDto>();

            string sql = @"
                SELECT 
                    s.BorrowSlipId, s.SlipCode, m.MemberCardCode, m.FullName AS MemberFullName,
                    m.PhoneNumber, b.Title AS BookTitle, c.Barcode, s.BorrowDate, s.DueDate,
                    m.DepositBalance
                FROM dbo.BorrowSlipDetails d
                INNER JOIN dbo.BorrowSlips s ON d.BorrowSlipId = s.BorrowSlipId
                INNER JOIN dbo.Members m ON s.MemberId = m.MemberId
                INNER JOIN dbo.BookCopies c ON d.CopyId = c.CopyId
                INNER JOIN dbo.Books b ON c.BookId = b.BookId
                WHERE s.DueDate < CAST(GETDATE() AS DATE)
                  AND NOT EXISTS (SELECT 1 FROM dbo.ReturnSlipDetails r WHERE r.BorrowSlipDetailId = d.DetailId)
                ORDER BY s.DueDate ASC";

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
            int seq = 1;
            DateTime today = DateTime.Today;

            foreach (DataRow row in table.Rows)
            {
                DateTime dueDate = Convert.ToDateTime(row["DueDate"]);
                int overdueDays = Math.Max(0, (today - dueDate.Date).Days);
                decimal fine = overdueDays * 5000m;

                list.Add(new OverdueReportDto
                {
                    SequenceNumber = seq++,
                    BorrowSlipId = Convert.ToInt32(row["BorrowSlipId"]),
                    SlipCode = row["SlipCode"].ToString(),
                    MemberCardCode = row["MemberCardCode"].ToString(),
                    MemberFullName = row["MemberFullName"].ToString(),
                    PhoneNumber = row["PhoneNumber"] != DBNull.Value ? row["PhoneNumber"].ToString() : string.Empty,
                    BookTitle = row["BookTitle"].ToString(),
                    Barcode = row["Barcode"].ToString(),
                    BorrowDate = Convert.ToDateTime(row["BorrowDate"]),
                    DueDate = dueDate,
                    OverdueDays = overdueDays,
                    EstimatedFine = fine,
                    DepositBalance = Convert.ToDecimal(row["DepositBalance"])
                });
            }

            return list;
        }

        /// <summary>
        /// Tổng hợp chỉ số tài chính (Doanh thu phí dịch vụ, Doanh thu phạt & Quỹ cọc)
        /// </summary>
        public virtual RevenueReportSummaryDto GetRevenueReportSummary(DateTime fromDate, DateTime toDate)
        {
            var summary = new RevenueReportSummaryDto();

            // 1. Tổng tiền phạt (FineReceipts)
            string sqlFine = @"
                SELECT 
                    ISNULL(SUM(TotalAmount), 0) AS TotalFine,
                    ISNULL(SUM(CASE WHEN PaymentMethod = 'DEPOSIT_DEDUCTION' THEN TotalAmount ELSE 0 END), 0) AS FineDeposit,
                    ISNULL(SUM(CASE WHEN PaymentMethod = 'CASH' THEN TotalAmount ELSE 0 END), 0) AS FineCash
                FROM dbo.FineReceipts
                WHERE PaymentDate >= @FromDate AND PaymentDate <= @ToDate";

            // 2. Doanh thu phí dịch vụ không hoàn lại (ServiceFeeReceipts)
            string sqlServiceFee = @"
                SELECT 
                    ISNULL(SUM(CASE WHEN FeeType = 'CARD_ISSUANCE' THEN Amount ELSE 0 END), 0) AS CardIssuanceFee,
                    ISNULL(SUM(CASE WHEN FeeType = 'ANNUAL_RENEWAL' THEN Amount ELSE 0 END), 0) AS AnnualRenewalFee
                FROM dbo.ServiceFeeReceipts
                WHERE PaymentDate >= @FromDate AND PaymentDate <= @ToDate";

            // 3. Biến động quỹ tiền cọc ký quỹ (DepositTransactions)
            string sqlDeposit = @"
                SELECT 
                    ISNULL(SUM(CASE WHEN TransactionType = 'INITIAL_DEPOSIT' THEN Amount ELSE 0 END), 0) AS InitialDeposit,
                    ISNULL(SUM(CASE WHEN TransactionType IN ('TOP_UP_DEPOSIT', 'TOP_UP') THEN Amount ELSE 0 END), 0) AS TopUpDeposit,
                    ISNULL(SUM(CASE WHEN TransactionType IN ('CARD_CANCEL_REFUND', 'REFUND') THEN Amount ELSE 0 END), 0) AS RefundDeposit
                FROM dbo.DepositTransactions
                WHERE TransactionDate >= @FromDate AND TransactionDate <= @ToDate";

            // 4. Số dư quỹ cọc hiện tại của toàn bộ độc giả chưa đóng thẻ
            string sqlCurrentFund = "SELECT ISNULL(SUM(DepositBalance), 0) AS CurrentFund FROM dbo.Members WHERE CardStatus != 'CLOSED'";

            var prmsFine = new[]
            {
                new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = fromDate.Date },
                new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = toDate.Date.AddDays(1).AddTicks(-1) }
            };

            DataTable tblFine = DatabaseConnectionHelper.ExecuteQueryToDataTable(sqlFine, prmsFine);
            if (tblFine.Rows.Count > 0)
            {
                summary.TotalFineCollected = Convert.ToDecimal(tblFine.Rows[0]["TotalFine"]);
                summary.FineFromDepositDeduction = Convert.ToDecimal(tblFine.Rows[0]["FineDeposit"]);
                summary.FineFromCash = Convert.ToDecimal(tblFine.Rows[0]["FineCash"]);
            }

            var prmsFee = new[]
            {
                new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = fromDate.Date },
                new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = toDate.Date.AddDays(1).AddTicks(-1) }
            };

            DataTable tblFee = DatabaseConnectionHelper.ExecuteQueryToDataTable(sqlServiceFee, prmsFee);
            if (tblFee.Rows.Count > 0)
            {
                summary.TotalCardIssuanceFee = Convert.ToDecimal(tblFee.Rows[0]["CardIssuanceFee"]);
                summary.TotalRenewalFee = Convert.ToDecimal(tblFee.Rows[0]["AnnualRenewalFee"]);
            }

            var prmsDep = new[]
            {
                new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = fromDate.Date },
                new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = toDate.Date.AddDays(1).AddTicks(-1) }
            };

            DataTable tblDep = DatabaseConnectionHelper.ExecuteQueryToDataTable(sqlDeposit, prmsDep);
            if (tblDep.Rows.Count > 0)
            {
                summary.TotalDepositInitial = Convert.ToDecimal(tblDep.Rows[0]["InitialDeposit"]);
                summary.TotalDepositTopUp = Convert.ToDecimal(tblDep.Rows[0]["TopUpDeposit"]);
                summary.TotalDepositRefunded = Convert.ToDecimal(tblDep.Rows[0]["RefundDeposit"]);
            }

            object curFund = DatabaseConnectionHelper.ExecuteScalar(sqlCurrentFund);
            summary.CurrentDepositFundBalance = curFund != null && curFund != DBNull.Value ? Convert.ToDecimal(curFund) : 0;

            return summary;
        }

        /// <summary>
        /// Danh sách các giao dịch tài chính trong khoảng thời gian
        /// </summary>
        public virtual List<FinancialTransactionDisplayDto> GetFinancialTransactions(DateTime fromDate, DateTime toDate, string filterType = "ALL")
        {
            var list = new List<FinancialTransactionDisplayDto>();

            // 1. Giao dịch thu phạt
            if (filterType == "ALL" || filterType == "FINE")
            {
                string sqlFine = @"
                    SELECT r.ReceiptCode, m.MemberCardCode, m.FullName AS MemberFullName,
                           r.TotalAmount, r.PaymentMethod, r.PaymentDate, u.FullName AS UserName, r.Reason
                    FROM dbo.FineReceipts r
                    INNER JOIN dbo.Members m ON r.MemberId = m.MemberId
                    LEFT JOIN dbo.UserAccounts u ON r.CollectedByUserId = u.UserId
                    WHERE r.PaymentDate >= @FromDate AND r.PaymentDate <= @ToDate";

                var prmsFine = new[]
                {
                    new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = fromDate.Date },
                    new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = toDate.Date.AddDays(1).AddTicks(-1) }
                };

                DataTable tblFine = DatabaseConnectionHelper.ExecuteQueryToDataTable(sqlFine, prmsFine);
                foreach (DataRow row in tblFine.Rows)
                {
                    string rawMethod = row["PaymentMethod"] != DBNull.Value ? row["PaymentMethod"].ToString() : "CASH";
                    string payMethod = rawMethod == "DEPOSIT_DEDUCTION" ? "Trừ tiền cọc" : (rawMethod == "BANK_TRANSFER" ? "Chuyển khoản QR" : (rawMethod == "DEBT" ? "Ghi nợ" : "Tiền mặt"));
                    list.Add(new FinancialTransactionDisplayDto
                    {
                        TransactionCategory = "Thu tiền phạt",
                        Code = row["ReceiptCode"].ToString(),
                        MemberCardCode = row["MemberCardCode"].ToString(),
                        MemberFullName = row["MemberFullName"].ToString(),
                        RawTransactionType = "FINE",
                        TransactionTypeDisplay = "Thu phạt vi phạm",
                        Amount = Convert.ToDecimal(row["TotalAmount"]),
                        IsInflow = true,
                        PaymentMethod = payMethod,
                        TransactionDate = Convert.ToDateTime(row["PaymentDate"]),
                        HandledByUserName = row["UserName"] != DBNull.Value ? row["UserName"].ToString() : "Thủ thư",
                        Notes = row["Reason"] != DBNull.Value ? row["Reason"].ToString() : string.Empty
                    });
                }
            }

            // 2. Giao dịch phí dịch vụ (Phí mở thẻ, Phí gia hạn thường niên)
            if (filterType == "ALL" || filterType == "INITIAL_DEPOSIT" || filterType == "CARD_ISSUANCE_FEE" || filterType == "SERVICE_FEE")
            {
                string sqlFee = @"
                    SELECT s.ReceiptCode, m.MemberCardCode, m.FullName AS MemberFullName,
                           s.FeeType, s.Amount, s.PaymentMethod, s.PaymentDate, u.FullName AS UserName, s.Notes
                    FROM dbo.ServiceFeeReceipts s
                    INNER JOIN dbo.Members m ON s.MemberId = m.MemberId
                    LEFT JOIN dbo.UserAccounts u ON s.CollectedByUserId = u.UserId
                    WHERE s.PaymentDate >= @FromDate AND s.PaymentDate <= @ToDate";

                var prmsFee = new[]
                {
                    new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = fromDate.Date },
                    new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = toDate.Date.AddDays(1).AddTicks(-1) }
                };

                DataTable tblFee = DatabaseConnectionHelper.ExecuteQueryToDataTable(sqlFee, prmsFee);
                foreach (DataRow row in tblFee.Rows)
                {
                    string feeType = row["FeeType"].ToString();
                    string rawMethod = row["PaymentMethod"] != DBNull.Value ? row["PaymentMethod"].ToString() : "CASH";
                    string payMethod = rawMethod == "DEPOSIT_DEDUCTION" ? "Trừ tiền cọc" : (rawMethod == "VIETQR" || rawMethod == "BANK_TRANSFER" ? "Chuyển khoản QR" : "Tiền mặt");

                    string category = feeType == "CARD_ISSUANCE" ? "Thu phí làm thẻ" : "Thu phí thường niên";
                    string typeDisplay = feeType == "CARD_ISSUANCE" ? "Phí phát hành thẻ" : "Phí gia hạn thẻ";

                    list.Add(new FinancialTransactionDisplayDto
                    {
                        TransactionCategory = category,
                        Code = row["ReceiptCode"].ToString(),
                        MemberCardCode = row["MemberCardCode"].ToString(),
                        MemberFullName = row["MemberFullName"].ToString(),
                        RawTransactionType = feeType == "CARD_ISSUANCE" ? "CARD_ISSUANCE_FEE" : "ANNUAL_RENEWAL",
                        TransactionTypeDisplay = typeDisplay,
                        Amount = Convert.ToDecimal(row["Amount"]),
                        IsInflow = true,
                        PaymentMethod = payMethod,
                        TransactionDate = Convert.ToDateTime(row["PaymentDate"]),
                        HandledByUserName = row["UserName"] != DBNull.Value ? row["UserName"].ToString() : "Thủ thư",
                        Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
                    });
                }
            }

            // 3. Giao dịch quỹ cọc thế chân (Thu cọc ban đầu, Nạp thêm cọc, Hoàn cọc)
            if (filterType == "ALL" || filterType == "INITIAL_DEPOSIT" || filterType == "TOP_UP_DEPOSIT" || filterType == "REFUND" || filterType == "CARD_CANCEL_REFUND" || filterType == "DEPOSIT")
            {
                string typeCondition = " AND t.TransactionType NOT IN ('FINE_DEDUCTION', 'FINE_DEBT', 'ANNUAL_FEE_DEDUCTION', 'CARD_ISSUANCE_FEE')";
                if (filterType == "INITIAL_DEPOSIT")
                {
                    typeCondition = " AND t.TransactionType = 'INITIAL_DEPOSIT'";
                }
                else if (filterType == "TOP_UP_DEPOSIT")
                {
                    typeCondition = " AND t.TransactionType IN ('TOP_UP_DEPOSIT', 'TOP_UP', 'DEBT_PAYMENT')";
                }
                else if (filterType == "REFUND" || filterType == "CARD_CANCEL_REFUND")
                {
                    typeCondition = " AND t.TransactionType IN ('CARD_CANCEL_REFUND', 'REFUND')";
                }
                else if (filterType == "DEPOSIT")
                {
                    typeCondition = " AND t.TransactionType IN ('INITIAL_DEPOSIT', 'TOP_UP_DEPOSIT', 'TOP_UP', 'DEBT_PAYMENT')";
                }

                string sqlDep = $@"
                    SELECT t.ReceiptCode, m.MemberCardCode, m.FullName AS MemberFullName,
                           t.TransactionType, t.Amount, t.TransactionDate, u.FullName AS UserName, t.Notes
                    FROM dbo.DepositTransactions t
                    INNER JOIN dbo.Members m ON t.MemberId = m.MemberId
                    LEFT JOIN dbo.UserAccounts u ON t.HandledByUserId = u.UserId
                    WHERE t.TransactionDate >= @FromDate AND t.TransactionDate <= @ToDate {typeCondition}";

                var prmsDep = new[]
                {
                    new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = fromDate.Date },
                    new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = toDate.Date.AddDays(1).AddTicks(-1) }
                };

                DataTable tblDep = DatabaseConnectionHelper.ExecuteQueryToDataTable(sqlDep, prmsDep);
                foreach (DataRow row in tblDep.Rows)
                {
                    string transType = row["TransactionType"].ToString();
                    bool isRefund = transType == "CARD_CANCEL_REFUND" || transType == "REFUND";
                    bool isInflow = !isRefund;

                    string category = transType == "INITIAL_DEPOSIT" ? "Thu cọc mở thẻ" :
                                      (isRefund ? "Hoàn trả tiền cọc" : "Nạp thêm tiền thẻ");
                    string typeDisplay = transType == "INITIAL_DEPOSIT" ? "Mở thẻ (Cọc ban đầu)" :
                                         (isRefund ? "Hủy thẻ hoàn cọc" : "Nạp vào thẻ");
                    string rawType = isRefund ? "CARD_CANCEL_REFUND" : (transType == "INITIAL_DEPOSIT" ? "INITIAL_DEPOSIT" : "TOP_UP_DEPOSIT");
                    string payMethod = isRefund ? "Tiền mặt / Hoàn ví" : "Tiền mặt / Chuyển khoản";

                    list.Add(new FinancialTransactionDisplayDto
                    {
                        TransactionCategory = category,
                        Code = row["ReceiptCode"].ToString(),
                        MemberCardCode = row["MemberCardCode"].ToString(),
                        MemberFullName = row["MemberFullName"].ToString(),
                        RawTransactionType = rawType,
                        TransactionTypeDisplay = typeDisplay,
                        Amount = Convert.ToDecimal(row["Amount"]),
                        IsInflow = isInflow,
                        PaymentMethod = payMethod,
                        TransactionDate = Convert.ToDateTime(row["TransactionDate"]),
                        HandledByUserName = row["UserName"] != DBNull.Value ? row["UserName"].ToString() : "Thủ thư",
                        Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
                    });
                }
            }

            // Sắp xếp thống nhất toàn bộ giao dịch theo thời gian giảm dần
            list = list.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Code).ToList();
            int seq = 1;
            foreach (var item in list)
            {
                item.SequenceNumber = seq++;
            }

            return list;
        }
    }
}
