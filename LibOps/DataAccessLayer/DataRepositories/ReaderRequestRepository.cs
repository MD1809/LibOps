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
    /// Repository thao tác CSDL với bảng Yêu Cầu Độc Giả (ReaderRequests)
    /// </summary>
    public class ReaderRequestRepository
    {
        public virtual int InsertRequest(ReaderRequestEntity request, SqlTransaction transaction = null)
        {
            string sql = @"
                INSERT INTO dbo.ReaderRequests (
                    RequestCode, MemberId, RequestType, Status, Amount, 
                    PayoutMethod, BankName, BankAccountNumber, BankAccountHolder, 
                    Reason, RequestDate, ProcessedByUserId, ProcessedDate, StaffNotes
                )
                VALUES (
                    @RequestCode, @MemberId, @RequestType, @Status, @Amount, 
                    @PayoutMethod, @BankName, @BankAccountNumber, @BankAccountHolder, 
                    @Reason, @RequestDate, @ProcessedByUserId, @ProcessedDate, @StaffNotes
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new[]
            {
                new SqlParameter("@RequestCode", SqlDbType.NVarChar, 30) { Value = request.RequestCode.Trim() },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = request.MemberId },
                new SqlParameter("@RequestType", SqlDbType.NVarChar, 30) { Value = request.RequestType },
                new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = request.Status },
                new SqlParameter("@Amount", SqlDbType.Decimal) { Value = request.Amount },
                new SqlParameter("@PayoutMethod", SqlDbType.NVarChar, 30) { Value = request.PayoutMethod },
                new SqlParameter("@BankName", SqlDbType.NVarChar, 100) { Value = (object)request.BankName ?? DBNull.Value },
                new SqlParameter("@BankAccountNumber", SqlDbType.NVarChar, 50) { Value = (object)request.BankAccountNumber ?? DBNull.Value },
                new SqlParameter("@BankAccountHolder", SqlDbType.NVarChar, 100) { Value = (object)request.BankAccountHolder ?? DBNull.Value },
                new SqlParameter("@Reason", SqlDbType.NVarChar, 250) { Value = (object)request.Reason ?? DBNull.Value },
                new SqlParameter("@RequestDate", SqlDbType.DateTime) { Value = request.RequestDate },
                new SqlParameter("@ProcessedByUserId", SqlDbType.Int) { Value = request.ProcessedByUserId.HasValue ? (object)request.ProcessedByUserId.Value : DBNull.Value },
                new SqlParameter("@ProcessedDate", SqlDbType.DateTime) { Value = request.ProcessedDate.HasValue ? (object)request.ProcessedDate.Value : DBNull.Value },
                new SqlParameter("@StaffNotes", SqlDbType.NVarChar, 250) { Value = (object)request.StaffNotes ?? DBNull.Value }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters, transaction);
            return Convert.ToInt32(result);
        }

        public virtual ReaderRequestEntity GetRequestById(int requestId, SqlTransaction transaction = null)
        {
            string sql = @"
                SELECT RequestId, RequestCode, MemberId, RequestType, Status, Amount, 
                       PayoutMethod, BankName, BankAccountNumber, BankAccountHolder, 
                       Reason, RequestDate, ProcessedByUserId, ProcessedDate, StaffNotes
                FROM dbo.ReaderRequests
                WHERE RequestId = @RequestId";

            var parameters = new[]
            {
                new SqlParameter("@RequestId", SqlDbType.Int) { Value = requestId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters, transaction);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            DataRow row = table.Rows[0];
            return MapDataRowToEntity(row);
        }

        public virtual List<ReaderRequestDisplayDto> GetRequestsByMemberId(int memberId)
        {
            var list = new List<ReaderRequestDisplayDto>();
            string sql = @"
                SELECT r.RequestId, r.RequestCode, r.MemberId, m.MemberCardCode, m.FullName AS MemberFullName, 
                       m.PhoneNumber AS MemberPhone, r.RequestType, r.Status, r.Amount, r.PayoutMethod, 
                       r.BankName, r.BankAccountNumber, r.BankAccountHolder, r.Reason, r.RequestDate, 
                       r.ProcessedByUserId, u.FullName AS ProcessedByStaffName, r.ProcessedDate, r.StaffNotes
                FROM dbo.ReaderRequests r
                INNER JOIN dbo.Members m ON r.MemberId = m.MemberId
                LEFT JOIN dbo.UserAccounts u ON r.ProcessedByUserId = u.UserId
                WHERE r.MemberId = @MemberId
                ORDER BY r.RequestId DESC";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            foreach (DataRow row in table.Rows)
            {
                list.Add(MapDataRowToDisplayDto(row));
            }

            return list;
        }

        public virtual List<ReaderRequestDisplayDto> GetAllRequests(string status = null, string requestType = null, string keyword = null)
        {
            var list = new List<ReaderRequestDisplayDto>();
            string sql = @"
                SELECT r.RequestId, r.RequestCode, r.MemberId, m.MemberCardCode, m.FullName AS MemberFullName, 
                       m.PhoneNumber AS MemberPhone, r.RequestType, r.Status, r.Amount, r.PayoutMethod, 
                       r.BankName, r.BankAccountNumber, r.BankAccountHolder, r.Reason, r.RequestDate, 
                       r.ProcessedByUserId, u.FullName AS ProcessedByStaffName, r.ProcessedDate, r.StaffNotes
                FROM dbo.ReaderRequests r
                INNER JOIN dbo.Members m ON r.MemberId = m.MemberId
                LEFT JOIN dbo.UserAccounts u ON r.ProcessedByUserId = u.UserId
                WHERE (@Status IS NULL OR @Status = '' OR @Status = 'ALL' OR r.Status = @Status)
                  AND (@RequestType IS NULL OR @RequestType = '' OR @RequestType = 'ALL' OR r.RequestType = @RequestType)
                  AND (@Keyword IS NULL OR m.FullName LIKE @SearchPattern OR m.MemberCardCode LIKE @SearchPattern OR m.PhoneNumber LIKE @SearchPattern OR r.RequestCode LIKE @SearchPattern)
                ORDER BY CASE WHEN r.Status = 'PENDING' THEN 0 ELSE 1 END, r.RequestId DESC";

            var parameters = new[]
            {
                new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = string.IsNullOrWhiteSpace(status) || status == "ALL" ? (object)DBNull.Value : status.Trim() },
                new SqlParameter("@RequestType", SqlDbType.NVarChar, 30) { Value = string.IsNullOrWhiteSpace(requestType) || requestType == "ALL" ? (object)DBNull.Value : requestType.Trim() },
                new SqlParameter("@Keyword", SqlDbType.NVarChar, 100) { Value = string.IsNullOrWhiteSpace(keyword) ? (object)DBNull.Value : keyword.Trim() },
                new SqlParameter("@SearchPattern", SqlDbType.NVarChar, 102) { Value = string.IsNullOrWhiteSpace(keyword) ? (object)DBNull.Value : "%" + keyword.Trim() + "%" }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            foreach (DataRow row in table.Rows)
            {
                list.Add(MapDataRowToDisplayDto(row));
            }

            return list;
        }

        public virtual bool UpdateRequestStatus(int requestId, string status, int processedByUserId, string staffNotes, SqlTransaction transaction = null)
        {
            string sql = @"
                UPDATE dbo.ReaderRequests
                SET Status = @Status, 
                    ProcessedByUserId = @ProcessedByUserId, 
                    ProcessedDate = GETDATE(), 
                    StaffNotes = @StaffNotes
                WHERE RequestId = @RequestId";

            var parameters = new[]
            {
                new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = status },
                new SqlParameter("@ProcessedByUserId", SqlDbType.Int) { Value = processedByUserId },
                new SqlParameter("@StaffNotes", SqlDbType.NVarChar, 250) { Value = (object)staffNotes ?? DBNull.Value },
                new SqlParameter("@RequestId", SqlDbType.Int) { Value = requestId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction);
            return rows > 0;
        }

        public virtual bool CancelRequestByMember(int requestId, int memberId)
        {
            string sql = @"
                UPDATE dbo.ReaderRequests
                SET Status = 'CANCELLED', 
                    ProcessedDate = GETDATE(), 
                    StaffNotes = N'Độc giả chủ động hủy yêu cầu'
                WHERE RequestId = @RequestId AND MemberId = @MemberId AND Status = 'PENDING'";

            var parameters = new[]
            {
                new SqlParameter("@RequestId", SqlDbType.Int) { Value = requestId },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters);
            return rows > 0;
        }

        public virtual bool HasPendingRequest(int memberId, string requestType = null)
        {
            string sql = @"
                IF OBJECT_ID(N'dbo.ReaderRequests', N'U') IS NOT NULL
                BEGIN
                    SELECT COUNT(*) FROM dbo.ReaderRequests 
                    WHERE MemberId = @MemberId 
                      AND Status = 'PENDING'
                      AND (@RequestType IS NULL OR @RequestType = '' OR RequestType = @RequestType)
                END
                ELSE
                BEGIN
                    SELECT 0
                END";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId },
                new SqlParameter("@RequestType", SqlDbType.NVarChar, 30) { Value = string.IsNullOrWhiteSpace(requestType) ? (object)DBNull.Value : requestType.Trim() }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return result != null && Convert.ToInt32(result) > 0;
        }

        public virtual int GetNextRequestSequenceForYear(int year, string requestType)
        {
            string prefixType = "CLS";
            switch (requestType)
            {
                case "REFUND_DEPOSIT":
                    prefixType = "REF";
                    break;
                case "REISSUE_CARD":
                    prefixType = "RIS";
                    break;
                case "UPDATE_INFO":
                    prefixType = "UPD";
                    break;
                case "FEEDBACK_INQUIRY":
                    prefixType = "FDB";
                    break;
                default:
                    prefixType = "CLS";
                    break;
            }

            string prefix = $"REQ-{prefixType}{year}%";
            string sql = @"
                IF OBJECT_ID(N'dbo.ReaderRequests', N'U') IS NOT NULL
                BEGIN
                    SELECT COUNT(*) FROM dbo.ReaderRequests WHERE RequestCode LIKE @Pattern
                END
                ELSE
                BEGIN
                    SELECT 0
                END";

            var parameters = new[]
            {
                new SqlParameter("@Pattern", SqlDbType.NVarChar, 30) { Value = prefix }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return (result != null ? Convert.ToInt32(result) : 0) + 1;
        }

        private ReaderRequestEntity MapDataRowToEntity(DataRow row)
        {
            return new ReaderRequestEntity
            {
                RequestId = Convert.ToInt32(row["RequestId"]),
                RequestCode = row["RequestCode"].ToString(),
                MemberId = Convert.ToInt32(row["MemberId"]),
                RequestType = row["RequestType"].ToString(),
                Status = row["Status"].ToString(),
                Amount = Convert.ToDecimal(row["Amount"]),
                PayoutMethod = row["PayoutMethod"].ToString(),
                BankName = row["BankName"] != DBNull.Value ? row["BankName"].ToString() : string.Empty,
                BankAccountNumber = row["BankAccountNumber"] != DBNull.Value ? row["BankAccountNumber"].ToString() : string.Empty,
                BankAccountHolder = row["BankAccountHolder"] != DBNull.Value ? row["BankAccountHolder"].ToString() : string.Empty,
                Reason = row["Reason"] != DBNull.Value ? row["Reason"].ToString() : string.Empty,
                RequestDate = Convert.ToDateTime(row["RequestDate"]),
                ProcessedByUserId = row["ProcessedByUserId"] != DBNull.Value ? (int?)Convert.ToInt32(row["ProcessedByUserId"]) : null,
                ProcessedDate = row["ProcessedDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["ProcessedDate"]) : null,
                StaffNotes = row["StaffNotes"] != DBNull.Value ? row["StaffNotes"].ToString() : string.Empty
            };
        }

        private ReaderRequestDisplayDto MapDataRowToDisplayDto(DataRow row)
        {
            return new ReaderRequestDisplayDto
            {
                RequestId = Convert.ToInt32(row["RequestId"]),
                RequestCode = row["RequestCode"].ToString(),
                MemberId = Convert.ToInt32(row["MemberId"]),
                MemberCardCode = row["MemberCardCode"].ToString(),
                MemberFullName = row["MemberFullName"].ToString(),
                MemberPhone = row["MemberPhone"] != DBNull.Value ? row["MemberPhone"].ToString() : string.Empty,
                RequestType = row["RequestType"].ToString(),
                Status = row["Status"].ToString(),
                Amount = Convert.ToDecimal(row["Amount"]),
                PayoutMethod = row["PayoutMethod"].ToString(),
                BankName = row["BankName"] != DBNull.Value ? row["BankName"].ToString() : string.Empty,
                BankAccountNumber = row["BankAccountNumber"] != DBNull.Value ? row["BankAccountNumber"].ToString() : string.Empty,
                BankAccountHolder = row["BankAccountHolder"] != DBNull.Value ? row["BankAccountHolder"].ToString() : string.Empty,
                Reason = row["Reason"] != DBNull.Value ? row["Reason"].ToString() : string.Empty,
                RequestDate = Convert.ToDateTime(row["RequestDate"]),
                ProcessedByUserId = row["ProcessedByUserId"] != DBNull.Value ? (int?)Convert.ToInt32(row["ProcessedByUserId"]) : null,
                ProcessedByStaffName = row["ProcessedByStaffName"] != DBNull.Value ? row["ProcessedByStaffName"].ToString() : string.Empty,
                ProcessedDate = row["ProcessedDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["ProcessedDate"]) : null,
                StaffNotes = row["StaffNotes"] != DBNull.Value ? row["StaffNotes"].ToString() : string.Empty
            };
        }
    }
}
