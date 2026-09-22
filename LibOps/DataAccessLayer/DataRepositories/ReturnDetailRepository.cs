using System;
using System.Data;
using System.Data.SqlClient;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;

namespace LibOps.DataAccessLayer.DataRepositories
{
    /// <summary>
    /// Repository thao tác CSDL với bảng Chi Tiết Trả Sách (ReturnSlipDetails)
    /// </summary>
    public class ReturnDetailRepository
    {
        public virtual int InsertReturnSlipDetail(ReturnSlipDetailEntity detail, SqlTransaction transaction = null)
        {
            string sql = @"
                INSERT INTO dbo.ReturnSlipDetails (BorrowSlipDetailId, ReceivedByUserId, ActualReturnDate, OverdueDays, FineAmount, ReturnConditionNote, BookCopyStatusAfterReturn)
                VALUES (@BorrowSlipDetailId, @ReceivedByUserId, @ActualReturnDate, @OverdueDays, @FineAmount, @ReturnConditionNote, @BookCopyStatusAfterReturn);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            DateTime minSqlDate = new DateTime(1753, 1, 1);
            DateTime safeReturnDate = detail.ActualReturnDate > minSqlDate ? detail.ActualReturnDate : DateTime.Now;

            var parameters = new[]
            {
                new SqlParameter("@BorrowSlipDetailId", SqlDbType.Int) { Value = detail.BorrowSlipDetailId },
                new SqlParameter("@ReceivedByUserId", SqlDbType.Int) { Value = detail.ReceivedByUserId },
                new SqlParameter("@ActualReturnDate", SqlDbType.DateTime) { Value = safeReturnDate },
                new SqlParameter("@OverdueDays", SqlDbType.Int) { Value = detail.OverdueDays },
                new SqlParameter("@FineAmount", SqlDbType.Decimal) { Value = detail.FineAmount },
                new SqlParameter("@ReturnConditionNote", SqlDbType.NVarChar, 250) { Value = (object)detail.ReturnConditionNote ?? DBNull.Value },
                new SqlParameter("@BookCopyStatusAfterReturn", SqlDbType.NVarChar, 30) { Value = detail.BookCopyStatusAfterReturn }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters, transaction);
            return Convert.ToInt32(result);
        }

        public virtual ReturnBookLookupDto GetActiveBorrowRecordByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;

            string sql = @"
                SELECT TOP 1 
                    d.DetailId AS BorrowSlipDetailId,
                    s.BorrowSlipId,
                    s.SlipCode,
                    c.CopyId,
                    c.Barcode,
                    b.BookId,
                    b.Title,
                    b.Price AS BookPrice,
                    b.ShelfLocation,
                    m.MemberId,
                    m.MemberCardCode,
                    m.FullName AS MemberFullName,
                    m.PhoneNumber AS MemberPhone,
                    m.DepositBalance AS MemberDepositBalance,
                    s.BorrowDate,
                    s.DueDate,
                    d.BorrowConditionNote
                FROM dbo.BookCopies c
                INNER JOIN dbo.Books b ON c.BookId = b.BookId
                INNER JOIN dbo.BorrowSlipDetails d ON c.CopyId = d.CopyId
                INNER JOIN dbo.BorrowSlips s ON d.BorrowSlipId = s.BorrowSlipId
                INNER JOIN dbo.Members m ON s.MemberId = m.MemberId
                WHERE c.Barcode = @Barcode
                  AND NOT EXISTS (SELECT 1 FROM dbo.ReturnSlipDetails r WHERE r.BorrowSlipDetailId = d.DetailId)
                ORDER BY d.DetailId DESC";

            var parameters = new[]
            {
                new SqlParameter("@Barcode", SqlDbType.NVarChar, 50) { Value = barcode.Trim() }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0) return null;

            DataRow row = table.Rows[0];
            DateTime dueDate = Convert.ToDateTime(row["DueDate"]);
            DateTime today = DateTime.Today;
            int overdueDays = Math.Max(0, (today - dueDate.Date).Days);
            decimal overdueFine = overdueDays * 5000m; // 5.000 VNĐ / ngày trễ hạn

            return new ReturnBookLookupDto
            {
                BorrowSlipDetailId = Convert.ToInt32(row["BorrowSlipDetailId"]),
                BorrowSlipId = Convert.ToInt32(row["BorrowSlipId"]),
                SlipCode = row["SlipCode"].ToString(),
                CopyId = Convert.ToInt32(row["CopyId"]),
                Barcode = row["Barcode"].ToString(),
                BookId = Convert.ToInt32(row["BookId"]),
                Title = row["Title"].ToString(),
                BookPrice = Convert.ToDecimal(row["BookPrice"]),
                ShelfLocation = row["ShelfLocation"] != DBNull.Value ? row["ShelfLocation"].ToString() : string.Empty,
                MemberId = Convert.ToInt32(row["MemberId"]),
                MemberCardCode = row["MemberCardCode"].ToString(),
                MemberFullName = row["MemberFullName"].ToString(),
                MemberPhone = row["MemberPhone"].ToString(),
                MemberDepositBalance = Convert.ToDecimal(row["MemberDepositBalance"]),
                BorrowDate = Convert.ToDateTime(row["BorrowDate"]),
                DueDate = dueDate,
                BorrowConditionNote = row["BorrowConditionNote"] != DBNull.Value ? row["BorrowConditionNote"].ToString() : string.Empty,
                OverdueDays = overdueDays,
                OverdueFine = overdueFine
            };
        }

