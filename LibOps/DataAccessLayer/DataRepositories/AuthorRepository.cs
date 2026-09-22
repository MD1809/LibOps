using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataModels.Entities;

namespace LibOps.DataAccessLayer.DataRepositories
{
    /// <summary>
    /// Repository thao tác CSDL với bảng Tác Giả (Authors) - Thiết kế tinh gọn
    /// </summary>
    public class AuthorRepository
    {
        public virtual List<AuthorEntity> GetAllAuthors(string keyword = null)
        {
            var list = new List<AuthorEntity>();
            string sql = @"
                SELECT a.AuthorId, a.AuthorName, a.Notes, 
                       COUNT(b.BookId) AS TotalBooks
                FROM dbo.Authors a
                LEFT JOIN dbo.Books b ON a.AuthorId = b.AuthorId
                WHERE (@Keyword IS NULL OR a.AuthorName LIKE @SearchPattern OR a.Notes LIKE @SearchPattern)
                GROUP BY a.AuthorId, a.AuthorName, a.Notes
                ORDER BY a.AuthorName ASC";

            var parameters = new[]
            {
                new SqlParameter("@Keyword", SqlDbType.NVarChar, 100) 
                { 
                    Value = string.IsNullOrWhiteSpace(keyword) ? (object)DBNull.Value : keyword.Trim() 
                },
                new SqlParameter("@SearchPattern", SqlDbType.NVarChar, 102) 
                { 
                    Value = string.IsNullOrWhiteSpace(keyword) ? (object)DBNull.Value : "%" + keyword.Trim() + "%" 
                }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new AuthorEntity
                {
                    AuthorId = Convert.ToInt32(row["AuthorId"]),
                    AuthorName = row["AuthorName"].ToString(),
                    Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty,
                    TotalBooks = Convert.ToInt32(row["TotalBooks"])
                });
            }

            return list;
        }

        public virtual AuthorEntity GetAuthorById(int authorId)
        {
            string sql = @"
                SELECT a.AuthorId, a.AuthorName, a.Notes, 
                       COUNT(b.BookId) AS TotalBooks
                FROM dbo.Authors a
                LEFT JOIN dbo.Books b ON a.AuthorId = b.AuthorId
                WHERE a.AuthorId = @AuthorId
                GROUP BY a.AuthorId, a.AuthorName, a.Notes";

            var parameters = new[]
            {
                new SqlParameter("@AuthorId", SqlDbType.Int) { Value = authorId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            DataRow row = table.Rows[0];
            return new AuthorEntity
            {
                AuthorId = Convert.ToInt32(row["AuthorId"]),
                AuthorName = row["AuthorName"].ToString(),
                Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty,
                TotalBooks = Convert.ToInt32(row["TotalBooks"])
            };
        }

        public virtual bool IsAuthorNameExists(string authorName, int excludeAuthorId = 0)
        {
            string sql = @"
                SELECT COUNT(*) 
                FROM dbo.Authors 
                WHERE LOWER(AuthorName) = LOWER(@AuthorName) AND AuthorId != @ExcludeAuthorId";

            var parameters = new[]
            {
                new SqlParameter("@AuthorName", SqlDbType.NVarChar, 100) { Value = authorName.Trim() },
                new SqlParameter("@ExcludeAuthorId", SqlDbType.Int) { Value = excludeAuthorId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result) > 0;
        }

        public virtual int InsertAuthor(AuthorEntity entity)
        {
            string sql = @"
                INSERT INTO dbo.Authors (AuthorName, Notes)
                VALUES (@AuthorName, @Notes);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new[]
            {
                new SqlParameter("@AuthorName", SqlDbType.NVarChar, 100) { Value = entity.AuthorName.Trim() },
                new SqlParameter("@Notes", SqlDbType.NVarChar, 250) { Value = (object)entity.Notes ?? DBNull.Value }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result);
        }

        public virtual bool UpdateAuthor(AuthorEntity entity)
        {
            string sql = @"
                UPDATE dbo.Authors 
                SET AuthorName = @AuthorName, Notes = @Notes
                WHERE AuthorId = @AuthorId";

            var parameters = new[]
            {
                new SqlParameter("@AuthorName", SqlDbType.NVarChar, 100) { Value = entity.AuthorName.Trim() },
                new SqlParameter("@Notes", SqlDbType.NVarChar, 250) { Value = (object)entity.Notes ?? DBNull.Value },
                new SqlParameter("@AuthorId", SqlDbType.Int) { Value = entity.AuthorId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters);
            return rows > 0;
        }

        public virtual bool DeleteAuthor(int authorId)
        {
            string sql = "DELETE FROM dbo.Authors WHERE AuthorId = @AuthorId";
            var parameters = new[]
            {
                new SqlParameter("@AuthorId", SqlDbType.Int) { Value = authorId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters);
            return rows > 0;
        }

        public virtual int CountBooksByAuthor(int authorId)
        {
            string sql = "SELECT COUNT(*) FROM dbo.Books WHERE AuthorId = @AuthorId";
            var parameters = new[]
            {
                new SqlParameter("@AuthorId", SqlDbType.Int) { Value = authorId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result);
        }
    }
}
