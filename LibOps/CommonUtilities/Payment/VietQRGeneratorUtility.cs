using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using QRCoder;

namespace LibOps.CommonUtilities.Payment
{
    /// <summary>
    /// Tiện ích sinh mã VietQR EMVCo tiêu chuẩn NAPAS và hiển thị mã QR lên giao diện WPF
    /// </summary>
    public static class VietQRGeneratorUtility
    {
        // Danh mục mã BIN chuẩn của các ngân hàng tại Việt Nam
        public static readonly Dictionary<string, string> BankBinLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "MB", "970422" },
            { "MBBANK", "970422" },
            { "VIETINBANK", "970415" },
            { "CTG", "970415" },
            { "VIETCOMBANK", "970436" },
            { "VCB", "970436" },
            { "BIDV", "970418" },
            { "TECHCOMBANK", "970407" },
            { "TCB", "970407" },
            { "VPBANK", "970432" },
            { "TPBANK", "970423" },
            { "ACB", "970416" },
            { "AGRIBANK", "970405" }
        };

        /// <summary>
        /// Tạo chuỗi Payload VietQR EMVCo tiêu chuẩn NAPAS được tất cả ứng dụng ngân hàng tại Việt Nam nhận diện
        /// </summary>
        public static string BuildVietQrEmvCoPayload(string bankCodeOrBin, string accountNumber, decimal amount, string transferContent)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                accountNumber = VietQRPaymentSimulatorUtility.DEFAULT_ACCOUNT_NUMBER;
            }

            string bankBin = "970422"; // Mặc định MBBank
            if (!string.IsNullOrWhiteSpace(bankCodeOrBin))
            {
                string cleanKey = bankCodeOrBin.Trim().ToUpper();
                if (BankBinLookup.TryGetValue(cleanKey, out string foundBin))
                {
                    bankBin = foundBin;
                }
                else if (Regex.IsMatch(cleanKey, @"^\d{6}$"))
                {
                    bankBin = cleanKey;
                }
            }

            // Tag 00: Payload Format Indicator
            string tag00 = "000201";

            // Tag 01: Point of Initiation Method (12 = Dynamic QR có số tiền, 11 = Static QR)
            string tag01 = amount > 0 ? "010212" : "010211";

            // Tag 38: Merchant Account Information - Chuẩn VietQR NAPAS
            string guid = "0010A000000727";
            string subTag00 = $"0006{bankBin}";
            string cleanAcc = accountNumber.Trim();
            string subTag01 = $"01{cleanAcc.Length:D2}{cleanAcc}";
            string beneficiaryVal = subTag00 + subTag01;
            string beneficiary = $"01{beneficiaryVal.Length:D2}{beneficiaryVal}";
            string serviceCode = "0208QRIBFTTA";
            string tag38Val = guid + beneficiary + serviceCode;
            string tag38 = $"38{tag38Val.Length:D2}{tag38Val}";

            // Tag 53: Transaction Currency (704 = VND)
            string tag53 = "5303704";

            // Tag 54: Transaction Amount
            string tag54 = string.Empty;
            if (amount > 0)
            {
                string amtStr = ((long)amount).ToString();
                tag54 = $"54{amtStr.Length:D2}{amtStr}";
            }

            // Tag 58: Country Code (VN)
            string tag58 = "5802VN";

            // Tag 62: Additional Data Field Template (Nội dung chuyển khoản)
            string tag62 = string.Empty;
            if (!string.IsNullOrWhiteSpace(transferContent))
            {
                string cleanDesc = Regex.Replace(transferContent.Trim(), @"[^a-zA-Z0-9 ]", "");
                if (cleanDesc.Length > 25) cleanDesc = cleanDesc.Substring(0, 25);
                string subTag08 = $"08{cleanDesc.Length:D2}{cleanDesc}";
                tag62 = $"62{subTag08.Length:D2}{subTag08}";
            }

            // Tag 63: CRC16-CCITT (Checksum 4 ký tự Hex)
            string raw = tag00 + tag01 + tag38 + tag53 + tag54 + tag58 + tag62 + "6304";
            string crc = ComputeCrc16Ccitt(raw);

            return raw + crc;
        }

        /// <summary>
        /// Tính toán mã kiểm tra CRC16-CCITT (Polynomial 0x1021, Init 0xFFFF)
        /// </summary>
        public static string ComputeCrc16Ccitt(string data)
        {
            ushort crc = 0xFFFF;
            ushort polynomial = 0x1021;
            byte[] bytes = Encoding.ASCII.GetBytes(data ?? string.Empty);

            foreach (byte b in bytes)
            {
                for (int i = 0; i < 8; i++)
                {
                    bool bit = ((b >> (7 - i)) & 1) == 1;
                    bool c15 = ((crc >> 15) & 1) == 1;
                    crc <<= 1;
                    if (c15 ^ bit) crc ^= polynomial;
                }
            }

            return (crc & 0xFFFF).ToString("X4");
        }

        /// <summary>
        /// Chuyển chuỗi định dạng EMVCo VietQR thành Bitmap
        /// </summary>
        public static Bitmap GenerateQrBitmap(string qrContent, int pixelsPerModule = 6)
        {
            if (string.IsNullOrWhiteSpace(qrContent))
            {
                return new Bitmap(200, 200);
            }

            using (var qrGenerator = new QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCode(qrCodeData))
            {
                return qrCode.GetGraphic(pixelsPerModule, Color.FromArgb(15, 23, 42), Color.White, true);
            }
        }

        /// <summary>
        /// Chuyển chuỗi định dạng EMVCo VietQR thành WPF BitmapSource để hiển thị trực tiếp lên UI
        /// </summary>
        public static BitmapSource GenerateQrBitmapSource(string qrContent, int pixelsPerModule = 6)
        {
            using (var bitmap = GenerateQrBitmap(qrContent, pixelsPerModule))
            {
                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, ImageFormat.Png);
                    memory.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
        }
    }
}
