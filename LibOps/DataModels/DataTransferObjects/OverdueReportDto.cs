using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// DTO biểu diễn dữ liệu Báo cáo Độc giả Quá hạn Và Thu hồi sách
    /// </summary>
    public class OverdueReportDto
    {
        public int SequenceNumber { get; set; }
        public int BorrowSlipId { get; set; }
        public string SlipCode { get; set; }
        public string MemberCardCode { get; set; }
        public string MemberFullName { get; set; }
        public string PhoneNumber { get; set; }
        public string BookTitle { get; set; }
        public string Barcode { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public int OverdueDays { get; set; }
        public decimal EstimatedFine { get; set; }
        public decimal DepositBalance { get; set; }
    }
}
