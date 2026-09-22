using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataModels.Entities;

namespace LibOps.DataAccessLayer.DataRepositories
{
    /// <summary>
    /// Repository thao tác CSDL với bảng Biên Lai Thu Phí Dịch Vụ (ServiceFeeReceipts)
    /// </summary>
    public class ServiceFeeRepository
    {
        public virtual int InsertReceipt(ServiceFeeReceiptEntity entity, SqlTransaction transaction = null)
        {
            string sql = @"
                INSERT INTO dbo.ServiceFeeReceipts 
                    (ReceiptCode, MemberId, FeeType, Amount, PaymentMethod, PaymentDate, CollectedByUserId, Notes)
                VALUES 
                    (@ReceiptCode, @MemberId, @FeeType, @Amount, @PaymentMethod, GETDATE(), @CollectedByUserId, @Notes);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new[]
            {
                new SqlParameter("@ReceiptCode", SqlDbType.NVarChar, 30) { Value = entity.ReceiptCode },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = entity.MemberId },
                new SqlParameter("@FeeType", SqlDbType.NVarChar, 30) { Value = entity.FeeType },
                new SqlParameter("@Amount", SqlDbType.Decimal) { Value = entity.Amount },
                new SqlParameter("@PaymentMethod", SqlDbType.NVarChar, 30) { Value = entity.PaymentMethod ?? "CASH" },
                new SqlParameter("@CollectedByUserId", SqlDbType.Int) { Value = entity.CollectedByUserId > 0 ? entity.CollectedByUserId : 1 },
                new SqlParameter("@Notes", SqlDbType.NVarChar, 250) { Value = (object)entity.Notes ?? DBNull.Value }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters, transaction);
            return Convert.ToInt32(result);
        }

        public virtual List<ServiceFeeReceiptEntity> GetReceiptsByMemberId(int memberId)
        {
            var list = new List<ServiceFeeReceiptEntity>();
            string sql = @"
                SELECT ReceiptId, ReceiptCode, MemberId, FeeType, Amount, PaymentMethod, PaymentDate, CollectedByUserId, Notes
                FROM dbo.ServiceFeeReceipts
                WHERE MemberId = @MemberId
                ORDER BY ReceiptId DESC";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new ServiceFeeReceiptEntity
                {
                    ReceiptId = Convert.ToInt32(row["ReceiptId"]),
                    ReceiptCode = row["ReceiptCode"].ToString(),
                    MemberId = Convert.ToInt32(row["MemberId"]),
                    FeeType = row["FeeType"].ToString(),
                    Amount = Convert.ToDecimal(row["Amount"]),
                    PaymentMethod = row["PaymentMethod"].ToString(),
                    PaymentDate = Convert.ToDateTime(row["PaymentDate"]),
                    CollectedByUserId = Convert.ToInt32(row["CollectedByUserId"]),
                    Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
                });
            }

            return list;
        }
    }
}