        public virtual bool AreAllDetailsReturned(int borrowSlipId, SqlTransaction transaction = null)
        {
            string sql = @"
                SELECT COUNT(*)
                FROM dbo.BorrowSlipDetails d
                WHERE d.BorrowSlipId = @BorrowSlipId
                  AND NOT EXISTS (SELECT 1 FROM dbo.ReturnSlipDetails r WHERE r.BorrowSlipDetailId = d.DetailId)";

            var parameters = new[]
            {
                new SqlParameter("@BorrowSlipId", SqlDbType.Int) { Value = borrowSlipId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters, transaction);
            return Convert.ToInt32(result) == 0;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ các cuốn sách đang mượn chưa trả của một độc giả (theo Mã thẻ, SĐT hoặc Tên)
        /// </summary>
        public virtual System.Collections.Generic.List<ReturnBookLookupDto> GetActiveBorrowRecordsByMember(string keyword)
        {
            var list = new System.Collections.Generic.List<ReturnBookLookupDto>();
            string sql = @"
                SELECT 
                    d.DetailId AS BorrowSlipDetailId,
                    s.BorrowSlipId,
                    s.SlipCode,
                    c.CopyId,
                    c.Barcode,
                    b.BookId,
                    b.Title,
                    b.Price AS BookPrice,
                    b.ShelfLocation,
                    m.MemberId,
                    m.MemberCardCode,
                    m.FullName AS MemberFullName,
                    m.PhoneNumber AS MemberPhone,
                    m.DepositBalance AS MemberDepositBalance,
                    s.BorrowDate,
                    s.DueDate,
                    d.BorrowConditionNote
                FROM dbo.BookCopies c
                INNER JOIN dbo.Books b ON c.BookId = b.BookId
                INNER JOIN dbo.BorrowSlipDetails d ON c.CopyId = d.CopyId
                INNER JOIN dbo.BorrowSlips s ON d.BorrowSlipId = s.BorrowSlipId
                INNER JOIN dbo.Members m ON s.MemberId = m.MemberId
                WHERE NOT EXISTS (SELECT 1 FROM dbo.ReturnSlipDetails r WHERE r.BorrowSlipDetailId = d.DetailId)";

            SqlParameter[] parameters;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sql += @" AND (m.MemberCardCode = @Keyword 
                            OR m.PhoneNumber = @Keyword 
                            OR m.IdentityCardNumber = @Keyword
                            OR m.FullName LIKE @KeywordPattern)";
                parameters = new[]
                {
                    new SqlParameter("@Keyword", SqlDbType.NVarChar, 100) { Value = keyword.Trim() },
                    new SqlParameter("@KeywordPattern", SqlDbType.NVarChar, 100) { Value = "%" + keyword.Trim() + "%" }
                };
            }
            else
            {
                parameters = new SqlParameter[0];
            }

            sql += " ORDER BY s.DueDate ASC, d.DetailId DESC";

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            DateTime today = DateTime.Today;

            foreach (DataRow row in table.Rows)
            {
                DateTime dueDate = Convert.ToDateTime(row["DueDate"]);
                int overdueDays = Math.Max(0, (today - dueDate.Date).Days);
                decimal overdueFine = overdueDays * 5000m;

                list.Add(new ReturnBookLookupDto
                {
                    BorrowSlipDetailId = Convert.ToInt32(row["BorrowSlipDetailId"]),
                    BorrowSlipId = Convert.ToInt32(row["BorrowSlipId"]),
                    SlipCode = row["SlipCode"].ToString(),
                    CopyId = Convert.ToInt32(row["CopyId"]),
                    Barcode = row["Barcode"].ToString(),
                    BookId = Convert.ToInt32(row["BookId"]),
                    Title = row["Title"].ToString(),
                    BookPrice = Convert.ToDecimal(row["BookPrice"]),
                    ShelfLocation = row["ShelfLocation"] != DBNull.Value ? row["ShelfLocation"].ToString() : string.Empty,
                    MemberId = Convert.ToInt32(row["MemberId"]),
                    MemberCardCode = row["MemberCardCode"].ToString(),
                    MemberFullName = row["MemberFullName"].ToString(),
                    MemberPhone = row["MemberPhone"].ToString(),
                    MemberDepositBalance = Convert.ToDecimal(row["MemberDepositBalance"]),
                    BorrowDate = Convert.ToDateTime(row["BorrowDate"]),
                    DueDate = dueDate,
                    BorrowConditionNote = row["BorrowConditionNote"] != DBNull.Value ? row["BorrowConditionNote"].ToString() : string.Empty,
                    OverdueDays = overdueDays,
                    OverdueFine = overdueFine
                });
            }

            return list;
        }
    }
}
