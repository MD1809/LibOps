using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// Đối tượng lưu trữ thông tin phiên làm việc của người dùng sau khi đăng nhập thành công
    /// </summary>
    public class UserSessionDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public int? MemberId { get; set; }
        public string MemberCardCode { get; set; }
        public DateTime LoginTime { get; set; }

        public bool IsAdmin => string.Equals(RoleName, "ADMIN", StringComparison.OrdinalIgnoreCase);
        public bool IsLibrarian => string.Equals(RoleName, "LIBRARIAN", StringComparison.OrdinalIgnoreCase);
        public bool IsReader => string.Equals(RoleName, "READER", StringComparison.OrdinalIgnoreCase);
    }
}
