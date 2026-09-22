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
    /// Repository thao tác CSDL với bảng Đầu Sách (Books)
    /// </summary>
    public class BookRepository
    {
        public virtual List<BookGridDisplayDto> GetAllBooks(string keyword = null, int? categoryId = null, int? authorId = null, int? publisherId = null)
        {
            var list = new List<BookGridDisplayDto>();
            string sql = @"
                SELECT b.BookId, b.ISBN, b.Title, b.CategoryId, c.CategoryName, 
                       b.AuthorId, 
                       COALESCE(
                           (SELECT STRING_AGG(a2.AuthorName, ', ') 
                            FROM dbo.BookAuthors ba2 
                            JOIN dbo.Authors a2 ON ba2.AuthorId = a2.AuthorId 
                            WHERE ba2.BookId = b.BookId), 
                           a.AuthorName, 
                           N'Chưa rõ'
                       ) AS AuthorName, 
                       b.PublisherId, p.PublisherName, 
                       b.PublishYear, b.Price, b.TotalQuantity, b.AvailableQuantity, 
                       b.ShelfLocation, b.CoverImagePath, b.Summary
                FROM dbo.Books b
                LEFT JOIN dbo.Categories c ON b.CategoryId = c.CategoryId
                LEFT JOIN dbo.Authors a ON b.AuthorId = a.AuthorId
                LEFT JOIN dbo.Publishers p ON b.PublisherId = p.PublisherId
                WHERE (@Keyword IS NULL 
                       OR b.Title LIKE @SearchPattern 
                       OR b.ISBN LIKE @SearchPattern 
                       OR a.AuthorName LIKE @SearchPattern 
                       OR EXISTS (SELECT 1 FROM dbo.BookAuthors ba3 JOIN dbo.Authors a3 ON ba3.AuthorId = a3.AuthorId WHERE ba3.BookId = b.BookId AND a3.AuthorName LIKE @SearchPattern)
                       OR b.ShelfLocation LIKE @SearchPattern)
                  AND (@CategoryId IS NULL OR @CategoryId = 0 OR b.CategoryId = @CategoryId)
                  AND (@AuthorId IS NULL OR @AuthorId = 0 OR b.AuthorId = @AuthorId OR EXISTS (SELECT 1 FROM dbo.BookAuthors ba4 WHERE ba4.BookId = b.BookId AND ba4.AuthorId = @AuthorId))
                  AND (@PublisherId IS NULL OR @PublisherId = 0 OR b.PublisherId = @PublisherId)
                ORDER BY b.BookId DESC";

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
                new SqlParameter("@CategoryId", SqlDbType.Int) 
                { 
                    Value = categoryId.HasValue && categoryId.Value > 0 ? (object)categoryId.Value : DBNull.Value 
                },
                new SqlParameter("@AuthorId", SqlDbType.Int) 
                { 
                    Value = authorId.HasValue && authorId.Value > 0 ? (object)authorId.Value : DBNull.Value 
                },
                new SqlParameter("@PublisherId", SqlDbType.Int) 
                { 
                    Value = publisherId.HasValue && publisherId.Value > 0 ? (object)publisherId.Value : DBNull.Value 
                }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new BookGridDisplayDto
                {
                    BookId = Convert.ToInt32(row["BookId"]),
                    ISBN = row["ISBN"] != DBNull.Value ? row["ISBN"].ToString() : string.Empty,
                    Title = row["Title"].ToString(),
                    CategoryId = Convert.ToInt32(row["CategoryId"]),
                    CategoryName = row["CategoryName"] != DBNull.Value ? row["CategoryName"].ToString() : "Chưa phân loại",
                    AuthorId = Convert.ToInt32(row["AuthorId"]),
                    AuthorName = row["AuthorName"] != DBNull.Value ? row["AuthorName"].ToString() : "Chưa rõ",
                    PublisherId = Convert.ToInt32(row["PublisherId"]),
                    PublisherName = row["PublisherName"] != DBNull.Value ? row["PublisherName"].ToString() : "Chưa rõ",
                    PublishYear = row["PublishYear"] != DBNull.Value ? Convert.ToInt32(row["PublishYear"]) : 0,
                    Price = Convert.ToDecimal(row["Price"]),
                    TotalQuantity = Convert.ToInt32(row["TotalQuantity"]),
                    AvailableQuantity = Convert.ToInt32(row["AvailableQuantity"]),
                    ShelfLocation = row["ShelfLocation"] != DBNull.Value ? row["ShelfLocation"].ToString() : string.Empty,
                    CoverImagePath = row["CoverImagePath"] != DBNull.Value ? row["CoverImagePath"].ToString() : string.Empty,
                    Summary = row["Summary"] != DBNull.Value ? row["Summary"].ToString() : string.Empty
                });
            }

            return list;
        }

        public virtual BookEntity GetBookById(int bookId)
        {
            string sql = @"
                SELECT BookId, ISBN, Title, CategoryId, AuthorId, PublisherId, 
                       PublishYear, Price, TotalQuantity, AvailableQuantity, 
                       ShelfLocation, CoverImagePath, Summary
                FROM dbo.Books
                WHERE BookId = @BookId";

            var parameters = new[]
            {
                new SqlParameter("@BookId", SqlDbType.Int) { Value = bookId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            DataRow row = table.Rows[0];
            return new BookEntity
            {
                BookId = Convert.ToInt32(row["BookId"]),
                ISBN = row["ISBN"] != DBNull.Value ? row["ISBN"].ToString() : string.Empty,
                Title = row["Title"].ToString(),
                CategoryId = Convert.ToInt32(row["CategoryId"]),
                AuthorId = Convert.ToInt32(row["AuthorId"]),
                PublisherId = Convert.ToInt32(row["PublisherId"]),
                PublishYear = row["PublishYear"] != DBNull.Value ? Convert.ToInt32(row["PublishYear"]) : 0,
                Price = Convert.ToDecimal(row["Price"]),
                TotalQuantity = Convert.ToInt32(row["TotalQuantity"]),
                AvailableQuantity = Convert.ToInt32(row["AvailableQuantity"]),
                ShelfLocation = row["ShelfLocation"] != DBNull.Value ? row["ShelfLocation"].ToString() : string.Empty,
                CoverImagePath = row["CoverImagePath"] != DBNull.Value ? row["CoverImagePath"].ToString() : string.Empty,
                Summary = row["Summary"] != DBNull.Value ? row["Summary"].ToString() : string.Empty
            };
        }

        public virtual List<int> GetAuthorIdsByBookId(int bookId)
        {
            var list = new List<int>();
            string sql = "SELECT AuthorId FROM dbo.BookAuthors WHERE BookId = @BookId";
            var parameters = new[]
            {
                new SqlParameter("@BookId", SqlDbType.Int) { Value = bookId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            foreach (DataRow row in table.Rows)
            {
                list.Add(Convert.ToInt32(row["AuthorId"]));
            }

            if (list.Count == 0)
            {
                var book = GetBookById(bookId);
                if (book != null && book.AuthorId > 0)
                {
                    list.Add(book.AuthorId);
                }
            }

            return list;
        }

        public virtual List<AuthorEntity> GetAuthorsByBookId(int bookId)
        {
            var list = new List<AuthorEntity>();
            string sql = @"
                SELECT a.AuthorId, a.AuthorName, a.Notes
                FROM dbo.BookAuthors ba
                JOIN dbo.Authors a ON ba.AuthorId = a.AuthorId
                WHERE ba.BookId = @BookId
                ORDER BY a.AuthorName ASC";

            var parameters = new[]
            {
                new SqlParameter("@BookId", SqlDbType.Int) { Value = bookId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new AuthorEntity
                {
                    AuthorId = Convert.ToInt32(row["AuthorId"]),
                    AuthorName = row["AuthorName"].ToString(),
                    Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : string.Empty
                });
            }

            if (list.Count == 0)
            {
                var book = GetBookById(bookId);
                if (book != null && book.AuthorId > 0)
                {
                    string authorSql = "SELECT AuthorId, AuthorName, Notes FROM dbo.Authors WHERE AuthorId = @AuthorId";
                    var authorParam = new[] { new SqlParameter("@AuthorId", SqlDbType.Int) { Value = book.AuthorId } };
                    DataTable dt = DatabaseConnectionHelper.ExecuteQueryToDataTable(authorSql, authorParam);
                    if (dt.Rows.Count > 0)
                    {
                        list.Add(new AuthorEntity
                        {
                            AuthorId = Convert.ToInt32(dt.Rows[0]["AuthorId"]),
                            AuthorName = dt.Rows[0]["AuthorName"].ToString(),
                            Notes = dt.Rows[0]["Notes"] != DBNull.Value ? dt.Rows[0]["Notes"].ToString() : string.Empty
                        });
                    }
                }
            }

            return list;
        }

        public virtual bool IsIsbnExists(string isbn, int excludeBookId = 0)
        {
            if (string.IsNullOrWhiteSpace(isbn))
            {
                return false;
            }

            string sql = "SELECT COUNT(*) FROM dbo.Books WHERE ISBN = @ISBN AND BookId != @ExcludeBookId";
            var parameters = new[]
            {
                new SqlParameter("@ISBN", SqlDbType.NVarChar, 30) { Value = isbn.Trim() },
                new SqlParameter("@ExcludeBookId", SqlDbType.Int) { Value = excludeBookId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result) > 0;
        }

        public virtual int InsertBook(BookEntity entity, List<int> authorIds = null)
        {
            if (authorIds != null && authorIds.Count > 0)
            {
                entity.AuthorId = authorIds[0];
            }

            string sql = @"
                INSERT INTO dbo.Books (ISBN, Title, CategoryId, AuthorId, PublisherId, PublishYear, Price, TotalQuantity, AvailableQuantity, ShelfLocation, CoverImagePath, Summary)
                VALUES (@ISBN, @Title, @CategoryId, @AuthorId, @PublisherId, @PublishYear, @Price, @TotalQuantity, @AvailableQuantity, @ShelfLocation, @CoverImagePath, @Summary);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new[]
            {
                new SqlParameter("@ISBN", SqlDbType.NVarChar, 30) { Value = (object)entity.ISBN ?? DBNull.Value },
                new SqlParameter("@Title", SqlDbType.NVarChar, 250) { Value = entity.Title.Trim() },
                new SqlParameter("@CategoryId", SqlDbType.Int) { Value = entity.CategoryId },
                new SqlParameter("@AuthorId", SqlDbType.Int) { Value = entity.AuthorId },
                new SqlParameter("@PublisherId", SqlDbType.Int) { Value = entity.PublisherId },
                new SqlParameter("@PublishYear", SqlDbType.Int) { Value = entity.PublishYear },
                new SqlParameter("@Price", SqlDbType.Decimal) { Value = entity.Price },
                new SqlParameter("@TotalQuantity", SqlDbType.Int) { Value = entity.TotalQuantity },
                new SqlParameter("@AvailableQuantity", SqlDbType.Int) { Value = entity.AvailableQuantity },
                new SqlParameter("@ShelfLocation", SqlDbType.NVarChar, 100) { Value = (object)entity.ShelfLocation ?? DBNull.Value },
                new SqlParameter("@CoverImagePath", SqlDbType.NVarChar, 500) { Value = (object)entity.CoverImagePath ?? DBNull.Value },
                new SqlParameter("@Summary", SqlDbType.NVarChar, -1) { Value = (object)entity.Summary ?? DBNull.Value }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            int newBookId = Convert.ToInt32(result);

            if (newBookId > 0)
            {
                SaveBookAuthors(newBookId, authorIds ?? (entity.AuthorId > 0 ? new List<int> { entity.AuthorId } : null));
            }

            return newBookId;
        }

        public virtual bool UpdateBook(BookEntity entity, List<int> authorIds = null)
        {
            if (authorIds != null && authorIds.Count > 0)
            {
                entity.AuthorId = authorIds[0];
            }

            string sql = @"
                UPDATE dbo.Books 
                SET ISBN = @ISBN, Title = @Title, CategoryId = @CategoryId, 
                    AuthorId = @AuthorId, PublisherId = @PublisherId, PublishYear = @PublishYear, 
                    Price = @Price, ShelfLocation = @ShelfLocation, 
                    CoverImagePath = @CoverImagePath, Summary = @Summary
                WHERE BookId = @BookId";

            var parameters = new[]
            {
                new SqlParameter("@ISBN", SqlDbType.NVarChar, 30) { Value = (object)entity.ISBN ?? DBNull.Value },
                new SqlParameter("@Title", SqlDbType.NVarChar, 250) { Value = entity.Title.Trim() },
                new SqlParameter("@CategoryId", SqlDbType.Int) { Value = entity.CategoryId },
                new SqlParameter("@AuthorId", SqlDbType.Int) { Value = entity.AuthorId },
                new SqlParameter("@PublisherId", SqlDbType.Int) { Value = entity.PublisherId },
                new SqlParameter("@PublishYear", SqlDbType.Int) { Value = entity.PublishYear },
                new SqlParameter("@Price", SqlDbType.Decimal) { Value = entity.Price },
                new SqlParameter("@ShelfLocation", SqlDbType.NVarChar, 100) { Value = (object)entity.ShelfLocation ?? DBNull.Value },
                new SqlParameter("@CoverImagePath", SqlDbType.NVarChar, 500) { Value = (object)entity.CoverImagePath ?? DBNull.Value },
                new SqlParameter("@Summary", SqlDbType.NVarChar, -1) { Value = (object)entity.Summary ?? DBNull.Value },
                new SqlParameter("@BookId", SqlDbType.Int) { Value = entity.BookId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters);
            if (rows > 0)
            {
                SaveBookAuthors(entity.BookId, authorIds ?? (entity.AuthorId > 0 ? new List<int> { entity.AuthorId } : null));
                return true;
            }
            return false;
        }

        private void SaveBookAuthors(int bookId, List<int> authorIds)
        {
            try
            {
                string deleteSql = "DELETE FROM dbo.BookAuthors WHERE BookId = @BookId";
                DatabaseConnectionHelper.ExecuteNonQuery(deleteSql, new[] { new SqlParameter("@BookId", SqlDbType.Int) { Value = bookId } });

                if (authorIds != null && authorIds.Count > 0)
                {
                    foreach (int authorId in authorIds)
                    {
                        if (authorId <= 0) continue;
                        string insertSql = @"
                            IF NOT EXISTS (SELECT 1 FROM dbo.BookAuthors WHERE BookId = @BookId AND AuthorId = @AuthorId)
                            BEGIN
                                INSERT INTO dbo.BookAuthors (BookId, AuthorId) VALUES (@BookId, @AuthorId);
                            END";
                        DatabaseConnectionHelper.ExecuteNonQuery(insertSql, new[]
                        {
                            new SqlParameter("@BookId", SqlDbType.Int) { Value = bookId },
                            new SqlParameter("@AuthorId", SqlDbType.Int) { Value = authorId }
                        });
                    }
                }
            }
            catch
            {
            }
        }

        public virtual bool DeleteBook(int bookId)
        {
            string sql = "DELETE FROM dbo.Books WHERE BookId = @BookId";
            var parameters = new[]
            {
                new SqlParameter("@BookId", SqlDbType.Int) { Value = bookId }
            };

            int rows = DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters);
            return rows > 0;
        }

        public virtual void UpdateBookCopyCounts(int bookId, SqlTransaction transaction = null)
        {
            string sql = @"
                UPDATE dbo.Books
                SET TotalQuantity = (SELECT COUNT(*) FROM dbo.BookCopies WHERE BookId = @BookId),
                    AvailableQuantity = (SELECT COUNT(*) FROM dbo.BookCopies WHERE BookId = @BookId AND Status = 'AVAILABLE')
                WHERE BookId = @BookId";

            var parameters = new[]
            {
                new SqlParameter("@BookId", SqlDbType.Int) { Value = bookId }
            };

            DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction);
        }
    }
}
