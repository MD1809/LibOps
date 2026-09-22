using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataModels.Entities;

namespace LibOps.DataAccessLayer.DataRepositories
{
    /// <summary>
    /// Repository thao tác CSDL với bảng Nhà Xuất Bản (Publishers)
    /// </summary>
    public class PublisherRepository
    {
        public virtual List<PublisherEntity> GetAllPublishers(string keyword = null)
        {
            var list = new List<PublisherEntity>();
            string sql = @"
                SELECT p.PublisherId, p.PublisherName, p.Address, p.PhoneNumber, 
                       COUNT(b.BookId) AS TotalBooks
                FROM dbo.Publishers p
                LEFT JOIN dbo.Books b ON p.PublisherId = b.PublisherId
                WHERE (@Keyword IS NULL OR p.PublisherName LIKE @SearchPattern OR p.Address LIKE @SearchPattern OR p.PhoneNumber LIKE @SearchPattern)
                GROUP BY p.PublisherId, p.PublisherName, p.Address, p.PhoneNumber
                ORDER BY p.PublisherName ASC";

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
                list.Add(new PublisherEntity
                {
                    PublisherId = Convert.ToInt32(row["PublisherId"]),
                    PublisherName = row["PublisherName"].ToString(),
                    Address = row["Address"] != DBNull.Value ? row["Address"].ToString() : string.Empty,
                    PhoneNumber = row["PhoneNumber"] != DBNull.Value ? row["PhoneNumber"].ToString() : string.Empty,
                    TotalBooks = Convert.ToInt32(row["TotalBooks"])
                });
            }

            return list;
        }

        public virtual PublisherEntity GetPublisherById(int publisherId)
        {
            string sql = @"
                SELECT p.PublisherId, p.PublisherName, p.Address, p.PhoneNumber, 
                       COUNT(b.BookId) AS TotalBooks
                FROM dbo.Publishers p
                LEFT JOIN dbo.Books b ON p.PublisherId = b.PublisherId
                WHERE p.PublisherId = @PublisherId
                GROUP BY p.PublisherId, p.PublisherName, p.Address, p.PhoneNumber";

            var parameters = new[]
            {
                new SqlParameter("@PublisherId", SqlDbType.Int) { Value = publisherId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            DataRow row = table.Rows[0];
            return new PublisherEntity
            {
                PublisherId = Convert.ToInt32(row["PublisherId"]),
                PublisherName = row["PublisherName"].ToString(),
                Address = row["Address"] != DBNull.Value ? row["Address"].ToString() : string.Empty,
                PhoneNumber = row["PhoneNumber"] != DBNull.Value ? row["PhoneNumber"].ToString() : string.Empty,
                TotalBooks = Convert.ToInt32(row["TotalBooks"])
            };
        }

        public virtual bool IsPublisherNameExists(string publisherName, int excludePublisherId = 0)
        {
            string sql = @"
                SELECT COUNT(*) 
                FROM dbo.Publishers 
                WHERE LOWER(PublisherName) = LOWER(@PublisherName) AND PublisherId != @ExcludePublisherId";

            var parameters = new[]
            {
                new SqlParameter("@PublisherName", SqlDbType.NVarChar, 100) { Value = publisherName.Trim() },
                new SqlParameter("@ExcludePublisherId", SqlDbType.Int) { Value = excludePublisherId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result) > 0;
        }

        public virtual int InsertPublisher(PublisherEntity entity)
        {
            string sql = @"
                INSERT INTO dbo.Publishers (PublisherName, Address, PhoneNumber)
                VALUES (@PublisherName, @Address, @PhoneNumber);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new[]
            {
                new SqlParameter("@PublisherName", SqlDbType.NVarChar, 100) { Value = entity.PublisherName.Trim() },
                new SqlParameter("@Address", SqlDbType.NVarChar, 250) { Value = (object)entity.Address ?? DBNull.Value },
                new SqlParameter("@PhoneNumber", SqlDbType.NVarChar, 20) { Value = (object)entity.PhoneNumber ?? DBNull.Value }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result);
        }

        public virtual bool UpdatePublisher(PublisherEntity entity)
        {
            string sql = @"
                UPDATE dbo.Publishers 
                SET PublisherName = @PublisherName, Address = @Address, PhoneNumber = @PhoneNumber
                WHERE PublisherId = @PublisherId";

            var parameters = new[]
            {
                new SqlParameter("@PublisherName", SqlDbType.NVarChar, 100) { Value = entity.PublisherName.Trim() },
                new SqlParameter("@Address", SqlDbType.NVarChar, 250) { Value = (object)entity.Address ?? DBNull.Value },
                new SqlParameter("@PhoneNumber", SqlDbType.NVarChar, 20) { Value = (object)entity.PhoneNumber ?? DBNull.Value },
                new SqlParameter("@PublisherId", SqlDbType.Int) { Value = entity.PublisherId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters);
            return rows > 0;
        }

        public virtual bool DeletePublisher(int publisherId)
        {
            string sql = "DELETE FROM dbo.Publishers WHERE PublisherId = @PublisherId";
            var parameters = new[]
            {
                new SqlParameter("@PublisherId", SqlDbType.Int) { Value = publisherId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters);
            return rows > 0;
        }

        public virtual int CountBooksByPublisher(int publisherId)
        {
            string sql = "SELECT COUNT(*) FROM dbo.Books WHERE PublisherId = @PublisherId";
            var parameters = new[]
            {
                new SqlParameter("@PublisherId", SqlDbType.Int) { Value = publisherId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result);
        }
    }
}
