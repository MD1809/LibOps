using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể ánh xạ bảng Phiếu Mượn Sách (BorrowSlips)
    /// </summary>
    public class BorrowSlipEntity
    {
        public int BorrowSlipId { get; set; }
        public string SlipCode { get; set; }
        public int MemberId { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public int RenewalCount { get; set; } = 0;
        public string Status { get; set; } // BORROWING, RETURNED, OVERDUE, PARTIALLY_RETURNED
        public string Notes { get; set; }
    }
}
