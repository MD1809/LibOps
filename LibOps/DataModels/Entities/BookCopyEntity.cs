using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể ánh xạ bảng Bản Sao Sách Và Mã Vạch (BookCopies)
    /// </summary>
    public class BookCopyEntity
    {
        public int CopyId { get; set; }
        public int BookId { get; set; }
        public string Barcode { get; set; }
        public string Status { get; set; } // AVAILABLE, BORROWED, DAMAGED, LOST
        public string ConditionNote { get; set; }
        public DateTime AddedDate { get; set; }
    }
}
