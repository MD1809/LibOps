using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể ánh xạ bảng Chi Tiết Trả Sách (ReturnSlipDetails)
    /// </summary>
    public class ReturnSlipDetailEntity
    {
        public int ReturnDetailId { get; set; }
        public int BorrowSlipDetailId { get; set; }
        public int ReceivedByUserId { get; set; }
        public DateTime ActualReturnDate { get; set; }
        public int OverdueDays { get; set; }
        public decimal FineAmount { get; set; }
        public string ReturnConditionNote { get; set; }
        public string BookCopyStatusAfterReturn { get; set; } // AVAILABLE, DAMAGED, LOST
    }
}
