using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể ánh xạ bảng Độc Giả / Thẻ Thư Viện (Members)
    /// </summary>
    public class MemberEntity
    {
        public int MemberId { get; set; }
        public string MemberCardCode { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string IdentityCardNumber { get; set; }
        public string Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public decimal DepositBalance { get; set; }
        public decimal TotalDebt { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string CardStatus { get; set; } // ACTIVE, LOCKED, CLOSED
        public string Notes { get; set; }
    }
}
