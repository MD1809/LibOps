using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// Đối tượng truyền tải dữ liệu hiển thị trên bảng danh mục đầu sách (BookListForm)
    /// </summary>
    public class BookGridDisplayDto
    {
        public int BookId { get; set; }
        public string ISBN { get; set; }
        public string Title { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int AuthorId { get; set; }
        public string AuthorName { get; set; }
        public int PublisherId { get; set; }
        public string PublisherName { get; set; }
        public int PublishYear { get; set; }
        public decimal Price { get; set; }
        public int TotalQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public string ShelfLocation { get; set; }
        public string CoverImagePath { get; set; }
        public string Summary { get; set; }
    }

    /// <summary>
    /// Đối tượng truyền tải dữ liệu hiển thị trên bảng bản sao và in mã vạch (BookCopyManagementForm)
    /// </summary>
    public class BookCopyGridDisplayDto
    {
        public bool IsSelected { get; set; }
        public int CopyId { get; set; }
        public string CopyCode => $"CP{CopyId:D4}";
        public int BookId { get; set; }
        public string BookTitle { get; set; }
        public string Barcode { get; set; }
        public string Status { get; set; }
        public string StatusDisplay { get; set; }
        public string ConditionNote { get; set; }
        public DateTime AddedDate { get; set; }
        public DateTime CreatedAt => AddedDate;

        public System.Windows.Media.Brush StatusColorBrush
        {
            get
            {
                if (Status == "AVAILABLE")
                    return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#059669"));
                if (Status == "BORROWED")
                    return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3B82F6"));
                return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#DC2626"));
            }
        }
    }
}
