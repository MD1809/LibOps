using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// DTO đại diện cho một cuốn sách được chọn trong danh sách mượn tại quầy (CreateBorrowSlipView)
    /// </summary>
    public class BorrowItemDisplayDto
    {
        public int SequenceNumber { get; set; }
        public int CopyId { get; set; }
        public int BookId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string BookTitle => Title;
        public string CategoryName { get; set; } = string.Empty;
        public string ShelfLocation { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ConditionNote { get; set; } = "Nguyên vẹn";
        public string InitialCondition => string.IsNullOrWhiteSpace(ConditionNote) ? "Nguyên vẹn" : ConditionNote;
        public string ConditionDisplay => string.IsNullOrWhiteSpace(ConditionNote) ? "Nguyên vẹn" : ConditionNote;
    }
}
