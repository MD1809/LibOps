using System;
using System.Security.Cryptography;
using System.Text;

namespace LibOps.CommonUtilities.Security
{
    /// <summary>
    /// Lớp tiện ích băm mật khẩu bằng SHA-256 kèm chuỗi Salt ngẫu nhiên để bảo mật tài khoản
    /// </summary>
    public static class PasswordHashUtility
    {
        private const string DefaultSaltPrefix = "LIB_OPS_SALT_";

        /// <summary>
        /// Sinh chuỗi Salt ngẫu nhiên bằng RNGCryptoServiceProvider
        /// </summary>
        public static string GenerateSalt(int byteLength = 16)
        {
            byte[] saltBytes = new byte[byteLength];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            return DefaultSaltPrefix + Convert.ToBase64String(saltBytes);
        }

        /// <summary>
        /// Ghép mật khẩu gốc với Salt rồi băm bằng thuật toán SHA-256
        /// </summary>
        public static string ComputeSha256Hash(string rawPassword, string salt)
        {
            if (rawPassword == null)
            {
                throw new ArgumentNullException(nameof(rawPassword));
            }

            // Ghép chuỗi mật khẩu và salt trước khi băm
            string combined = rawPassword + (salt ?? string.Empty);
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                var builder = new StringBuilder();
                // Đổi mảng byte sang dạng chuỗi Hex in hoa
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("X2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Kiểm tra mật khẩu người dùng nhập vào có khớp với hash và salt trong CSDL không
        /// </summary>
        public static bool VerifyPassword(string inputPassword, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            string computed = ComputeSha256Hash(inputPassword, storedSalt);
            return string.Equals(computed, storedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
