using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể ánh xạ bảng Biên Lai Thu Tiền Phạt Và Đền Bù (FineReceipts)
    /// </summary>
    public class FineReceiptEntity
    {
        public int ReceiptId { get; set; }
        public string ReceiptCode { get; set; }
        public int MemberId { get; set; }
        public int? BorrowSlipId { get; set; }
        public int CollectedByUserId { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } // DEPOSIT_DEDUCTION, CASH
        public string Reason { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Notes { get; set; }
    }
}
