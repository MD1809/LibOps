using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// Đối tượng DTO hiển thị danh sách độc giả trên DataGridView (MemberListForm)
    /// </summary>
    public class MemberGridDisplayDto
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
        public int CurrentlyBorrowingCount { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string CardStatus { get; set; }
        public string CardStatusDisplay { get; set; }
        public string Notes { get; set; }

        public string ExpiryDateFormatted => ExpiryDate != DateTime.MinValue ? ExpiryDate.ToString("dd/MM/yyyy") : string.Empty;
        public string IssueDateFormatted => IssueDate != DateTime.MinValue ? IssueDate.ToString("dd/MM/yyyy") : string.Empty;
        public string DateOfBirthFormatted => DateOfBirth.HasValue ? DateOfBirth.Value.ToString("dd/MM/yyyy") : string.Empty;
        public string DepositBalanceFormatted => DepositBalance.ToString("N0") + " VNĐ";
        public string TotalDebtFormatted => TotalDebt > 0 ? $"{TotalDebt:N0} VNĐ" : "0 VNĐ";
    }
}
