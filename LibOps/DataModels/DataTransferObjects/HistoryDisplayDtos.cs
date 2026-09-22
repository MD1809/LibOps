using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// DTO hiển thị danh sách lịch sử mượn trả trên lưới BorrowSlipListForm
    /// </summary>
    public class BorrowSlipGridDisplayDto
    {
        public int SequenceNumber { get; set; }
        public string TransactionType { get; set; } = "BORROW"; // "BORROW" | "RETURN"
        public string TransactionTypeDisplay { get; set; } = "Mượn sách"; // "Mượn sách" | "Trả sách"
        public int ReferenceId { get; set; } // BorrowSlipId hoặc ReturnSlipDetailId
        public int BorrowSlipId { get; set; }
        public string SlipCode { get; set; } = string.Empty;
        public int MemberId { get; set; }
        public string MemberCardCode { get; set; } = string.Empty;
        public string MemberFullName { get; set; } = string.Empty;
        public string MemberName => MemberFullName;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public int RenewalCount { get; set; }
        public int BookCount { get; set; }
        public int TotalBooks => BookCount;
        public decimal FineAmount { get; set; }
        public int TotalItems { get; set; }
        public int ReturnedItems { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusDisplay { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO hiển thị chi tiết các cuốn sách trong một phiếu mượn
    /// </summary>
    public class BorrowSlipItemDetailDto
    {
        public int SequenceNumber { get; set; }
        public int BorrowSlipDetailId { get; set; }
        public int CopyId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ShelfLocation { get; set; } = string.Empty;
        public decimal BookPrice { get; set; }
        public string BorrowConditionNote { get; set; } = string.Empty;
        public string ReturnStatus { get; set; } = string.Empty; // BORROWING, AVAILABLE, DAMAGED, LOST
        public string ReturnStatusDisplay { get; set; } = string.Empty;
        public DateTime? ActualReturnDate { get; set; }
        public int OverdueDays { get; set; }
        public decimal FineAmount { get; set; }
        public string ReturnConditionNote { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO hiển thị lịch sử mượn trả của 1 độc giả cụ thể
    /// </summary>
    public class MemberBorrowHistoryDto
    {
        public int SequenceNumber { get; set; }
        public int BorrowSlipDetailId { get; set; }
        public string SlipCode { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public int RenewalCount { get; set; }
        public DateTime? ActualReturnDate { get; set; }
        public int OverdueDays { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public string BookConditionAfterReturn { get; set; } = string.Empty;
        public decimal FineAmount { get; set; }
    }

    /// <summary>
    /// DTO hiển thị lịch sử biến động tiền cọc và nạp tiền của 1 độc giả
    /// </summary>
    public class MemberDepositHistoryDto
    {
        public int SequenceNumber { get; set; }
        public int TransactionId { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public string TransactionTypeDisplay { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime TransactionDate { get; set; }
        public string HandledByUserName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO hiển thị lịch sử các lần bị phạt vi phạm của 1 độc giả
    /// </summary>
    public class MemberFineHistoryDto
    {
        public int SequenceNumber { get; set; }
        public int ReceiptId { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public string SlipCode { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentMethodDisplay { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string CollectedByUserName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
