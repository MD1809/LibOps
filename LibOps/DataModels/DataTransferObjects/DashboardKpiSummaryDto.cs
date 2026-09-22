using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// Đối tượng truyền tải dữ liệu tổng hợp chỉ số KPI hiển thị trên Main Dashboard
    /// </summary>
    public class DashboardKpiSummaryDto
    {
        public int TotalBookTitles { get; set; }
        public int TotalBookCopies { get; set; }
        public int AvailableBooks { get; set; }
        public int BorrowingBooks { get; set; }
        public int OverdueBooks { get; set; }
        public int TotalMembers { get; set; }
        public decimal TotalDepositBalance { get; set; }
    }
}
