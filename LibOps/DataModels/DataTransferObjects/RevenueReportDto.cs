using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// DTO tổng hợp Doanh thu Và Dòng tiền (Mở thẻ, Nạp thẻ, Phạt, Hoàn cọc)
    /// </summary>
    public class RevenueReportSummaryDto
    {
        public decimal TotalFineCollected { get; set; }
        public decimal FineFromDepositDeduction { get; set; }
        public decimal FineFromCash { get; set; }
        public decimal TotalCardIssuanceFee { get; set; }
        public decimal TotalRenewalFee { get; set; }
        public decimal TotalDepositInitial { get; set; }
        public decimal TotalDepositTopUp { get; set; }
        public decimal TotalDepositRefunded { get; set; }
        public decimal CurrentDepositFundBalance { get; set; }

        public decimal TotalInflow => TotalCardIssuanceFee + TotalRenewalFee + TotalDepositInitial + TotalDepositTopUp + TotalFineCollected;
        public decimal TotalOutflow => TotalDepositRefunded;
        public decimal NetCashFlow => TotalInflow - TotalOutflow;
    }

    /// <summary>
    /// DTO từng dòng giao dịch dòng tiền tài chính
    /// </summary>
    public class FinancialTransactionDisplayDto
    {
        public int SequenceNumber { get; set; }
        public string TransactionCategory { get; set; } // "THU PHẠT", "MỞ THẺ", "NẠP THẺ", "HOÀN CỌC"
        public string Code { get; set; }
        public string ReceiptCode => Code;
        public string MemberCardCode { get; set; }
        public string MemberFullName { get; set; }
        public string RawTransactionType { get; set; }
        public string TransactionTypeDisplay { get; set; }
        public decimal Amount { get; set; }
        public bool IsInflow { get; set; } = true;
        public string FormattedAmount => (IsInflow ? "+ " : "- ") + $"{Amount:N0} đ";
        public string PaymentMethod { get; set; }
        public DateTime TransactionDate { get; set; }
        public string HandledByUserName { get; set; }
        public string Notes { get; set; }
    }
}
