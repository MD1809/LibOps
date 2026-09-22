using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataModels.Entities;

namespace LibOps.DataAccessLayer.DataRepositories
{
    /// <summary>
    /// Repository thao tác CSDL với bảng Giao Dịch Tiền Cọc (DepositTransactions)
    /// </summary>
    public class DepositRepository
    {
        public virtual List<DepositTransactionEntity> GetTransactionsByMemberId(int memberId)
        {
            var list = new List<DepositTransactionEntity>();
            string sql = @"
                SELECT TransactionId, MemberId, HandledByUserId, TransactionType, 
                       Amount, BalanceAfter, ReceiptCode, TransactionDate, Notes
                FROM dbo.DepositTransactions
                WHERE MemberId = @MemberId
                ORDER BY TransactionId DESC";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new DepositTransactionEntity
                {
                    TransactionId = Convert.ToInt32(row["TransactionId"]),
                    MemberId = Convert.ToInt32(row["MemberId"]),
                    HandledByUserId = Convert.ToInt32(row["HandledByUserId"]),
                    TransactionType = row["TransactionType"].ToString(),
                    Amount = Convert.ToDecimal(row["Amount"]),
                    BalanceAfter = Convert.ToDecimal(row["BalanceAfter"]),
                    ReceiptCode = row["ReceiptCode"] != DBNull.Value ? row["ReceiptCode"].ToString() : string.Empty,
                    TransactionDate = Convert.ToDateTime(row["TransactionDate"]),
                    Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
                });
            }

            return list;
        }

        public virtual int InsertTransaction(DepositTransactionEntity entity, SqlTransaction transaction = null)
        {
            string sql = @"
                INSERT INTO dbo.DepositTransactions (MemberId, HandledByUserId, TransactionType, Amount, BalanceAfter, ReceiptCode, TransactionDate, Notes)
                VALUES (@MemberId, @HandledByUserId, @TransactionType, @Amount, @BalanceAfter, @ReceiptCode, GETDATE(), @Notes);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = entity.MemberId },
                new SqlParameter("@HandledByUserId", SqlDbType.Int) { Value = entity.HandledByUserId },
                new SqlParameter("@TransactionType", SqlDbType.NVarChar, 30) { Value = entity.TransactionType },
                new SqlParameter("@Amount", SqlDbType.Decimal) { Value = entity.Amount },
                new SqlParameter("@BalanceAfter", SqlDbType.Decimal) { Value = entity.BalanceAfter },
                new SqlParameter("@ReceiptCode", SqlDbType.NVarChar, 30) { Value = (object)entity.ReceiptCode ?? DBNull.Value },
                new SqlParameter("@Notes", SqlDbType.NVarChar, 250) { Value = (object)entity.Notes ?? DBNull.Value }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters, transaction);
            return Convert.ToInt32(result);
        }

        public virtual List<LibOps.DataModels.DataTransferObjects.MemberDepositHistoryDto> GetMemberDepositHistory(int memberId)
        {
            var list = new List<LibOps.DataModels.DataTransferObjects.MemberDepositHistoryDto>();
            string sql = @"
                SELECT t.TransactionId, t.ReceiptCode, t.TransactionType, t.Amount, t.BalanceAfter, 
                       t.TransactionDate, ISNULL(u.FullName, N'Thủ thư') AS HandledByUserName, t.Notes
                FROM dbo.DepositTransactions t
                LEFT JOIN dbo.UserAccounts u ON t.HandledByUserId = u.UserId
                WHERE t.MemberId = @MemberId
                ORDER BY t.TransactionDate DESC, t.TransactionId DESC";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            int seq = 1;
            foreach (DataRow row in table.Rows)
            {
                string transType = row["TransactionType"].ToString();
                string typeDisplay = transType == "INITIAL_DEPOSIT" ? "Nộp cọc mở thẻ" :
                                     transType == "TOP_UP" || transType == "TOP_UP_DEPOSIT" ? "Nạp thêm tiền / QR" :
                                     transType == "FINE_DEDUCTION" ? "Khấu trừ nộp phạt" :
                                     transType == "FINE_DEBT" ? "Ghi nhận nợ phạt" :
                                     transType == "ANNUAL_FEE_DEDUCTION" ? "Khấu trừ phí thường niên" :
                                     transType == "CARD_CANCEL_REFUND" || transType == "REFUND" ? "Hoàn cọc" : transType;

                list.Add(new LibOps.DataModels.DataTransferObjects.MemberDepositHistoryDto
                {
                    SequenceNumber = seq++,
                    TransactionId = Convert.ToInt32(row["TransactionId"]),
                    ReceiptCode = row["ReceiptCode"] != DBNull.Value ? row["ReceiptCode"].ToString() : string.Empty,
                    TransactionType = transType,
                    TransactionTypeDisplay = typeDisplay,
                    Amount = Convert.ToDecimal(row["Amount"]),
                    BalanceAfter = Convert.ToDecimal(row["BalanceAfter"]),
                    TransactionDate = Convert.ToDateTime(row["TransactionDate"]),
                    HandledByUserName = row["HandledByUserName"].ToString(),
                    Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
                });
            }

            return list;
        }
    }
}
