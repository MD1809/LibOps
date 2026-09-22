using System;

namespace LibOps.DataModels.Entities
{
    /// <summary>
    /// Thực thể đại diện một tham số cấu hình hệ thống động (bảng SystemSettings)
    /// </summary>
    public class SystemSettingEntity
    {
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
        public string Description { get; set; }
        public DateTime LastModifiedAt { get; set; }
    }
}
