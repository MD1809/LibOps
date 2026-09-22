using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể ánh xạ bảng quan hệ Sách - Tác giả (BookAuthors)
    /// </summary>
    public class BookAuthorEntity
    {
        public int BookId { get; set; }
        public int AuthorId { get; set; }
    }
}
