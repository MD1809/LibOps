using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataModels.Entities;

namespace LibOps.DataAccessLayer.DataRepositories
{
    /// <summary>
    /// Repository thao tác bảng SystemSettings trong CSDL
    /// </summary>
    public class SystemSettingRepository
    {
        public List<SystemSettingEntity> GetAllSettings()
        {
            string sql = "SELECT SettingKey, SettingValue, Description, LastModifiedAt FROM dbo.SystemSettings ORDER BY SettingKey ASC";
            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
            var list = new List<SystemSettingEntity>();

            foreach (DataRow row in table.Rows)
            {
                list.Add(new SystemSettingEntity
                {
                    SettingKey = row["SettingKey"].ToString(),
                    SettingValue = row["SettingValue"].ToString(),
                    Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : string.Empty,
                    LastModifiedAt = Convert.ToDateTime(row["LastModifiedAt"])
                });
            }

            return list;
        }

        public string GetSettingValue(string settingKey, string defaultValue = "")
        {
            string sql = "SELECT SettingValue FROM dbo.SystemSettings WHERE SettingKey = @SettingKey";
            var prms = new[]
            {
                new SqlParameter("@SettingKey", SqlDbType.NVarChar, 50) { Value = settingKey }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, prms);
            if (result != null && result != DBNull.Value)
            {
                return result.ToString();
            }

            return defaultValue;
        }

        public bool UpdateSettingValue(string settingKey, string settingValue)
        {
            string sql = @"
                IF EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = @SettingKey)
                BEGIN
                    UPDATE dbo.SystemSettings
                    SET SettingValue = @SettingValue, LastModifiedAt = GETDATE()
                    WHERE SettingKey = @SettingKey;
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description, LastModifiedAt)
                    VALUES (@SettingKey, @SettingValue, @SettingKey, GETDATE());
                END";

            var prms = new[]
            {
                new SqlParameter("@SettingValue", SqlDbType.NVarChar, 250) { Value = settingValue },
                new SqlParameter("@SettingKey", SqlDbType.NVarChar, 50) { Value = settingKey }
            };

            return DatabaseConnectionHelper.ExecuteNonQuery(sql, prms) > 0;
        }
    }
}

