using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể ánh xạ bảng Giao Dịch Tiền Cọc Thế Chân (DepositTransactions)
    /// </summary>
    public class DepositTransactionEntity
    {
        public int TransactionId { get; set; }
        public int MemberId { get; set; }
        public int HandledByUserId { get; set; }
        public string TransactionType { get; set; } // INITIAL_DEPOSIT, TOP_UP, FINE_DEDUCTION, REFUND
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string ReceiptCode { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Notes { get; set; }
    }
}
