using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataModels.Entities;

namespace LibOps.DataAccessLayer.DataRepositories
{
    /// <summary>
    /// Repository thao tác CSDL với bảng Thể Loại (Categories)
    /// </summary>
    public class CategoryRepository
    {
        public virtual List<CategoryEntity> GetAllCategories(string keyword = null)
        {
            var list = new List<CategoryEntity>();
            string sql = @"
                SELECT c.CategoryId, c.CategoryName, c.Description, 
                       COUNT(b.BookId) AS TotalBooks
                FROM dbo.Categories c
                LEFT JOIN dbo.Books b ON c.CategoryId = b.CategoryId
                WHERE (@Keyword IS NULL OR c.CategoryName LIKE @SearchPattern OR c.Description LIKE @SearchPattern)
                GROUP BY c.CategoryId, c.CategoryName, c.Description
                ORDER BY c.CategoryName ASC";

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
                list.Add(new CategoryEntity
                {
                    CategoryId = Convert.ToInt32(row["CategoryId"]),
                    CategoryName = row["CategoryName"].ToString(),
                    Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : string.Empty,
                    TotalBooks = Convert.ToInt32(row["TotalBooks"])
                });
            }

            return list;
        }

        public virtual CategoryEntity GetCategoryById(int categoryId)
        {
            string sql = @"
                SELECT c.CategoryId, c.CategoryName, c.Description, 
                       COUNT(b.BookId) AS TotalBooks
                FROM dbo.Categories c
                LEFT JOIN dbo.Books b ON c.CategoryId = b.CategoryId
                WHERE c.CategoryId = @CategoryId
                GROUP BY c.CategoryId, c.CategoryName, c.Description";

            var parameters = new[]
            {
                new SqlParameter("@CategoryId", SqlDbType.Int) { Value = categoryId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            DataRow row = table.Rows[0];
            return new CategoryEntity
            {
                CategoryId = Convert.ToInt32(row["CategoryId"]),
                CategoryName = row["CategoryName"].ToString(),
                Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : string.Empty,
                TotalBooks = Convert.ToInt32(row["TotalBooks"])
            };
        }

        public virtual bool IsCategoryNameExists(string categoryName, int excludeCategoryId = 0)
        {
            string sql = @"
                SELECT COUNT(*) 
                FROM dbo.Categories 
                WHERE LOWER(CategoryName) = LOWER(@CategoryName) AND CategoryId != @ExcludeCategoryId";

            var parameters = new[]
            {
                new SqlParameter("@CategoryName", SqlDbType.NVarChar, 100) { Value = categoryName.Trim() },
                new SqlParameter("@ExcludeCategoryId", SqlDbType.Int) { Value = excludeCategoryId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result) > 0;
        }

        public virtual int InsertCategory(CategoryEntity entity)
        {
            string sql = @"
                INSERT INTO dbo.Categories (CategoryName, Description)
                VALUES (@CategoryName, @Description);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new[]
            {
                new SqlParameter("@CategoryName", SqlDbType.NVarChar, 100) { Value = entity.CategoryName.Trim() },
                new SqlParameter("@Description", SqlDbType.NVarChar, 250) { Value = (object)entity.Description ?? DBNull.Value }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result);
        }

        public virtual bool UpdateCategory(CategoryEntity entity)
        {
            string sql = @"
                UPDATE dbo.Categories 
                SET CategoryName = @CategoryName, Description = @Description
                WHERE CategoryId = @CategoryId";

            var parameters = new[]
            {
                new SqlParameter("@CategoryName", SqlDbType.NVarChar, 100) { Value = entity.CategoryName.Trim() },
                new SqlParameter("@Description", SqlDbType.NVarChar, 250) { Value = (object)entity.Description ?? DBNull.Value },
                new SqlParameter("@CategoryId", SqlDbType.Int) { Value = entity.CategoryId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters);
            return rows > 0;
        }

        public virtual bool DeleteCategory(int categoryId)
        {
            string sql = "DELETE FROM dbo.Categories WHERE CategoryId = @CategoryId";
            var parameters = new[]
            {
                new SqlParameter("@CategoryId", SqlDbType.Int) { Value = categoryId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters);
            return rows > 0;
        }

        public virtual int CountBooksInCategory(int categoryId)
        {
            string sql = "SELECT COUNT(*) FROM dbo.Books WHERE CategoryId = @CategoryId";
            var parameters = new[]
            {
                new SqlParameter("@CategoryId", SqlDbType.Int) { Value = categoryId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result);
        }
    }
}
