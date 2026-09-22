using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataModels.Entities;

namespace LibOps.DataAccessLayer.DataRepositories
{
    /// <summary>
    /// Repository thao tác dữ liệu bảng UserAccounts và Roles trong CSDL
    /// </summary>
    public class UserRepository
    {
        /// <summary>
        /// Lấy thông tin tài khoản theo tên đăng nhập
        /// </summary>
        public virtual UserAccountEntity GetUserByUsername(string username)
        {
            string sql = @"
                SELECT u.UserId, u.Username, u.PasswordHash, u.PasswordSalt, u.FullName, 
                       u.Email, u.PhoneNumber, u.RoleId, r.RoleName, u.MemberId, m.MemberCardCode, u.IsActive, u.CreatedAt
                FROM dbo.UserAccounts u
                INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
                LEFT JOIN dbo.Members m ON u.MemberId = m.MemberId
                WHERE u.Username = @Username";

            var parameters = new[]
            {
                new SqlParameter("@Username", SqlDbType.NVarChar, 50) { Value = username }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            DataRow row = table.Rows[0];
            return MapDataRowToUserEntity(row);
        }

        /// <summary>
        /// Lấy thông tin tài khoản theo mã người dùng
        /// </summary>
        public virtual UserAccountEntity GetUserById(int userId)
        {
            string sql = @"
                SELECT u.UserId, u.Username, u.PasswordHash, u.PasswordSalt, u.FullName, 
                       u.Email, u.PhoneNumber, u.RoleId, r.RoleName, u.MemberId, m.MemberCardCode, u.IsActive, u.CreatedAt
                FROM dbo.UserAccounts u
                INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
                LEFT JOIN dbo.Members m ON u.MemberId = m.MemberId
                WHERE u.UserId = @UserId";

            var parameters = new[]
            {
                new SqlParameter("@UserId", SqlDbType.Int) { Value = userId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            return MapDataRowToUserEntity(table.Rows[0]);
        }

        /// <summary>
        /// Lấy thông tin tài khoản theo mã độc giả (MemberId)
        /// </summary>
        public virtual UserAccountEntity GetUserByMemberId(int memberId, SqlTransaction transaction = null)
        {
            string sql = @"
                SELECT u.UserId, u.Username, u.PasswordHash, u.PasswordSalt, u.FullName, 
                       u.Email, u.PhoneNumber, u.RoleId, r.RoleName, u.MemberId, m.MemberCardCode, u.IsActive, u.CreatedAt
                FROM dbo.UserAccounts u
                INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
                LEFT JOIN dbo.Members m ON u.MemberId = m.MemberId
                WHERE u.MemberId = @MemberId";

            var parameters = new[]
            {
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql, parameters, transaction);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            return MapDataRowToUserEntity(table.Rows[0]);
        }

        /// <summary>
        /// Cập nhật mật khẩu mới của người dùng
        /// </summary>
        public virtual bool UpdatePassword(int userId, string newPasswordHash, string newPasswordSalt)
        {
            string sql = @"
                UPDATE dbo.UserAccounts 
                SET PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt 
                WHERE UserId = @UserId";

            var parameters = new[]
            {
                new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 256) { Value = newPasswordHash },
                new SqlParameter("@PasswordSalt", SqlDbType.NVarChar, 128) { Value = newPasswordSalt },
                new SqlParameter("@UserId", SqlDbType.Int) { Value = userId }
            };

            return DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters) > 0;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách tài khoản kèm vai trò
        /// </summary>
        public List<UserAccountEntity> GetAllUsers()
        {
            string sql = @"
                SELECT u.UserId, u.Username, u.PasswordHash, u.PasswordSalt, u.FullName, 
                       u.Email, u.PhoneNumber, u.RoleId, r.RoleName, u.MemberId, m.MemberCardCode, u.IsActive, u.CreatedAt
                FROM dbo.UserAccounts u
                INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
                LEFT JOIN dbo.Members m ON u.MemberId = m.MemberId
                ORDER BY u.UserId ASC";

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
            var users = new List<UserAccountEntity>();

            foreach (DataRow row in table.Rows)
            {
                users.Add(MapDataRowToUserEntity(row));
            }

            return users;
        }

        /// <summary>
        /// Lấy danh sách tài khoản nhân viên vận hành hệ thống (ADMIN, LIBRARIAN)
        /// </summary>
        public List<UserAccountEntity> GetStaffUsers()
        {
            string sql = @"
                SELECT u.UserId, u.Username, u.PasswordHash, u.PasswordSalt, u.FullName, 
                       u.Email, u.PhoneNumber, u.RoleId, r.RoleName, u.MemberId, m.MemberCardCode, u.IsActive, u.CreatedAt
                FROM dbo.UserAccounts u
                INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
                LEFT JOIN dbo.Members m ON u.MemberId = m.MemberId
                WHERE r.RoleName IN ('ADMIN', 'LIBRARIAN')
                ORDER BY u.UserId ASC";

            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
            var users = new List<UserAccountEntity>();

            foreach (DataRow row in table.Rows)
            {
                users.Add(MapDataRowToUserEntity(row));
            }

            return users;
        }

        /// <summary>
        /// Thêm mới tài khoản người dùng
        /// </summary>
        public int InsertUser(UserAccountEntity user, SqlTransaction transaction = null)
        {
            string sql = @"
                INSERT INTO dbo.UserAccounts (Username, PasswordHash, PasswordSalt, FullName, Email, PhoneNumber, RoleId, MemberId, IsActive, CreatedAt)
                VALUES (@Username, @PasswordHash, @PasswordSalt, @FullName, @Email, @PhoneNumber, @RoleId, @MemberId, @IsActive, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new[]
            {
                new SqlParameter("@Username", SqlDbType.NVarChar, 50) { Value = user.Username },
                new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 256) { Value = user.PasswordHash },
                new SqlParameter("@PasswordSalt", SqlDbType.NVarChar, 128) { Value = user.PasswordSalt },
                new SqlParameter("@FullName", SqlDbType.NVarChar, 100) { Value = user.FullName },
                new SqlParameter("@Email", SqlDbType.NVarChar, 100) { Value = (object)user.Email ?? DBNull.Value },
                new SqlParameter("@PhoneNumber", SqlDbType.NVarChar, 20) { Value = (object)user.PhoneNumber ?? DBNull.Value },
                new SqlParameter("@RoleId", SqlDbType.Int) { Value = user.RoleId },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = user.MemberId.HasValue ? (object)user.MemberId.Value : DBNull.Value },
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = user.IsActive }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters, transaction);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Cập nhật thông tin tài khoản người dùng
        /// </summary>
        public bool UpdateUser(UserAccountEntity user)
        {
            string sql = @"
                UPDATE dbo.UserAccounts
                SET FullName = @FullName, Email = @Email, PhoneNumber = @PhoneNumber, RoleId = @RoleId, MemberId = @MemberId, IsActive = @IsActive
                WHERE UserId = @UserId";

            var parameters = new[]
            {
                new SqlParameter("@FullName", SqlDbType.NVarChar, 100) { Value = user.FullName },
                new SqlParameter("@Email", SqlDbType.NVarChar, 100) { Value = (object)user.Email ?? DBNull.Value },
                new SqlParameter("@PhoneNumber", SqlDbType.NVarChar, 20) { Value = (object)user.PhoneNumber ?? DBNull.Value },
                new SqlParameter("@RoleId", SqlDbType.Int) { Value = user.RoleId },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = user.MemberId.HasValue ? (object)user.MemberId.Value : DBNull.Value },
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = user.IsActive },
                new SqlParameter("@UserId", SqlDbType.Int) { Value = user.UserId }
            };

