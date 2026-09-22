using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể ánh xạ bảng Thể Loại Sách (Categories)
    /// </summary>
    public class CategoryEntity
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public int TotalBooks { get; set; } // Thuộc tính bổ trợ thống kê số lượng sách thuộc thể loại
    }
}
