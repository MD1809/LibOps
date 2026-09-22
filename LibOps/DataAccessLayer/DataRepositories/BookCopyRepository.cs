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
    /// Repository thao tác CSDL với bảng Bản Sao Sách Và Mã Vạch (BookCopies)
    /// </summary>
    public class BookCopyRepository
    {
        public virtual List<BookCopyGridDisplayDto> GetCopiesByBookId(int bookId)
        {
            var list = new List<BookCopyGridDisplayDto>();
            string sql = @"
                SELECT c.CopyId, c.BookId, b.Title AS BookTitle, c.Barcode, 
                       c.Status, c.ConditionNote, c.AddedDate
                FROM dbo.BookCopies c
                INNER JOIN dbo.Books b ON c.BookId = b.BookId
                WHERE c.BookId = @BookId
                ORDER BY c.CopyId ASC";

            var parameters = new[]
            {
                new SqlParameter("@BookId", SqlDbType.Int) { Value = bookId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            foreach (DataRow row in table.Rows)
            {
                string status = row["Status"].ToString();
                string statusDisplay;
                switch (status)
                {
                    case "AVAILABLE":
                        statusDisplay = "Sẵn sàng";
                        break;
                    case "BORROWED":
                        statusDisplay = "Đang mượn";
                        break;
                    case "DAMAGED":
                        statusDisplay = "Hư hỏng";
                        break;
                    case "LOST":
                        statusDisplay = "Đã mất";
                        break;
                    default:
                        statusDisplay = status;
                        break;
                }

                list.Add(new BookCopyGridDisplayDto
                {
                    IsSelected = false,
                    CopyId = Convert.ToInt32(row["CopyId"]),
                    BookId = Convert.ToInt32(row["BookId"]),
                    BookTitle = row["BookTitle"].ToString(),
                    Barcode = row["Barcode"].ToString(),
                    Status = status,
                    StatusDisplay = statusDisplay,
                    ConditionNote = row["ConditionNote"] != DBNull.Value ? row["ConditionNote"].ToString() : string.Empty,
                    AddedDate = Convert.ToDateTime(row["AddedDate"])
                });
            }

            return list;
        }

        public virtual BookCopyEntity GetCopyByBarcode(string barcode)
        {
            string sql = @"
                SELECT CopyId, BookId, Barcode, Status, ConditionNote, AddedDate
                FROM dbo.BookCopies
                WHERE Barcode = @Barcode";

            var parameters = new[]
            {
                new SqlParameter("@Barcode", SqlDbType.NVarChar, 50) { Value = barcode.Trim() }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            DataRow row = table.Rows[0];
            return new BookCopyEntity
            {
                CopyId = Convert.ToInt32(row["CopyId"]),
                BookId = Convert.ToInt32(row["BookId"]),
                Barcode = row["Barcode"].ToString(),
                Status = row["Status"].ToString(),
                ConditionNote = row["ConditionNote"] != DBNull.Value ? row["ConditionNote"].ToString() : string.Empty,
                AddedDate = Convert.ToDateTime(row["AddedDate"])
            };
        }

        public virtual BookCopyEntity GetCopyById(int copyId)
        {
            string sql = @"
                SELECT CopyId, BookId, Barcode, Status, ConditionNote, AddedDate
                FROM dbo.BookCopies
                WHERE CopyId = @CopyId";

            var parameters = new[]
            {
                new SqlParameter("@CopyId", SqlDbType.Int) { Value = copyId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            DataRow row = table.Rows[0];
            return new BookCopyEntity
            {
                CopyId = Convert.ToInt32(row["CopyId"]),
                BookId = Convert.ToInt32(row["BookId"]),
                Barcode = row["Barcode"].ToString(),
                Status = row["Status"].ToString(),
                ConditionNote = row["ConditionNote"] != DBNull.Value ? row["ConditionNote"].ToString() : string.Empty,
                AddedDate = Convert.ToDateTime(row["AddedDate"])
            };
        }

        public virtual bool IsBarcodeExists(string barcode, int excludeCopyId = 0)
        {
            string sql = "SELECT COUNT(*) FROM dbo.BookCopies WHERE Barcode = @Barcode AND CopyId != @ExcludeCopyId";
            var parameters = new[]
            {
                new SqlParameter("@Barcode", SqlDbType.NVarChar, 50) { Value = barcode.Trim() },
                new SqlParameter("@ExcludeCopyId", SqlDbType.Int) { Value = excludeCopyId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result) > 0;
        }

        public virtual int InsertCopy(BookCopyEntity entity)
        {
            string sql = @"
                INSERT INTO dbo.BookCopies (BookId, Barcode, Status, ConditionNote, AddedDate)
                VALUES (@BookId, @Barcode, @Status, @ConditionNote, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new[]
            {
                new SqlParameter("@BookId", SqlDbType.Int) { Value = entity.BookId },
                new SqlParameter("@Barcode", SqlDbType.NVarChar, 50) { Value = entity.Barcode.Trim() },
                new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = entity.Status },
                new SqlParameter("@ConditionNote", SqlDbType.NVarChar, 250) { Value = (object)entity.ConditionNote ?? DBNull.Value }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result);
        }

        public virtual bool UpdateCopy(BookCopyEntity entity)
        {
            string sql = @"
                UPDATE dbo.BookCopies
                SET Barcode = @Barcode, Status = @Status, ConditionNote = @ConditionNote
                WHERE CopyId = @CopyId";

            var parameters = new[]
            {
                new SqlParameter("@Barcode", SqlDbType.NVarChar, 50) { Value = entity.Barcode.Trim() },
                new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = entity.Status },
                new SqlParameter("@ConditionNote", SqlDbType.NVarChar, 250) { Value = (object)entity.ConditionNote ?? DBNull.Value },
                new SqlParameter("@CopyId", SqlDbType.Int) { Value = entity.CopyId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters);
            return rows > 0;
        }

        public virtual bool UpdateCopyStatus(int copyId, string status, SqlTransaction transaction = null)
        {
            string sql = "UPDATE dbo.BookCopies SET Status = @Status WHERE CopyId = @CopyId";
            var parameters = new[]
            {
                new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = status },
                new SqlParameter("@CopyId", SqlDbType.Int) { Value = copyId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction);
            return rows > 0;
        }

        public virtual bool DeleteCopy(int copyId)
        {
            string sql = "DELETE FROM dbo.BookCopies WHERE CopyId = @CopyId";
            var parameters = new[]
            {
                new SqlParameter("@CopyId", SqlDbType.Int) { Value = copyId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters);
            return rows > 0;
        }

        public virtual int GetNextCopyIndex(int bookId)
        {
            string sql = "SELECT COUNT(*) FROM dbo.BookCopies WHERE BookId = @BookId";
            var parameters = new[]
            {
                new SqlParameter("@BookId", SqlDbType.Int) { Value = bookId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result) + 1;
        }
    }
}
