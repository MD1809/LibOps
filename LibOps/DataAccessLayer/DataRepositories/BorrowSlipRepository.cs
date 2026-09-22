using System;
using System.Data;
using System.Data.SqlClient;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataModels.Entities;

namespace LibOps.DataAccessLayer.DataRepositories
{
    /// <summary>
    /// Repository thao tác CSDL với bảng Phiếu Mượn Sách (BorrowSlips)
    /// </summary>
    public class BorrowSlipRepository
    {
        public virtual int InsertBorrowSlip(BorrowSlipEntity slip, SqlTransaction transaction = null)
        {
            string sql = @"
                INSERT INTO dbo.BorrowSlips (SlipCode, MemberId, CreatedByUserId, BorrowDate, DueDate, RenewalCount, Status, Notes)
                VALUES (@SlipCode, @MemberId, @CreatedByUserId, @BorrowDate, @DueDate, @RenewalCount, @Status, @Notes);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            DateTime minSqlDate = new DateTime(1753, 1, 1);
            DateTime safeBorrowDate = slip.BorrowDate > minSqlDate ? slip.BorrowDate : DateTime.Now;
            DateTime safeDueDate = slip.DueDate > minSqlDate ? slip.DueDate : DateTime.Now.AddDays(14);

            var parameters = new[]
            {
                new SqlParameter("@SlipCode", SqlDbType.NVarChar, 30) { Value = slip.SlipCode.Trim() },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = slip.MemberId },
                new SqlParameter("@CreatedByUserId", SqlDbType.Int) { Value = slip.CreatedByUserId },
                new SqlParameter("@BorrowDate", SqlDbType.DateTime) { Value = safeBorrowDate },
                new SqlParameter("@DueDate", SqlDbType.DateTime) { Value = safeDueDate },
                new SqlParameter("@RenewalCount", SqlDbType.Int) { Value = slip.RenewalCount },
                new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = string.IsNullOrWhiteSpace(slip.Status) ? "BORROWING" : slip.Status },
                new SqlParameter("@Notes", SqlDbType.NVarChar, 250) { Value = (object)slip.Notes ?? DBNull.Value }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters, transaction);
            return Convert.ToInt32(result);
        }

        public virtual BorrowSlipEntity GetBorrowSlipById(int borrowSlipId)
        {
            string sql = @"
                SELECT BorrowSlipId, SlipCode, MemberId, CreatedByUserId, BorrowDate, DueDate, ISNULL(RenewalCount, 0) AS RenewalCount, Status, Notes
                FROM dbo.BorrowSlips
                WHERE BorrowSlipId = @BorrowSlipId";

            var parameters = new[]
            {
                new SqlParameter("@BorrowSlipId", SqlDbType.Int) { Value = borrowSlipId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0) return null;

            DataRow row = table.Rows[0];
            return new BorrowSlipEntity
            {
                BorrowSlipId = Convert.ToInt32(row["BorrowSlipId"]),
                SlipCode = row["SlipCode"].ToString(),
                MemberId = Convert.ToInt32(row["MemberId"]),
                CreatedByUserId = Convert.ToInt32(row["CreatedByUserId"]),
                BorrowDate = Convert.ToDateTime(row["BorrowDate"]),
                DueDate = Convert.ToDateTime(row["DueDate"]),
                RenewalCount = row.Table.Columns.Contains("RenewalCount") && row["RenewalCount"] != DBNull.Value ? Convert.ToInt32(row["RenewalCount"]) : 0,
                Status = row["Status"].ToString(),
                Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
            };
        }

        public virtual BorrowSlipEntity GetBorrowSlipByCode(string slipCode)
        {
            if (string.IsNullOrWhiteSpace(slipCode)) return null;

            string sql = @"
                SELECT BorrowSlipId, SlipCode, MemberId, CreatedByUserId, BorrowDate, DueDate, ISNULL(RenewalCount, 0) AS RenewalCount, Status, Notes
                FROM dbo.BorrowSlips
                WHERE SlipCode = @SlipCode";

            var parameters = new[]
            {
                new SqlParameter("@SlipCode", SqlDbType.NVarChar, 30) { Value = slipCode.Trim() }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0) return null;

            DataRow row = table.Rows[0];
            return new BorrowSlipEntity
            {
                BorrowSlipId = Convert.ToInt32(row["BorrowSlipId"]),
                SlipCode = row["SlipCode"].ToString(),
                MemberId = Convert.ToInt32(row["MemberId"]),
                CreatedByUserId = Convert.ToInt32(row["CreatedByUserId"]),
                BorrowDate = Convert.ToDateTime(row["BorrowDate"]),
                DueDate = Convert.ToDateTime(row["DueDate"]),
                RenewalCount = row.Table.Columns.Contains("RenewalCount") && row["RenewalCount"] != DBNull.Value ? Convert.ToInt32(row["RenewalCount"]) : 0,
                Status = row["Status"].ToString(),
                Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
            };
        }

        public virtual bool UpdateBorrowSlipRenewal(int borrowSlipId, DateTime newDueDate, int renewalCount, string notes, SqlTransaction transaction = null)
        {
            string sql = @"
                UPDATE dbo.BorrowSlips
                SET DueDate = @DueDate,
                    RenewalCount = @RenewalCount,
                    Notes = @Notes
                WHERE BorrowSlipId = @BorrowSlipId";

            var parameters = new[]
            {
                new SqlParameter("@BorrowSlipId", SqlDbType.Int) { Value = borrowSlipId },
                new SqlParameter("@DueDate", SqlDbType.DateTime) { Value = newDueDate },
                new SqlParameter("@RenewalCount", SqlDbType.Int) { Value = renewalCount },
                new SqlParameter("@Notes", SqlDbType.NVarChar, 250) { Value = (object)notes ?? DBNull.Value }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction);
            return rows > 0;
        }

        public virtual int GetNextSlipSequenceForDay(DateTime date)
        {
            string datePrefix = $"PM{date:yyyyMMdd}%";
            string sql = "SELECT COUNT(*) FROM dbo.BorrowSlips WHERE SlipCode LIKE @Prefix";
            var parameters = new[]
            {
                new SqlParameter("@Prefix", SqlDbType.NVarChar, 30) { Value = datePrefix }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result) + 1;
        }

        public virtual int GetOverdueBorrowCountForMember(int memberId)
        {
            string sql = @"
                SELECT COUNT(*)
                FROM dbo.BorrowSlipDetails d
                INNER JOIN dbo.BorrowSlips s ON d.BorrowSlipId = s.BorrowSlipId
                WHERE s.MemberId = @MemberId 
                  AND s.DueDate < CAST(GETDATE() AS DATE)
                  AND NOT EXISTS (SELECT 1 FROM dbo.ReturnSlipDetails r WHERE r.BorrowSlipDetailId = d.DetailId)";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result);
        }

        public virtual bool UpdateBorrowSlipStatus(int borrowSlipId, string status, SqlTransaction transaction = null)
        {
            string sql = "UPDATE dbo.BorrowSlips SET Status = @Status WHERE BorrowSlipId = @BorrowSlipId";
            var parameters = new[]
            {
                new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = status },
                new SqlParameter("@BorrowSlipId", SqlDbType.Int) { Value = borrowSlipId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction);
            return rows > 0;
        }

        /// <summary>
        /// Lấy danh sách lịch sử mượn trả theo bộ lọc tìm kiếm (tách riêng dòng Mượn và dòng Trả)
        /// </summary>
        public virtual System.Collections.Generic.List<LibOps.DataModels.DataTransferObjects.BorrowSlipGridDisplayDto> GetBorrowSlips(
            string keyword, string status, DateTime? fromDate, DateTime? toDate, string transactionType = "ALL")
        {
            var list = new System.Collections.Generic.List<LibOps.DataModels.DataTransferObjects.BorrowSlipGridDisplayDto>();

            var prms = new System.Collections.Generic.List<SqlParameter>();
            var whereConditions = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                whereConditions.Add("(T.SlipCode LIKE @KW OR T.MemberCardCode LIKE @KW OR T.MemberFullName LIKE @KW OR T.PhoneNumber LIKE @KW)");
                prms.Add(new SqlParameter("@KW", SqlDbType.NVarChar, 100) { Value = $"%{keyword.Trim()}%" });
            }

            if (!string.IsNullOrWhiteSpace(transactionType) && transactionType != "ALL")
            {
                whereConditions.Add("T.TransactionType = @TxType");
                prms.Add(new SqlParameter("@TxType", SqlDbType.VarChar, 20) { Value = transactionType });
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "ALL")
            {
                if (status == "OVERDUE")
                {
                    whereConditions.Add("(T.TransactionType = 'BORROW' AND T.Status != 'RETURNED' AND T.DueDate < CAST(GETDATE() AS DATE))");
                }
                else if (status == "BORROWING")
                {
                    whereConditions.Add("(T.TransactionType = 'BORROW' AND T.Status = 'BORROWING')");
                }
                else if (status == "RETURNED")
                {
                    whereConditions.Add("(T.TransactionType = 'RETURN' OR T.Status = 'RETURNED')");
                }
                else
                {
                    whereConditions.Add("T.Status = @Status");
                    prms.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = status });
                }
            }

            if (fromDate.HasValue)
            {
                whereConditions.Add("T.TransactionDate >= @FromDate");
                prms.Add(new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = fromDate.Value.Date });
            }

            if (toDate.HasValue)
            {
                whereConditions.Add("T.TransactionDate <= @ToDate");
                prms.Add(new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = toDate.Value.Date.AddDays(1).AddTicks(-1) });
            }

            string whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : string.Empty;

            string sql = $@"
                SELECT * FROM (
                    -- 1. Giao dịch Mượn Sách (BORROW)
                    SELECT 
                        'BORROW' AS TransactionType,
                        N'Mượn sách' AS TransactionTypeDisplay,
                        s.BorrowSlipId AS ReferenceId,
                        s.BorrowSlipId,
                        s.SlipCode,
                        s.MemberId,
                        m.MemberCardCode,
                        m.FullName AS MemberFullName,
                        m.PhoneNumber,
                        s.BorrowDate AS TransactionDate,
                        s.BorrowDate,
                        s.DueDate,
                        ISNULL(s.RenewalCount, 0) AS RenewalCount,
                        COUNT(d.DetailId) AS BookCount,
                        CAST(0 AS DECIMAL(18,2)) AS FineAmount,
                        COUNT(d.DetailId) AS TotalItems,
                        SUM(CASE WHEN r.ReturnDetailId IS NOT NULL THEN 1 ELSE 0 END) AS ReturnedItems,
                        s.Status,
                        ISNULL(u.FullName, N'Thủ thư') AS CreatedByUserName,
                        ISNULL(s.Notes, N'') AS Notes
                    FROM dbo.BorrowSlips s
                    INNER JOIN dbo.Members m ON s.MemberId = m.MemberId
                    LEFT JOIN dbo.UserAccounts u ON s.CreatedByUserId = u.UserId
                    LEFT JOIN dbo.BorrowSlipDetails d ON s.BorrowSlipId = d.BorrowSlipId
                    LEFT JOIN dbo.ReturnSlipDetails r ON d.DetailId = r.BorrowSlipDetailId
                    GROUP BY s.BorrowSlipId, s.SlipCode, s.MemberId, m.MemberCardCode, m.FullName, m.PhoneNumber, s.BorrowDate, s.DueDate, s.RenewalCount, s.Status, s.Notes, u.FullName

                    UNION ALL

                    -- 2. Giao dịch Trả Sách (RETURN)
                    SELECT 
                        'RETURN' AS TransactionType,
                        N'Trả sách' AS TransactionTypeDisplay,
                        r.ReturnDetailId AS ReferenceId,
                        s.BorrowSlipId,
                        s.SlipCode,
                        s.MemberId,
                        m.MemberCardCode,
                        m.FullName AS MemberFullName,
                        m.PhoneNumber,
                        r.ActualReturnDate AS TransactionDate,
                        s.BorrowDate,
                        s.DueDate,
                        ISNULL(s.RenewalCount, 0) AS RenewalCount,
                        1 AS BookCount,
                        ISNULL(r.FineAmount, 0) AS FineAmount,
                        1 AS TotalItems,
                        1 AS ReturnedItems,
                        'RETURNED' AS Status,
                        ISNULL(u_ret.FullName, N'Thủ thư') AS CreatedByUserName,
                        ISNULL(r.ReturnConditionNote, N'') AS Notes
                    FROM dbo.ReturnSlipDetails r
                    INNER JOIN dbo.BorrowSlipDetails d ON r.BorrowSlipDetailId = d.DetailId
                    INNER JOIN dbo.BorrowSlips s ON d.BorrowSlipId = s.BorrowSlipId
                    INNER JOIN dbo.Members m ON s.MemberId = m.MemberId
                    LEFT JOIN dbo.UserAccounts u_ret ON r.ReceivedByUserId = u_ret.UserId
                ) AS T
                {whereClause}
                ORDER BY T.TransactionDate DESC, T.ReferenceId DESC";

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, prms.ToArray());
            int seq = 1;
            foreach (DataRow row in table.Rows)
            {
                string txType = row["TransactionType"].ToString();
                string rawStatus = row["Status"].ToString();
                DateTime dueDate = Convert.ToDateTime(row["DueDate"]);
                int total = Convert.ToInt32(row["TotalItems"]);
                int returned = Convert.ToInt32(row["ReturnedItems"]);
                decimal fine = Convert.ToDecimal(row["FineAmount"]);

                string displayStatus;
                if (txType == "RETURN")
                {
                    displayStatus = fine > 0 ? "Đã trả (Có phạt)" : "Đã trả";
                }
                else
                {
                    if (rawStatus == "RETURNED" || (total > 0 && total == returned))
                    {
                        displayStatus = "Đã trả hết";
                    }
                    else if (dueDate.Date < DateTime.Today)
                    {
                        displayStatus = "Quá hạn";
                    }
                    else if (returned > 0)
                    {
                        displayStatus = "Trả một phần";
                    }
                    else
                    {
                        displayStatus = "Đang mượn";
                    }
                }

                list.Add(new LibOps.DataModels.DataTransferObjects.BorrowSlipGridDisplayDto
                {
                    SequenceNumber = seq++,
                    TransactionType = txType,
                    TransactionTypeDisplay = row["TransactionTypeDisplay"].ToString(),
                    ReferenceId = Convert.ToInt32(row["ReferenceId"]),
                    BorrowSlipId = Convert.ToInt32(row["BorrowSlipId"]),
                    SlipCode = row["SlipCode"].ToString(),
                    MemberId = Convert.ToInt32(row["MemberId"]),
                    MemberCardCode = row["MemberCardCode"].ToString(),
                    MemberFullName = row["MemberFullName"].ToString(),
                    PhoneNumber = row["PhoneNumber"].ToString(),
                    TransactionDate = Convert.ToDateTime(row["TransactionDate"]),
                    BorrowDate = Convert.ToDateTime(row["BorrowDate"]),
                    DueDate = dueDate,
                    RenewalCount = row.Table.Columns.Contains("RenewalCount") && row["RenewalCount"] != DBNull.Value ? Convert.ToInt32(row["RenewalCount"]) : 0,
                    BookCount = Convert.ToInt32(row["BookCount"]),
                    FineAmount = fine,
                    TotalItems = total,
                    ReturnedItems = returned,
                    Status = rawStatus,
                    StatusDisplay = displayStatus,
                    CreatedByUserName = row["CreatedByUserName"].ToString(),
                    Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
                });
            }

            return list;
        }

        /// <summary>
        /// Lấy chi tiết các cuốn sách trong một phiếu mượn
        /// </summary>
        public virtual System.Collections.Generic.List<LibOps.DataModels.DataTransferObjects.BorrowSlipItemDetailDto> GetBorrowSlipItems(int borrowSlipId)
        {
            var list = new System.Collections.Generic.List<LibOps.DataModels.DataTransferObjects.BorrowSlipItemDetailDto>();

            string sql = @"
                SELECT 
                    d.DetailId AS BorrowSlipDetailId, d.CopyId, c.Barcode, b.Title AS BookTitle,
                    ISNULL(cat.CategoryName, N'--') AS CategoryName, ISNULL(b.ShelfLocation, N'--') AS ShelfLocation, 
                    b.Price AS BookPrice, ISNULL(d.BorrowConditionNote, N'Nguyên vẹn') AS BorrowConditionNote,
                    r.ReturnDetailId, r.ActualReturnDate, ISNULL(r.OverdueDays, 0) AS OverdueDays, 
                    ISNULL(r.FineAmount, 0) AS FineAmount, r.ReturnConditionNote,
                    c.Status AS CopyCurrentStatus, r.BookCopyStatusAfterReturn
                FROM dbo.BorrowSlipDetails d
                INNER JOIN dbo.BookCopies c ON d.CopyId = c.CopyId
                INNER JOIN dbo.Books b ON c.BookId = b.BookId
                LEFT JOIN dbo.Categories cat ON b.CategoryId = cat.CategoryId
                LEFT JOIN dbo.ReturnSlipDetails r ON d.DetailId = r.BorrowSlipDetailId
                WHERE d.BorrowSlipId = @BorrowSlipId
                ORDER BY d.DetailId ASC";

            var parameters = new[]
            {
                new SqlParameter("@BorrowSlipId", SqlDbType.Int) { Value = borrowSlipId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            int seq = 1;
            foreach (DataRow row in table.Rows)
            {
                bool isReturned = row["ReturnDetailId"] != DBNull.Value;
                string status = isReturned ? "RETURNED" : "BORROWING";
                string statusDisplay;

                if (isReturned)
                {
                    string afterStatus = row["BookCopyStatusAfterReturn"] != DBNull.Value ? row["BookCopyStatusAfterReturn"].ToString() : "AVAILABLE";
                    statusDisplay = afterStatus == "DAMAGED" ? "Hư hỏng" : (afterStatus == "LOST" ? "Mất sách" : "Nguyên vẹn");
                }
                else
                {
                    statusDisplay = "Đang mượn";
                }

                list.Add(new LibOps.DataModels.DataTransferObjects.BorrowSlipItemDetailDto
                {
                    SequenceNumber = seq++,
                    BorrowSlipDetailId = Convert.ToInt32(row["BorrowSlipDetailId"]),
                    CopyId = Convert.ToInt32(row["CopyId"]),
                    Barcode = row["Barcode"].ToString(),
                    BookTitle = row["BookTitle"].ToString(),
                    CategoryName = row["CategoryName"].ToString(),
                    ShelfLocation = row["ShelfLocation"].ToString(),
                    BookPrice = Convert.ToDecimal(row["BookPrice"]),
                    BorrowConditionNote = row["BorrowConditionNote"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["BorrowConditionNote"].ToString())
                        ? row["BorrowConditionNote"].ToString()
                        : "Nguyên vẹn",
                    ReturnStatus = status,
                    ReturnStatusDisplay = statusDisplay,
                    ActualReturnDate = isReturned ? (DateTime?)Convert.ToDateTime(row["ActualReturnDate"]) : null,
                    OverdueDays = Convert.ToInt32(row["OverdueDays"]),
                    FineAmount = Convert.ToDecimal(row["FineAmount"]),
                    ReturnConditionNote = row["ReturnConditionNote"] != DBNull.Value ? row["ReturnConditionNote"].ToString() : string.Empty
                });
            }

            return list;
        }

        /// <summary>
        /// Lấy chi tiết một cuốn sách trong giao dịch trả
        /// </summary>
        public virtual System.Collections.Generic.List<LibOps.DataModels.DataTransferObjects.BorrowSlipItemDetailDto> GetReturnTransactionItem(int returnDetailId)
        {
            var list = new System.Collections.Generic.List<LibOps.DataModels.DataTransferObjects.BorrowSlipItemDetailDto>();

            string sql = @"
                SELECT 
                    d.DetailId AS BorrowSlipDetailId, d.CopyId, c.Barcode, b.Title AS BookTitle,
                    ISNULL(cat.CategoryName, N'--') AS CategoryName, ISNULL(b.ShelfLocation, N'--') AS ShelfLocation, 
                    b.Price AS BookPrice, ISNULL(d.BorrowConditionNote, N'Nguyên vẹn') AS BorrowConditionNote,
                    r.ReturnDetailId, r.ActualReturnDate, ISNULL(r.OverdueDays, 0) AS OverdueDays, 
                    ISNULL(r.FineAmount, 0) AS FineAmount, r.ReturnConditionNote,
                    c.Status AS CopyCurrentStatus, r.BookCopyStatusAfterReturn
                FROM dbo.ReturnSlipDetails r
                INNER JOIN dbo.BorrowSlipDetails d ON r.BorrowSlipDetailId = d.DetailId
                INNER JOIN dbo.BookCopies c ON d.CopyId = c.CopyId
                INNER JOIN dbo.Books b ON c.BookId = b.BookId
                LEFT JOIN dbo.Categories cat ON b.CategoryId = cat.CategoryId
                WHERE r.ReturnDetailId = @ReturnDetailId";

            var parameters = new[]
            {
                new SqlParameter("@ReturnDetailId", SqlDbType.Int) { Value = returnDetailId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            int seq = 1;
            foreach (DataRow row in table.Rows)
            {
                string afterStatus = row["BookCopyStatusAfterReturn"] != DBNull.Value ? row["BookCopyStatusAfterReturn"].ToString() : "AVAILABLE";
                string statusDisplay = afterStatus == "DAMAGED" ? "Hư hỏng" : (afterStatus == "LOST" ? "Mất sách" : "Nguyên vẹn");

                list.Add(new LibOps.DataModels.DataTransferObjects.BorrowSlipItemDetailDto
                {
                    SequenceNumber = seq++,
                    BorrowSlipDetailId = Convert.ToInt32(row["BorrowSlipDetailId"]),
                    CopyId = Convert.ToInt32(row["CopyId"]),
                    Barcode = row["Barcode"].ToString(),
                    BookTitle = row["BookTitle"].ToString(),
                    CategoryName = row["CategoryName"].ToString(),
                    ShelfLocation = row["ShelfLocation"].ToString(),
                    BookPrice = Convert.ToDecimal(row["BookPrice"]),
                    BorrowConditionNote = row["BorrowConditionNote"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["BorrowConditionNote"].ToString())
                        ? row["BorrowConditionNote"].ToString()
                        : "Nguyên vẹn",
                    ReturnStatus = "RETURNED",
                    ReturnStatusDisplay = statusDisplay,
                    ActualReturnDate = Convert.ToDateTime(row["ActualReturnDate"]),
                    OverdueDays = Convert.ToInt32(row["OverdueDays"]),
                    FineAmount = Convert.ToDecimal(row["FineAmount"]),
                    ReturnConditionNote = row["ReturnConditionNote"] != DBNull.Value ? row["ReturnConditionNote"].ToString() : string.Empty
                });
            }

            return list;
        }

        /// <summary>
        /// Lấy toàn bộ lịch sử mượn trả của 1 độc giả
        /// </summary>
        public virtual System.Collections.Generic.List<LibOps.DataModels.DataTransferObjects.MemberBorrowHistoryDto> GetMemberBorrowHistory(int memberId)
        {
            var list = new System.Collections.Generic.List<LibOps.DataModels.DataTransferObjects.MemberBorrowHistoryDto>();

            string sql = @"
                SELECT 
                    d.DetailId AS BorrowSlipDetailId, s.SlipCode, c.Barcode, b.Title AS BookTitle,
                    s.BorrowDate, s.DueDate, ISNULL(s.RenewalCount, 0) AS RenewalCount, r.ActualReturnDate, ISNULL(r.OverdueDays, 0) AS OverdueDays,
                    CASE 
                        WHEN r.ReturnDetailId IS NOT NULL THEN N'Đã trả'
                        WHEN s.DueDate < CAST(GETDATE() AS DATE) THEN N'Quá hạn'
                        ELSE N'Đang mượn'
                    END AS StatusDisplay,
                    CASE 
                        WHEN r.ReturnDetailId IS NULL THEN N'Chưa trả'
                        WHEN r.BookCopyStatusAfterReturn = 'AVAILABLE' THEN N'Nguyên vẹn'
                        WHEN r.BookCopyStatusAfterReturn = 'DAMAGED' THEN N'Hư hỏng nhẹ'
                        WHEN r.BookCopyStatusAfterReturn = 'LOST' THEN N'Làm mất sách'
                        ELSE ISNULL(r.BookCopyStatusAfterReturn, N'--')
                    END AS BookConditionAfterReturn,
                    ISNULL(r.FineAmount, 0) AS FineAmount
                FROM dbo.BorrowSlipDetails d
                INNER JOIN dbo.BorrowSlips s ON d.BorrowSlipId = s.BorrowSlipId
                INNER JOIN dbo.BookCopies c ON d.CopyId = c.CopyId
                INNER JOIN dbo.Books b ON c.BookId = b.BookId
                LEFT JOIN dbo.ReturnSlipDetails r ON d.DetailId = r.BorrowSlipDetailId
                WHERE s.MemberId = @MemberId
                ORDER BY s.BorrowDate DESC, d.DetailId DESC";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            int seq = 1;
            foreach (DataRow row in table.Rows)
            {
                list.Add(new LibOps.DataModels.DataTransferObjects.MemberBorrowHistoryDto
                {
                    SequenceNumber = seq++,
                    BorrowSlipDetailId = Convert.ToInt32(row["BorrowSlipDetailId"]),
                    SlipCode = row["SlipCode"].ToString(),
                    Barcode = row["Barcode"].ToString(),
                    BookTitle = row["BookTitle"].ToString(),
                    BorrowDate = Convert.ToDateTime(row["BorrowDate"]),
                    DueDate = Convert.ToDateTime(row["DueDate"]),
                    RenewalCount = row.Table.Columns.Contains("RenewalCount") && row["RenewalCount"] != DBNull.Value ? Convert.ToInt32(row["RenewalCount"]) : 0,
                    ActualReturnDate = row["ActualReturnDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["ActualReturnDate"]) : null,
                    OverdueDays = Convert.ToInt32(row["OverdueDays"]),
                    StatusDisplay = row["StatusDisplay"].ToString(),
                    BookConditionAfterReturn = row["BookConditionAfterReturn"].ToString(),
                    FineAmount = Convert.ToDecimal(row["FineAmount"])
                });
            }

            return list;
        }
    }
}
