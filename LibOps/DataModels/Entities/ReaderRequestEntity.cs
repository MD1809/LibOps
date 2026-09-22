using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể ánh xạ bảng ReaderRequests (Yêu cầu tự phục vụ của Độc giả)
    /// </summary>
    public class ReaderRequestEntity
    {
        public int RequestId { get; set; }
        public string RequestCode { get; set; }
        public int MemberId { get; set; }
        public string RequestType { get; set; } // REFUND_DEPOSIT, CANCEL_CARD
        public string Status { get; set; } // PENDING, APPROVED, REJECTED, CANCELLED
        public decimal Amount { get; set; }
        public string PayoutMethod { get; set; } // BANK_TRANSFER, CASH
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
        public string BankAccountHolder { get; set; }
        public string Reason { get; set; }
        public DateTime RequestDate { get; set; }
        public int? ProcessedByUserId { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string StaffNotes { get; set; }

        public ReaderRequestEntity()
        {
            RequestDate = DateTime.Now;
            Status = "PENDING";
            PayoutMethod = "BANK_TRANSFER";
        }
    }
}
