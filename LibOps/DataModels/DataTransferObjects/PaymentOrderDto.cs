using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// Data Transfer Object đại diện cho một đơn thanh toán nạp tiền / dịch vụ VietQR
    /// </summary>
    public class PaymentOrderDto
    {
        public long OrderCode { get; set; }
        public int MemberId { get; set; }
        public string MemberCardCode { get; set; }
        public string MemberFullName { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public string ActionType { get; set; } // TOP_UP, RENEW, FINE_PAYMENT
        public string QrCodeText { get; set; }
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public string BankName { get; set; }
        public string PaymentLinkId { get; set; }
        public string Status { get; set; } // PENDING, PAID, CANCELLED, EXPIRED
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
