using System;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace LibOps.CommonUtilities.Validation
{
    /// <summary>
    /// Lớp tiện ích kiểm tra dữ liệu đầu vào (SĐT, CCCD, Email, Username, Ngày sinh)
    /// </summary>
    public static class InputValidationUtility
    {
        // Regex kiểm tra số điện thoại Việt Nam (đầu số 03, 05, 07, 08, 09, 02...)
        private static readonly Regex PhoneRegex = new Regex(@"^(0|\+84)(3[2-9]|5[25689]|7[06-9]|8[1-9]|9[0-9]|2[0-9])[0-9]{7}$", RegexOptions.Compiled);
        
        // Regex phụ kiểm tra chuỗi số từ 9 đến 11 chữ số
        private static readonly Regex GenericPhoneRegex = new Regex(@"^[0-9]{9,11}$", RegexOptions.Compiled);

        // Regex kiểm tra CCCD (12 số) hoặc CMND cũ (9 số)
        private static readonly Regex CitizenIdRegex = new Regex(@"^[0-9]{9}$|^[0-9]{12}$", RegexOptions.Compiled);

        // Regex kiểm tra định dạng email
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex kiểm tra tên đăng nhập: từ 3-50 ký tự (chữ cái, chữ số, gạch dưới)
        private static readonly Regex UsernameRegex = new Regex(@"^[a-zA-Z0-9_]{3,50}$", RegexOptions.Compiled);

        /// <summary>
        /// Kiểm tra định dạng số điện thoại Việt Nam hợp lệ
        /// </summary>
        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            string clean = phoneNumber.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
            return PhoneRegex.IsMatch(clean) || GenericPhoneRegex.IsMatch(clean);
        }

        /// <summary>
        /// Kiểm tra định dạng số CCCD / CMND hợp lệ (9 hoặc 12 chữ số)
        /// </summary>
        public static bool IsValidIdentityCard(string identityCardNumber)
        {
            if (string.IsNullOrWhiteSpace(identityCardNumber))
                return false;

            string clean = identityCardNumber.Trim().Replace(" ", "");
            return CitizenIdRegex.IsMatch(clean);
        }

        /// <summary>
        /// Kiểm tra định dạng địa chỉ Email hợp lệ
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            string clean = email.Trim();
            if (clean.Length > 254)
                return false;

            if (!EmailRegex.IsMatch(clean))
                return false;

            try
            {
                var addr = new MailAddress(clean);
                return addr.Address == clean;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra định dạng Tên đăng nhập hợp lệ
        /// </summary>
        public static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return UsernameRegex.IsMatch(username.Trim());
        }

        /// <summary>
        /// Kiểm tra ngày sinh hợp lệ (không ở tương lai, tuổi từ 5 đến 120 tuổi)
        /// </summary>
        public static bool IsValidDateOfBirth(DateTime? dob, out string errorMessage, int minAge = 5, int maxAge = 120)
        {
            errorMessage = string.Empty;
            if (!dob.HasValue)
                return true; // Cho phép để trống nếu không bắt buộc

            DateTime birthDate = dob.Value.Date;
            if (birthDate > DateTime.Today)
            {
                errorMessage = "Ngày sinh không thể lớn hơn ngày hiện tại.";
                return false;
            }

            int age = DateTime.Today.Year - birthDate.Year;
            if (birthDate > DateTime.Today.AddYears(-age)) age--;

            if (age < minAge)
            {
                errorMessage = $"Độc giả phải từ đủ {minAge} tuổi trở lên.";
                return false;
            }

            if (age > maxAge)
            {
                errorMessage = "Ngày sinh không hợp lệ.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Chuẩn hóa chuỗi số điện thoại
        /// </summary>
        public static string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            return phone.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
        }

        /// <summary>
        /// Chuẩn hóa chuỗi số CCCD/CMND
        /// </summary>
        public static string NormalizeIdentityCard(string idCard)
        {
            if (string.IsNullOrWhiteSpace(idCard))
                return string.Empty;

            return idCard.Trim().Replace(" ", "");
        }
    }
}
