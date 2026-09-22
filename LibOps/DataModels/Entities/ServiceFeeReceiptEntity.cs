using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể ánh xạ bảng Biên Lai Thu Phí Dịch Vụ Thư Viện (ServiceFeeReceipts)
    /// </summary>
    public class ServiceFeeReceiptEntity
    {
        public int ReceiptId { get; set; }
        public string ReceiptCode { get; set; }
        public int MemberId { get; set; }
        public string FeeType { get; set; } // CARD_ISSUANCE, ANNUAL_RENEWAL
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } // CASH, VIETQR, DEPOSIT_DEDUCTION
        public DateTime PaymentDate { get; set; }
        public int CollectedByUserId { get; set; }
        public string Notes { get; set; }
    }
}
