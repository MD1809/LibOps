using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataModels.Entities;

namespace LibOps.DataAccessLayer.DataRepositories
{
    /// <summary>
    /// Repository thao tác CSDL với bảng Biên Lai Thu Tiền Phạt Và Đền Bù (FineReceipts)
    /// </summary>
    public class FineReceiptRepository
    {
        public virtual int InsertFineReceipt(FineReceiptEntity entity, SqlTransaction transaction = null)
        {
            string sql = @"
                INSERT INTO dbo.FineReceipts (ReceiptCode, MemberId, BorrowSlipId, CollectedByUserId, TotalAmount, PaymentMethod, Reason, PaymentDate, Notes)
                VALUES (@ReceiptCode, @MemberId, @BorrowSlipId, @CollectedByUserId, @TotalAmount, @PaymentMethod, @Reason, GETDATE(), @Notes);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new[]
            {
                new SqlParameter("@ReceiptCode", SqlDbType.NVarChar, 30) { Value = entity.ReceiptCode.Trim() },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = entity.MemberId },
                new SqlParameter("@BorrowSlipId", SqlDbType.Int) { Value = entity.BorrowSlipId.HasValue ? (object)entity.BorrowSlipId.Value : DBNull.Value },
                new SqlParameter("@CollectedByUserId", SqlDbType.Int) { Value = entity.CollectedByUserId },
                new SqlParameter("@TotalAmount", SqlDbType.Decimal) { Value = entity.TotalAmount },
                new SqlParameter("@PaymentMethod", SqlDbType.NVarChar, 30) { Value = entity.PaymentMethod },
                new SqlParameter("@Reason", SqlDbType.NVarChar, 250) { Value = entity.Reason.Trim() },
                new SqlParameter("@Notes", SqlDbType.NVarChar, 250) { Value = (object)entity.Notes ?? DBNull.Value }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters, transaction);
            return Convert.ToInt32(result);
        }

        public virtual int GetNextReceiptSequenceForDay(DateTime date)
        {
            string datePrefix = $"REC-FINE-{date:yyyyMMdd}%";
            string sql = "SELECT COUNT(*) FROM dbo.FineReceipts WHERE ReceiptCode LIKE @Prefix";
            var parameters = new[]
            {
                new SqlParameter("@Prefix", SqlDbType.NVarChar, 30) { Value = datePrefix }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result) + 1;
        }

        public virtual FineReceiptEntity GetReceiptByCode(string receiptCode)
        {
            if (string.IsNullOrWhiteSpace(receiptCode)) return null;

            string sql = @"
                SELECT ReceiptId, ReceiptCode, MemberId, BorrowSlipId, CollectedByUserId, TotalAmount, PaymentMethod, Reason, PaymentDate, Notes
                FROM dbo.FineReceipts
                WHERE ReceiptCode = @ReceiptCode";

            var parameters = new[]
            {
                new SqlParameter("@ReceiptCode", SqlDbType.NVarChar, 30) { Value = receiptCode.Trim() }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0) return null;

            DataRow row = table.Rows[0];
            return new FineReceiptEntity
            {
                ReceiptId = Convert.ToInt32(row["ReceiptId"]),
                ReceiptCode = row["ReceiptCode"].ToString(),
                MemberId = Convert.ToInt32(row["MemberId"]),
                BorrowSlipId = row["BorrowSlipId"] != DBNull.Value ? (int?)Convert.ToInt32(row["BorrowSlipId"]) : null,
                CollectedByUserId = Convert.ToInt32(row["CollectedByUserId"]),
                TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                PaymentMethod = row["PaymentMethod"].ToString(),
                Reason = row["Reason"].ToString(),
                PaymentDate = Convert.ToDateTime(row["PaymentDate"]),
                Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
            };
        }

        public virtual List<LibOps.DataModels.DataTransferObjects.MemberFineHistoryDto> GetMemberFineHistory(int memberId)
        {
            var list = new List<LibOps.DataModels.DataTransferObjects.MemberFineHistoryDto>();
            string sql = @"
                SELECT r.ReceiptId, r.ReceiptCode, ISNULL(s.SlipCode, N'--') AS SlipCode, 
                       r.TotalAmount, r.PaymentMethod, r.Reason, r.PaymentDate, 
                       ISNULL(u.FullName, N'Thủ thư') AS CollectedByUserName, r.Notes
                FROM dbo.FineReceipts r
                LEFT JOIN dbo.BorrowSlips s ON r.BorrowSlipId = s.BorrowSlipId
                LEFT JOIN dbo.UserAccounts u ON r.CollectedByUserId = u.UserId
                WHERE r.MemberId = @MemberId
                ORDER BY r.PaymentDate DESC, r.ReceiptId DESC";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            int seq = 1;
            foreach (DataRow row in table.Rows)
            {
                string payMethod = row["PaymentMethod"].ToString();
                string payDisplay = payMethod == "CASH" ? "Tiền mặt / QR" :
                                    payMethod == "DEPOSIT_DEDUCTION" ? "Khấu trừ cọc" :
                                    payMethod == "DEBT" ? "Ghi nhận nợ" : payMethod;

                list.Add(new LibOps.DataModels.DataTransferObjects.MemberFineHistoryDto
                {
                    SequenceNumber = seq++,
                    ReceiptId = Convert.ToInt32(row["ReceiptId"]),
                    ReceiptCode = row["ReceiptCode"].ToString(),
                    SlipCode = row["SlipCode"].ToString(),
                    TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                    PaymentMethod = payMethod,
                    PaymentMethodDisplay = payDisplay,
                    Reason = row["Reason"].ToString(),
                    PaymentDate = Convert.ToDateTime(row["PaymentDate"]),
                    CollectedByUserName = row["CollectedByUserName"].ToString(),
                    Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
                });
            }

            return list;
        }
    }
}
