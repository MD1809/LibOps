using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể ánh xạ bảng Roles trong CSDL
    /// </summary>
    public class RoleEntity
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }

        public string RoleDescription => string.IsNullOrWhiteSpace(Description) ? RoleName : $"{RoleName} ({Description})";
    }
}
