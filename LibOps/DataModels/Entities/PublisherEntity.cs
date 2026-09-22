using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể ánh xạ bảng Nhà Xuất Bản (Publishers)
    /// </summary>
    public class PublisherEntity
    {
        public int PublisherId { get; set; }
        public string PublisherName { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public int TotalBooks { get; set; } // Thuộc tính bổ trợ thống kê số lượng sách do NXB phát hành
    }
}