            return DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters) > 0;
        }

        /// <summary>
        /// Cập nhật thông tin liên hệ của tài khoản độc giả liên kết theo MemberId
        /// </summary>
        public bool UpdateUserContactByMemberId(int memberId, string fullName, string email, string phoneNumber, SqlTransaction transaction = null)
        {
            string sql = @"
                UPDATE dbo.UserAccounts
                SET FullName = @FullName, Email = @Email, PhoneNumber = @PhoneNumber
                WHERE MemberId = @MemberId";

            var parameters = new[]
            {
                new SqlParameter("@FullName", SqlDbType.NVarChar, 100) { Value = fullName },
                new SqlParameter("@Email", SqlDbType.NVarChar, 100) { Value = (object)email ?? DBNull.Value },
                new SqlParameter("@PhoneNumber", SqlDbType.NVarChar, 20) { Value = (object)phoneNumber ?? DBNull.Value },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            return DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction) > 0;
        }

        /// <summary>
        /// Đặt lại mật khẩu tài khoản độc giả theo MemberId
        /// </summary>
        public bool ResetPasswordByMemberId(int memberId, string newPasswordHash, string newPasswordSalt, SqlTransaction transaction = null)
        {
            string sql = @"
                UPDATE dbo.UserAccounts
                SET PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt
                WHERE MemberId = @MemberId";

            var parameters = new[]
            {
                new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 256) { Value = newPasswordHash },
                new SqlParameter("@PasswordSalt", SqlDbType.NVarChar, 128) { Value = newPasswordSalt },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };

            return DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction) > 0;
        }

        public bool IsUsernameExists(string username, int excludeUserId = 0)
        {
            string sql = "SELECT COUNT(*) FROM dbo.UserAccounts WHERE Username = @Username AND UserId != @ExcludeUserId";
            var parameters = new[]
            {
                new SqlParameter("@Username", SqlDbType.NVarChar, 50) { Value = username.Trim() },
                new SqlParameter("@ExcludeUserId", SqlDbType.Int) { Value = excludeUserId }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters);
            return Convert.ToInt32(result) > 0;
        }

        public List<RoleEntity> GetAllRoles()
        {
            string sql = "SELECT RoleId, RoleName, Description FROM dbo.Roles ORDER BY RoleId ASC";
            DataTable table = DatabaseConnectionHelper.ExecuteQueryToDataTable(sql);
            var roles = new List<RoleEntity>();
            foreach (DataRow row in table.Rows)
            {
                roles.Add(new RoleEntity
                {
                    RoleId = Convert.ToInt32(row["RoleId"]),
                    RoleName = row["RoleName"].ToString(),
                    Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : string.Empty
                });
            }
            return roles;
        }

        /// <summary>
        /// Lấy RoleId theo RoleName (Ví dụ: 'READER', 'LIBRARIAN', 'ADMIN')
        /// </summary>
        public virtual int GetRoleIdByName(string roleName, SqlTransaction transaction = null)
        {
            string cleanRole = (roleName ?? "READER").Trim().ToUpperInvariant();
            string sql = "SELECT TOP 1 RoleId FROM dbo.Roles WHERE UPPER(RTRIM(LTRIM(RoleName))) = @RoleName";
            var parameters = new[]
            {
                new SqlParameter("@RoleName", SqlDbType.NVarChar, 50) { Value = cleanRole }
            };

            object result = DatabaseConnectionHelper.ExecuteScalar(sql, parameters, transaction);
            if (result != null && result != DBNull.Value && Convert.ToInt32(result) > 0)
            {
                return Convert.ToInt32(result);
            }

            // Nếu chưa có vai trò, tự động tạo mới an toàn
            string insertSql = @"
                INSERT INTO dbo.Roles (RoleName, Description) 
                VALUES (@RoleName, N'Vai trò ' + @RoleName + N' hệ thống');
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var insertParams = new[]
            {
                new SqlParameter("@RoleName", SqlDbType.NVarChar, 50) { Value = cleanRole }
            };
            object newId = DatabaseConnectionHelper.ExecuteScalar(insertSql, insertParams, transaction);
            if (newId != null && newId != DBNull.Value)
            {
                return Convert.ToInt32(newId);
            }

            // Fallback: Lấy RoleId đầu tiên bất kỳ trong bảng Roles
            object fallback = DatabaseConnectionHelper.ExecuteScalar("SELECT TOP 1 RoleId FROM dbo.Roles ORDER BY RoleId ASC", null, transaction);
            return fallback != null && fallback != DBNull.Value ? Convert.ToInt32(fallback) : 1;
        }

        /// <summary>
        /// Cập nhật trạng thái kích hoạt / khóa tài khoản
        /// </summary>
        public bool UpdateUserStatus(int userId, bool isActive, SqlTransaction transaction = null)
        {
            string sql = "UPDATE dbo.UserAccounts SET IsActive = @IsActive WHERE UserId = @UserId";
            var parameters = new[]
            {
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = isActive },
                new SqlParameter("@UserId", SqlDbType.Int) { Value = userId }
            };
            return DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction) > 0;
        }

        /// <summary>
        /// Cập nhật trạng thái kích hoạt / khóa tài khoản độc giả theo MemberId
        /// </summary>
        public bool UpdateIsActiveByMemberId(int memberId, bool isActive, SqlTransaction transaction = null)
        {
            string sql = "UPDATE dbo.UserAccounts SET IsActive = @IsActive WHERE MemberId = @MemberId";
            var parameters = new[]
            {
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = isActive },
                new SqlParameter("@MemberId", SqlDbType.Int) { Value = memberId }
            };
            return DatabaseConnectionHelper.ExecuteNonQuery(sql, parameters, transaction) > 0;
        }

        private static UserAccountEntity MapDataRowToUserEntity(DataRow row)
        {
            return new UserAccountEntity
            {
                UserId = Convert.ToInt32(row["UserId"]),
                Username = row["Username"].ToString(),
                PasswordHash = row["PasswordHash"].ToString(),
                PasswordSalt = row["PasswordSalt"].ToString(),
                FullName = row["FullName"].ToString(),
                Email = row["Email"] != DBNull.Value ? row["Email"].ToString() : string.Empty,
                PhoneNumber = row["PhoneNumber"] != DBNull.Value ? row["PhoneNumber"].ToString() : string.Empty,
                RoleId = Convert.ToInt32(row["RoleId"]),
                RoleName = row["RoleName"].ToString(),
                MemberId = row.Table.Columns.Contains("MemberId") && row["MemberId"] != DBNull.Value ? (int?)Convert.ToInt32(row["MemberId"]) : null,
                MemberCardCode = row.Table.Columns.Contains("MemberCardCode") && row["MemberCardCode"] != DBNull.Value ? row["MemberCardCode"].ToString() : string.Empty,
                IsActive = Convert.ToBoolean(row["IsActive"]),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"])
            };
        }
    }
}
