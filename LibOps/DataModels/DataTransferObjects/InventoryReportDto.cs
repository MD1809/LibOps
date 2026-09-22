using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// DTO biểu diễn dữ liệu Báo cáo Tồn kho và Lưu thông
    /// </summary>
    public class InventoryReportDto
    {
        public int SequenceNumber { get; set; }
        public int BookId { get; set; }
        public string ISBN { get; set; }
        public string Title { get; set; }
        public string CategoryName { get; set; }
        public string AuthorName { get; set; }
        public string PublisherName { get; set; }
        public decimal Price { get; set; }
        public string ShelfLocation { get; set; }
        public int TotalQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public int BorrowedQuantity { get; set; }
        public int DamagedQuantity { get; set; }
        public int LostQuantity { get; set; }
        public decimal TotalStockValue => TotalQuantity * Price;
    }
}
