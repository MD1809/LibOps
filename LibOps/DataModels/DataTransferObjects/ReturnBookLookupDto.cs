using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// DTO chứa thông tin đầy đủ của một lượt mượn khi quét mã sách trả
    /// </summary>
    public class ReturnBookLookupDto
    {
        public int BorrowSlipDetailId { get; set; }
        public int BorrowSlipId { get; set; }
        public string SlipCode { get; set; }
        public int CopyId { get; set; }
        public string Barcode { get; set; }
        public int BookId { get; set; }
        public string Title { get; set; }
        public decimal BookPrice { get; set; }
        public string ShelfLocation { get; set; }
        public int MemberId { get; set; }
        public string MemberCardCode { get; set; }
        public string MemberFullName { get; set; }
        public string MemberPhone { get; set; }
        public decimal MemberDepositBalance { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public string BorrowConditionNote { get; set; }
        public int OverdueDays { get; set; }
        public decimal OverdueFine { get; set; }
    }
}
