using System;
using System.Globalization;

namespace LibOps.CommonUtilities.Formatting
{
    /// <summary>
    /// Tiện ích định dạng tiền tệ Việt Nam Đồng (VNĐ) và ngày tháng chuẩn
    /// </summary>
    public static class CurrencyAndDateFormattingUtility
    {
        private static readonly CultureInfo VietnameseCulture = new CultureInfo("vi-VN");

        /// <summary>
        /// Định dạng số tiền sang dạng tiền tệ VNĐ (ví dụ: 200.000 đ hoặc 200,000 VNĐ)
        /// </summary>
        public static string FormatCurrency(decimal amount, bool includeSymbol = true)
        {
            if (includeSymbol)
            {
                return amount.ToString("#,##0 VNĐ", VietnameseCulture);
            }
            return amount.ToString("#,##0", VietnameseCulture);
        }

        /// <summary>
        /// Định dạng ngày tháng dạng dd/MM/yyyy
        /// </summary>
        public static string FormatDate(DateTime? dateTime)
        {
            if (!dateTime.HasValue)
            {
                return string.Empty;
            }
            return dateTime.Value.ToString("dd/MM/yyyy", VietnameseCulture);
        }

        /// <summary>
        /// Định dạng ngày giờ dạng dd/MM/yyyy HH:mm
        /// </summary>
        public static string FormatDateTime(DateTime? dateTime)
        {
            if (!dateTime.HasValue)
            {
                return string.Empty;
            }
            return dateTime.Value.ToString("dd/MM/yyyy HH:mm", VietnameseCulture);
        }

        /// <summary>
        /// Parse chuỗi tiền tệ hoặc số về kiểu decimal an toàn
        /// </summary>
        public static bool TryParseDecimal(string input, out decimal result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            string sanitized = input.Replace("VNĐ", "").Replace("đ", "").Replace(".", "").Replace(",", "").Trim();
            return decimal.TryParse(sanitized, out result);
        }
    }
}
