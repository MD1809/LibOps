using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;

namespace LibOps.DataAccessLayer.DataRepositories
{
    /// <summary>
    /// Repository thao tác CSDL với bảng Độc Giả (Members)
    /// </summary>
    public class MemberRepository
    {
        static MemberRepository()
        {
            DatabaseConnectionHelper.EnsureDatabaseSchemaUpToDate();
        }

        public virtual List<MemberGridDisplayDto> GetAllMembers(string keyword = null, string cardStatus = null)
        {
            var list = new List<MemberGridDisplayDto>();
            string sql = @"
                SELECT m.MemberId, m.MemberCardCode, m.FullName, m.PhoneNumber, 
                       m.Email, m.IdentityCardNumber, m.Address, m.DateOfBirth, 
                       m.DepositBalance, m.TotalDebt, m.IssueDate, m.ExpiryDate, m.CardStatus, m.Notes,
                       (
                           SELECT COUNT(*)
                           FROM dbo.BorrowSlipDetails d
                           INNER JOIN dbo.BorrowSlips s ON d.BorrowSlipId = s.BorrowSlipId
                           WHERE s.MemberId = m.MemberId 
                             AND NOT EXISTS (SELECT 1 FROM dbo.ReturnSlipDetails r WHERE r.BorrowSlipDetailId = d.DetailId)
                       ) AS CurrentlyBorrowingCount
                FROM dbo.Members m
                WHERE (@Keyword IS NULL OR m.FullName LIKE @SearchPattern OR m.MemberCardCode LIKE @SearchPattern OR m.PhoneNumber LIKE @SearchPattern OR m.IdentityCardNumber LIKE @SearchPattern)
                  AND (@CardStatus IS NULL OR @CardStatus = '' OR @CardStatus = 'ALL' OR m.CardStatus = @CardStatus)
                ORDER BY m.MemberId DESC";

            var parameters = new[]
            {
                new SqlParameter("@Keyword", SqlDbType.NVarChar, 100)
                {
                    Value = string.IsNullOrWhiteSpace(keyword) ? (object)DBNull.Value : keyword.Trim()
                },
                new SqlParameter("@SearchPattern", SqlDbType.NVarChar, 102)
                {
                    Value = string.IsNullOrWhiteSpace(keyword) ? (object)DBNull.Value : "%" + keyword.Trim() + "%"
                },
                new SqlParameter("@CardStatus", SqlDbType.NVarChar, 30)
                {
                    Value = string.IsNullOrWhiteSpace(cardStatus) || cardStatus == "ALL" ? (object)DBNull.Value : cardStatus.Trim()
                }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            foreach (DataRow row in table.Rows)
            {
                string status = row["CardStatus"].ToString();
                string statusDisplay;
                switch (status)
                {
                    case "ACTIVE":
                        statusDisplay = "Đang hoạt động";
                        break;
                    case "LOCKED":
                        statusDisplay = "Bị khóa";
                        break;
                    case "CLOSED":
                        statusDisplay = "Đã hủy thẻ";
                        break;
                    default:
                        statusDisplay = status;
                        break;
                }

                list.Add(new MemberGridDisplayDto
                {
                    MemberId = Convert.ToInt32(row["MemberId"]),
                    MemberCardCode = row["MemberCardCode"].ToString(),
                    FullName = row["FullName"].ToString(),
                    PhoneNumber = row["PhoneNumber"].ToString(),
                    Email = row["Email"] != DBNull.Value ? row["Email"].ToString() : string.Empty,
                    IdentityCardNumber = row["IdentityCardNumber"] != DBNull.Value ? row["IdentityCardNumber"].ToString() : string.Empty,
                    Address = row["Address"] != DBNull.Value ? row["Address"].ToString() : string.Empty,
                    DateOfBirth = row["DateOfBirth"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["DateOfBirth"]) : null,
                    DepositBalance = Convert.ToDecimal(row["DepositBalance"]),
                    TotalDebt = row.Table.Columns.Contains("TotalDebt") && row["TotalDebt"] != DBNull.Value ? Convert.ToDecimal(row["TotalDebt"]) : 0,
                    CurrentlyBorrowingCount = Convert.ToInt32(row["CurrentlyBorrowingCount"]),
                    IssueDate = Convert.ToDateTime(row["IssueDate"]),
                    ExpiryDate = Convert.ToDateTime(row["ExpiryDate"]),
                    CardStatus = status,
                    CardStatusDisplay = statusDisplay,
                    Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
                });
            }

            return list;
        }

        public virtual MemberEntity GetMemberById(int memberId)
        {
            string sql = @"
                SELECT MemberId, MemberCardCode, FullName, PhoneNumber, Email, 
                       IdentityCardNumber, Address, DateOfBirth, DepositBalance, TotalDebt,
                       IssueDate, ExpiryDate, CardStatus, Notes
                FROM dbo.Members
                WHERE MemberId = @MemberId";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            DataRow row = table.Rows[0];
            return new MemberEntity
            {
                MemberId = Convert.ToInt32(row["MemberId"]),
                MemberCardCode = row["MemberCardCode"].ToString(),
                FullName = row["FullName"].ToString(),
                PhoneNumber = row["PhoneNumber"].ToString(),
                Email = row["Email"] != DBNull.Value ? row["Email"].ToString() : string.Empty,
                IdentityCardNumber = row["IdentityCardNumber"] != DBNull.Value ? row["IdentityCardNumber"].ToString() : string.Empty,
                Address = row["Address"] != DBNull.Value ? row["Address"].ToString() : string.Empty,
                DateOfBirth = row["DateOfBirth"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["DateOfBirth"]) : null,
                DepositBalance = Convert.ToDecimal(row["DepositBalance"]),
                TotalDebt = row.Table.Columns.Contains("TotalDebt") && row["TotalDebt"] != DBNull.Value ? Convert.ToDecimal(row["TotalDebt"]) : 0,
                IssueDate = Convert.ToDateTime(row["IssueDate"]),
                ExpiryDate = Convert.ToDateTime(row["ExpiryDate"]),
                CardStatus = row["CardStatus"].ToString(),
                Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
            };
        }

        public virtual MemberEntity GetMemberByCardCode(string cardCode)
        {
            if (string.IsNullOrWhiteSpace(cardCode))
            {
                return null;
            }

            string trimmed = cardCode.Trim();
            string sql = @"
                SELECT TOP 1 MemberId, MemberCardCode, FullName, PhoneNumber, Email, 
                       IdentityCardNumber, Address, DateOfBirth, DepositBalance, TotalDebt,
                       IssueDate, ExpiryDate, CardStatus, Notes
                FROM dbo.Members
                WHERE MemberCardCode = @Code
                   OR PhoneNumber = @Code
                   OR IdentityCardNumber = @Code
                   OR Email = @Code
                   OR FullName = @Code
                   OR MemberCardCode LIKE @SearchPattern
                   OR PhoneNumber LIKE @SearchPattern
                   OR IdentityCardNumber LIKE @SearchPattern
                   OR FullName LIKE @SearchPattern
                ORDER BY 
                   CASE 
                       WHEN MemberCardCode = @Code THEN 1
                       WHEN PhoneNumber = @Code THEN 2
                       WHEN IdentityCardNumber = @Code THEN 3
                       WHEN Email = @Code THEN 4
                       WHEN FullName = @Code THEN 5
                       WHEN MemberCardCode LIKE @Code + '%' THEN 6
                       ELSE 7
                   END ASC";

            var parameters = new[]
            {
                new SqlParameter("@Code", SqlDbType.NVarChar, 100) { Value = trimmed },
                new SqlParameter("@SearchPattern", SqlDbType.NVarChar, 102) { Value = "%" + trimmed + "%" }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            DataRow row = table.Rows[0];
            return new MemberEntity
            {
                MemberId = Convert.ToInt32(row["MemberId"]),
                MemberCardCode = row["MemberCardCode"].ToString(),
                FullName = row["FullName"].ToString(),
                PhoneNumber = row["PhoneNumber"].ToString(),
                Email = row["Email"] != DBNull.Value ? row["Email"].ToString() : string.Empty,
                IdentityCardNumber = row["IdentityCardNumber"] != DBNull.Value ? row["IdentityCardNumber"].ToString() : string.Empty,
                Address = row["Address"] != DBNull.Value ? row["Address"].ToString() : string.Empty,
                DateOfBirth = row["DateOfBirth"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["DateOfBirth"]) : null,
                DepositBalance = Convert.ToDecimal(row["DepositBalance"]),
                TotalDebt = row.Table.Columns.Contains("TotalDebt") && row["TotalDebt"] != DBNull.Value ? Convert.ToDecimal(row["TotalDebt"]) : 0,
                IssueDate = Convert.ToDateTime(row["IssueDate"]),
                ExpiryDate = Convert.ToDateTime(row["ExpiryDate"]),
                CardStatus = row["CardStatus"].ToString(),
                Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
            };
        }

        public virtual bool IsCardCodeExists(string cardCode, int excludeMemberId = 0)
        {
            string sql = "SELECT COUNT(*) FROM dbo.Members WHERE MemberCardCode = @MemberCardCode AND MemberId != @ExcludeMemberId";
            var parameters = new[]
            {
                new SqlParameter("@MemberCardCode", SqlDbType.NVarChar, 30) { Value = cardCode.Trim() },
                new SqlParameter("@ExcludeMemberId", SqlDbType.Int) { Value = excludeMemberId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result) > 0;
        }

        public virtual bool IsIdentityCardExists(string identityCard, int excludeMemberId = 0)
        {
            if (string.IsNullOrWhiteSpace(identityCard))
            {
                return false;
            }

            string sql = "SELECT COUNT(*) FROM dbo.Members WHERE IdentityCardNumber = @IdentityCardNumber AND MemberId != @ExcludeMemberId";
            var parameters = new[]
            {
                new SqlParameter("@IdentityCardNumber", SqlDbType.NVarChar, 20) { Value = identityCard.Trim() },
                new SqlParameter("@ExcludeMemberId", SqlDbType.Int) { Value = excludeMemberId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result) > 0;
        }

        public virtual int GetNextCardSequenceForYear(int year)
        {
            string pattern = $"DG{year}%";
            string sql = "SELECT COUNT(*) FROM dbo.Members WHERE MemberCardCode LIKE @Pattern";
            var parameters = new[]
            {
                new SqlParameter("@Pattern", SqlDbType.NVarChar, 30) { Value = pattern }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result) + 1;
        }

        public virtual int InsertMember(MemberEntity member, SqlTransaction transaction = null)
        {
            string sql = @"
                INSERT INTO dbo.Members (MemberCardCode, FullName, PhoneNumber, Email, IdentityCardNumber, Address, DateOfBirth, DepositBalance, TotalDebt, IssueDate, ExpiryDate, CardStatus, Notes)
                VALUES (@MemberCardCode, @FullName, @PhoneNumber, @Email, @IdentityCardNumber, @Address, @DateOfBirth, @DepositBalance, @TotalDebt, @IssueDate, @ExpiryDate, @CardStatus, @Notes);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            DateTime minSqlDate = new DateTime(1753, 1, 1);
            DateTime safeIssueDate = member.IssueDate > minSqlDate ? member.IssueDate : DateTime.Now;
            DateTime safeExpiryDate = member.ExpiryDate > minSqlDate ? member.ExpiryDate : DateTime.Now.AddYears(1);
            object safeDateOfBirth = (member.DateOfBirth.HasValue && member.DateOfBirth.Value > minSqlDate)
                ? (object)member.DateOfBirth.Value
                : DBNull.Value;

            var parameters = new[]
            {
                new SqlParameter("@MemberCardCode", SqlDbType.NVarChar, 30) { Value = member.MemberCardCode.Trim() },
                new SqlParameter("@FullName", SqlDbType.NVarChar, 100) { Value = member.FullName.Trim() },
                new SqlParameter("@PhoneNumber", SqlDbType.NVarChar, 20) { Value = member.PhoneNumber.Trim() },
                new SqlParameter("@Email", SqlDbType.NVarChar, 100) { Value = (object)member.Email ?? DBNull.Value },
                new SqlParameter("@IdentityCardNumber", SqlDbType.NVarChar, 20) { Value = (object)member.IdentityCardNumber ?? DBNull.Value },
                new SqlParameter("@Address", SqlDbType.NVarChar, 250) { Value = (object)member.Address ?? DBNull.Value },
                new SqlParameter("@DateOfBirth", SqlDbType.DateTime) { Value = safeDateOfBirth },
                new SqlParameter("@DepositBalance", SqlDbType.Decimal) { Value = member.DepositBalance },
                new SqlParameter("@TotalDebt", SqlDbType.Decimal) { Value = member.TotalDebt },
                new SqlParameter("@IssueDate", SqlDbType.DateTime) { Value = safeIssueDate },
                new SqlParameter("@ExpiryDate", SqlDbType.DateTime) { Value = safeExpiryDate },
                new SqlParameter("@CardStatus", SqlDbType.NVarChar, 30) { Value = member.CardStatus },
                new SqlParameter("@Notes", SqlDbType.NVarChar, 250) { Value = (object)member.Notes ?? DBNull.Value }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters, transaction);
            return Convert.ToInt32(result);
        }

        public virtual bool UpdateMember(MemberEntity member, SqlTransaction transaction = null)
        {
            string sql = @"
                UPDATE dbo.Members
                SET FullName = @FullName, PhoneNumber = @PhoneNumber, Email = @Email, 
                    IdentityCardNumber = @IdentityCardNumber, Address = @Address, 
                    DateOfBirth = @DateOfBirth, ExpiryDate = @ExpiryDate, Notes = @Notes
                WHERE MemberId = @MemberId";

            DateTime minSqlDate = new DateTime(1753, 1, 1);
            DateTime safeExpiryDate = member.ExpiryDate > minSqlDate ? member.ExpiryDate : DateTime.Now.AddYears(1);
            object safeDateOfBirth = (member.DateOfBirth.HasValue && member.DateOfBirth.Value > minSqlDate)
                ? (object)member.DateOfBirth.Value
                : DBNull.Value;

            var parameters = new[]
            {
                new SqlParameter("@FullName", SqlDbType.NVarChar, 100) { Value = member.FullName.Trim() },
                new SqlParameter("@PhoneNumber", SqlDbType.NVarChar, 20) { Value = member.PhoneNumber.Trim() },
                new SqlParameter("@Email", SqlDbType.NVarChar, 100) { Value = (object)member.Email ?? DBNull.Value },
                new SqlParameter("@IdentityCardNumber", SqlDbType.NVarChar, 20) { Value = (object)member.IdentityCardNumber ?? DBNull.Value },
                new SqlParameter("@Address", SqlDbType.NVarChar, 250) { Value = (object)member.Address ?? DBNull.Value },
                new SqlParameter("@DateOfBirth", SqlDbType.DateTime) { Value = safeDateOfBirth },
                new SqlParameter("@ExpiryDate", SqlDbType.DateTime) { Value = safeExpiryDate },
                new SqlParameter("@Notes", SqlDbType.NVarChar, 250) { Value = (object)member.Notes ?? DBNull.Value },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = member.MemberId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction);
            return rows > 0;
        }

        public virtual bool UpdateDepositBalance(int memberId, decimal newBalance, SqlTransaction transaction = null)
        {
            string sql = "UPDATE dbo.Members SET DepositBalance = @DepositBalance WHERE MemberId = @MemberId";
            var parameters = new[]
            {
                new SqlParameter("@DepositBalance", SqlDbType.Decimal) { Value = newBalance },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction);
            return rows > 0;
        }

        public virtual bool UpdateTotalDebt(int memberId, decimal newDebt, SqlTransaction transaction = null)
        {
            string sql = "UPDATE dbo.Members SET TotalDebt = @TotalDebt WHERE MemberId = @MemberId";
            var parameters = new[]
            {
                new SqlParameter("@TotalDebt", SqlDbType.Decimal) { Value = newDebt },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction);
            return rows > 0;
        }

        public virtual bool UpdateDepositAndDebt(int memberId, decimal newDeposit, decimal newDebt, SqlTransaction transaction = null)
        {
            string sql = "UPDATE dbo.Members SET DepositBalance = @DepositBalance, TotalDebt = @TotalDebt WHERE MemberId = @MemberId";
            var parameters = new[]
            {
                new SqlParameter("@DepositBalance", SqlDbType.Decimal) { Value = newDeposit },
                new SqlParameter("@TotalDebt", SqlDbType.Decimal) { Value = newDebt },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction);
            return rows > 0;
        }

        public virtual bool UpdateCardStatus(int memberId, string status, SqlTransaction transaction = null)
        {
            string sql = "UPDATE dbo.Members SET CardStatus = @CardStatus WHERE MemberId = @MemberId";
            var parameters = new[]
            {
                new SqlParameter("@CardStatus", SqlDbType.NVarChar, 30) { Value = status },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction);
            return rows > 0;
        }

        public virtual bool UpdateExpiryDate(int memberId, DateTime newExpiryDate, SqlTransaction transaction = null)
        {
            string sql = "UPDATE dbo.Members SET ExpiryDate = @ExpiryDate WHERE MemberId = @MemberId";
            var parameters = new[]
            {
                new SqlParameter("@ExpiryDate", SqlDbType.DateTime) { Value = newExpiryDate },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction);
            return rows > 0;
        }

        /// <summary>
        /// Tái kích hoạt hồ sơ thẻ độc giả đã bị đóng/hủy (CLOSED -> ACTIVE)
        /// </summary>
        public virtual bool ReactivateMember(int memberId, decimal initialDeposit, DateTime newExpiryDate, SqlTransaction transaction = null)
        {
            string sql = @"
                UPDATE dbo.Members 
                SET CardStatus = 'ACTIVE', 
                    DepositBalance = @DepositBalance, 
                    ExpiryDate = @ExpiryDate,
                    IssueDate = GETDATE()
                WHERE MemberId = @MemberId";

            var parameters = new[]
            {
                new SqlParameter("@DepositBalance", SqlDbType.Decimal) { Value = initialDeposit },
                new SqlParameter("@ExpiryDate", SqlDbType.DateTime) { Value = newExpiryDate },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            return DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction) > 0;
        }

        public virtual MemberEntity GetMemberByIdentityCard(string identityCard)
        {
            if (string.IsNullOrWhiteSpace(identityCard)) return null;

            string sql = @"
                SELECT TOP 1 MemberId, MemberCardCode, FullName, PhoneNumber, Email, 
                       IdentityCardNumber, Address, DateOfBirth, DepositBalance, TotalDebt,
                       IssueDate, ExpiryDate, CardStatus, Notes
                FROM dbo.Members
                WHERE IdentityCardNumber = @IdentityCardNumber";

            var parameters = new[]
            {
                new SqlParameter("@IdentityCardNumber", SqlDbType.NVarChar, 20) { Value = identityCard.Trim() }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0) return null;

            DataRow row = table.Rows[0];
            return new MemberEntity
            {
                MemberId = Convert.ToInt32(row["MemberId"]),
                MemberCardCode = row["MemberCardCode"].ToString(),
                FullName = row["FullName"].ToString(),
                PhoneNumber = row["PhoneNumber"].ToString(),
                Email = row["Email"] != DBNull.Value ? row["Email"].ToString() : string.Empty,
                IdentityCardNumber = row["IdentityCardNumber"] != DBNull.Value ? row["IdentityCardNumber"].ToString() : string.Empty,
                Address = row["Address"] != DBNull.Value ? row["Address"].ToString() : string.Empty,
                DateOfBirth = row["DateOfBirth"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["DateOfBirth"]) : null,
                DepositBalance = Convert.ToDecimal(row["DepositBalance"]),
                TotalDebt = row.Table.Columns.Contains("TotalDebt") && row["TotalDebt"] != DBNull.Value ? Convert.ToDecimal(row["TotalDebt"]) : 0,
                IssueDate = Convert.ToDateTime(row["IssueDate"]),
                ExpiryDate = Convert.ToDateTime(row["ExpiryDate"]),
                CardStatus = row["CardStatus"].ToString(),
                Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
            };
        }

        public virtual int GetCurrentlyBorrowingCount(int memberId)
        {
            string sql = @"
                SELECT COUNT(*)
                FROM dbo.BorrowSlipDetails d
                INNER JOIN dbo.BorrowSlips s ON d.BorrowSlipId = s.BorrowSlipId
                WHERE s.MemberId = @MemberId 
                  AND NOT EXISTS (SELECT 1 FROM dbo.ReturnSlipDetails r WHERE r.BorrowSlipDetailId = d.DetailId)";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Kiểm tra xem độc giả có yêu cầu rút tiền cọc hoặc hủy thẻ nào đang ở trạng thái chờ duyệt (PENDING) không
        /// </summary>
        public virtual bool HasPendingReaderRequest(int memberId)
        {
            string sql = @"
                IF OBJECT_ID(N'dbo.ReaderRequests', N'U') IS NOT NULL
                BEGIN
                    SELECT COUNT(*) FROM dbo.ReaderRequests 
                    WHERE MemberId = @MemberId AND Status = 'PENDING'
                END
                ELSE
                BEGIN
                    SELECT 0
                END";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return result != null && Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// Lấy danh sách độc giả có thẻ đã hết hạn sử dụng và đang có nợ phạt (kèm số lượng sách đang mượn)
        /// </summary>
        public virtual List<ExpiredMemberDebtDto> GetExpiredMembersWithDebt()
        {
            var list = new List<ExpiredMemberDebtDto>();
            string sql = @"
                SELECT m.MemberId, m.MemberCardCode, m.FullName, m.PhoneNumber, m.ExpiryDate, 
                       m.TotalDebt, m.DepositBalance, m.CardStatus,
                       (
                           SELECT COUNT(*)
                           FROM dbo.BorrowSlipDetails d
                           INNER JOIN dbo.BorrowSlips s ON d.BorrowSlipId = s.BorrowSlipId
                           WHERE s.MemberId = m.MemberId 
                             AND NOT EXISTS (SELECT 1 FROM dbo.ReturnSlipDetails r WHERE r.BorrowSlipDetailId = d.DetailId)
                       ) AS CurrentlyBorrowingCount
                FROM dbo.Members m
                WHERE m.TotalDebt > 0 
                  AND (m.ExpiryDate < GETDATE() OR m.CardStatus = 'EXPIRED')
                ORDER BY m.ExpiryDate ASC, m.MemberId ASC";

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new ExpiredMemberDebtDto
                {
                    MemberId = Convert.ToInt32(row["MemberId"]),
                    MemberCardCode = row["MemberCardCode"].ToString(),
                    FullName = row["FullName"].ToString(),
                    PhoneNumber = row["PhoneNumber"] != DBNull.Value ? row["PhoneNumber"].ToString() : string.Empty,
                    ExpiryDate = Convert.ToDateTime(row["ExpiryDate"]),
                    TotalDebt = Convert.ToDecimal(row["TotalDebt"]),
                    DepositBalance = Convert.ToDecimal(row["DepositBalance"]),
                    CardStatus = row["CardStatus"].ToString(),
                    CurrentlyBorrowingCount = Convert.ToInt32(row["CurrentlyBorrowingCount"])
                });
            }

            return list;
        }
    }
}
