using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataModels.Entities;

namespace LibOps.DataAccessLayer.DataRepositories
{
    /// <summary>
    /// Repository thao tác CSDL với bảng Chi Tiết Phiếu Mượn (BorrowSlipDetails)
    /// </summary>
    public class BorrowDetailRepository
    {
        public virtual int InsertBorrowSlipDetail(BorrowSlipDetailEntity detail, SqlTransaction transaction = null)
        {
            string sql = @"
                INSERT INTO dbo.BorrowSlipDetails (BorrowSlipId, CopyId, BorrowConditionNote)
                VALUES (@BorrowSlipId, @CopyId, @BorrowConditionNote);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new[]
            {
                new SqlParameter("@BorrowSlipId", SqlDbType.Int) { Value = detail.BorrowSlipId },
                new SqlParameter("@CopyId", SqlDbType.Int) { Value = detail.CopyId },
                new SqlParameter("@BorrowConditionNote", SqlDbType.NVarChar, 250) { Value = (object)detail.BorrowConditionNote ?? DBNull.Value }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters, transaction);
            return Convert.ToInt32(result);
        }

        public virtual List<BorrowSlipDetailEntity> GetDetailsByBorrowSlipId(int borrowSlipId)
        {
            var list = new List<BorrowSlipDetailEntity>();
            string sql = @"
                SELECT DetailId, BorrowSlipId, CopyId, BorrowConditionNote
                FROM dbo.BorrowSlipDetails
                WHERE BorrowSlipId = @BorrowSlipId";

            var parameters = new[]
            {
                new SqlParameter("@BorrowSlipId", SqlDbType.Int) { Value = borrowSlipId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new BorrowSlipDetailEntity
                {
                    DetailId = Convert.ToInt32(row["DetailId"]),
                    BorrowSlipId = Convert.ToInt32(row["BorrowSlipId"]),
                    CopyId = Convert.ToInt32(row["CopyId"]),
                    BorrowConditionNote = row["BorrowConditionNote"] != DBNull.Value ? row["BorrowConditionNote"].ToString() : string.Empty
                });
            }

            return list;
        }
    }
}
