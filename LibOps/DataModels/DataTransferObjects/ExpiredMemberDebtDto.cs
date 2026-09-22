using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// DTO đối soát và cấn trừ nợ đối với độc giả có thẻ hết hạn còn nợ phạt
    /// </summary>
    public class ExpiredMemberDebtDto
    {
        public int SequenceNumber { get; set; }
        public int MemberId { get; set; }
        public string MemberCardCode { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string CardStatus { get; set; }

        public decimal TotalDebt { get; set; }
        public decimal DepositBalance { get; set; }
        public int CurrentlyBorrowingCount { get; set; }

        // Tính toán tự động
        public int OverdueDays => Math.Max(0, (DateTime.Today - ExpiryDate.Date).Days);
        public string ExpiryDisplay => $"{ExpiryDate:dd/MM/yyyy} (Quá {OverdueDays} ngày)";

        // Quy tắc an toàn: Chỉ cho phép cấn trừ khi Sách Đang Giữ == 0 và có nợ > 0 và có cọc > 0
        public bool IsBorrowingLockout => CurrentlyBorrowingCount > 0;
        public bool CanSettleDebt => CurrentlyBorrowingCount == 0 && TotalDebt > 0 && DepositBalance > 0;

        public decimal SettlementAmount => Math.Min(DepositBalance, TotalDebt);
        public decimal RemainingDepositAfterSettlement => DepositBalance - SettlementAmount;
        public decimal RemainingDebtAfterSettlement => TotalDebt - SettlementAmount;

        public string ActionButtonText => CurrentlyBorrowingCount > 0 ? "Đang giữ sách" : (DepositBalance <= 0 ? "Hết tiền cọc" : "Cấn Trừ Nợ");
        public string ActionTooltip => CurrentlyBorrowingCount > 0 
            ? $"Độc giả đang giữ {CurrentlyBorrowingCount} cuốn sách chưa trả. Bắt buộc phải thu hồi sách hoặc báo mất sách trước khi cấn trừ cọc!" 
            : $"Cấn trừ {SettlementAmount:N0} đ vào tiền cọc";
    }
}
