using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể ánh xạ bảng Tác Giả (Authors) - Thiết kế tinh gọn theo yêu cầu người dùng
    /// </summary>
    public class AuthorEntity
    {
        public int AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string Notes { get; set; }
        public int TotalBooks { get; set; } // Thuộc tính bổ trợ thống kê số lượng sách của tác giả
    }
}
