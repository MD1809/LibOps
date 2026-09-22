using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace LibOps.CommonUtilities.Payment
{
    /// <summary>
    /// Tiện ích sinh mã VietQR chuẩn và mô phỏng giao diện thanh toán chuyển khoản trực tuyến
    /// </summary>
    public static class VietQRPaymentSimulatorUtility
    {
        public static string DEFAULT_BANK_CODE => System.Configuration.ConfigurationManager.AppSettings["VietQR_BankCode"] ?? "MB";
        public static string DEFAULT_BANK_NAME => System.Configuration.ConfigurationManager.AppSettings["VietQR_BankName"] ?? "MBBank (Ngân Hàng Quân Đội)";
        public static string DEFAULT_ACCOUNT_NUMBER => System.Configuration.ConfigurationManager.AppSettings["VietQR_AccountNumber"] ?? "0868889999";
        public static string DEFAULT_ACCOUNT_HOLDER => System.Configuration.ConfigurationManager.AppSettings["VietQR_AccountHolder"] ?? "HE THONG THU VIEN LIBOPS";

        /// <summary>
        /// Tạo nội dung thanh toán chuẩn VietQR
        /// </summary>
        public static string GeneratePaymentTransferContent(string memberCardCode, string actionType)
        {
            return $"LIBOPS {memberCardCode.Trim().ToUpper()} {actionType.Trim().ToUpper()}";
        }

        /// <summary>
        /// Vẽ đồ họa mô phỏng khung mã VietQR chuẩn đẹp mắt (360x440 px)
        /// </summary>
        public static Bitmap GenerateVietQRBitmap(
            string memberCardCode, 
            string memberName, 
            decimal amount, 
            string transferContent, 
            string purposeTitle = "NẠP TIỀN THẺ THƯ VIỆN")
        {
            int width = 360;
            int height = 440;
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // 1. Nền trắng bo góc
                g.Clear(Color.FromArgb(248, 250, 252));

                using (var penBorder = new Pen(Color.FromArgb(226, 232, 240), 2f))
                {
                    g.DrawRectangle(penBorder, 1, 1, width - 2, height - 2);
                }

                // 2. Header Banner xanh VietQR
                using (var headerBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, width, 65),
                    Color.FromArgb(30, 58, 138),
                    Color.FromArgb(2, 132, 199),
                    LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(headerBrush, 0, 0, width, 65);
                }

                using (var fontHeader = new Font("Segoe UI", 11, FontStyle.Bold))
                using (var fontSubHeader = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (var textBrush = new SolidBrush(Color.White))
                {
                    g.DrawString("VIETQR CỔNG THANH TOÁN TỰ ĐỘNG", fontHeader, textBrush, new PointF(15, 12));
                    g.DrawString(purposeTitle, fontSubHeader, textBrush, new PointF(15, 36));
                }

                // 3. Khung vẽ mô phỏng mã QR Code trung tâm (200x200 px)
                int qrX = (width - 190) / 2;
                int qrY = 80;
                int qrSize = 190;

                using (var qrBgBrush = new SolidBrush(Color.White))
                using (var qrBorderPen = new Pen(Color.FromArgb(203, 213, 225), 1.5f))
                {
                    g.FillRectangle(qrBgBrush, qrX, qrY, qrSize, qrSize);
                    g.DrawRectangle(qrBorderPen, qrX, qrY, qrSize, qrSize);
                }

                // Vẽ các ô định vị QR Code (Finder patterns góc)
                DrawFinderPattern(g, qrX + 12, qrY + 12, 36);
                DrawFinderPattern(g, qrX + qrSize - 48, qrY + 12, 36);
                DrawFinderPattern(g, qrX + 12, qrY + qrSize - 48, 36);

                // Vẽ các pixel ma trận QR ngẫu nhiên xác thực theo transferContent
                int seed = Math.Abs((transferContent + memberCardCode + amount).GetHashCode());
                var rand = new Random(seed);
                using (var pixelBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    int cellSize = 5;
                    for (int r = 0; r < 26; r++)
                    {
                        for (int c = 0; c < 26; c++)
                        {
                            // Tránh vẽ đè vào 3 góc Finder Patterns
                            bool inFinder1 = r < 9 && c < 9;
                            bool inFinder2 = r < 9 && c > 16;
                            bool inFinder3 = r > 16 && c < 9;
                            bool inCenterLogo = r >= 10 && r <= 15 && c >= 10 && c <= 15;

                            if (!inFinder1 && !inFinder2 && !inFinder3 && !inCenterLogo)
                            {
                                if (rand.Next(100) < 45)
                                {
                                    g.FillRectangle(pixelBrush, qrX + 15 + c * cellSize + 2, qrY + 15 + r * cellSize + 2, cellSize - 1, cellSize - 1);
                                }
                            }
                        }
                    }

                    // Logo nhỏ ở giữa QR
                    int logoX = qrX + (qrSize - 44) / 2;
                    int logoY = qrY + (qrSize - 44) / 2;
                    g.FillRectangle(new SolidBrush(Color.White), logoX, logoY, 44, 44);
                    g.DrawRectangle(new Pen(Color.FromArgb(2, 132, 199), 1.5f), logoX, logoY, 44, 44);
                    using (var logoFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                    {
                        g.DrawString("LIB\nOPS", logoFont, new SolidBrush(Color.FromArgb(2, 132, 199)), new RectangleF(logoX, logoY + 6, 44, 30), new StringFormat { Alignment = StringAlignment.Center });
                    }
                }

                // 4. Thông tin chuyển khoản phía dưới QR
                int infoY = qrY + qrSize + 15;
                using (var fontLabel = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (var fontValue = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (var fontAmount = new Font("Segoe UI", 12f, FontStyle.Bold))
                using (var brushMuted = new SolidBrush(Color.FromArgb(100, 116, 139)))
                using (var brushDark = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (var brushPrimary = new SolidBrush(Color.FromArgb(2, 132, 199)))
                using (var brushSuccess = new SolidBrush(Color.FromArgb(22, 101, 52)))
                {
                    g.DrawString($"STK: {DEFAULT_ACCOUNT_NUMBER} ({DEFAULT_BANK_CODE})", fontValue, brushDark, new PointF(18, infoY));
                    g.DrawString($"Chủ TK: {DEFAULT_ACCOUNT_HOLDER}", fontLabel, brushMuted, new PointF(18, infoY + 20));
                    g.DrawString($"Số tiền:", fontLabel, brushMuted, new PointF(18, infoY + 42));
                    g.DrawString($"{amount:N0} VNĐ", fontAmount, brushSuccess, new PointF(75, infoY + 38));
                    
                    g.DrawString($"Nội dung CK:", fontLabel, brushMuted, new PointF(18, infoY + 68));
                    g.DrawString(transferContent, fontValue, brushPrimary, new PointF(100, infoY + 68));

                    g.DrawString("💡 Quét mã bằng App Ngân hàng hoặc bấm 'Xác Nhận Đã Chuyển Khoản'", fontLabel, brushMuted, new PointF(12, infoY + 95));
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Danh sách ngân hàng phổ biến tại Việt Nam
        /// </summary>
        public static System.Collections.Generic.List<BankInfoItem> GetPopularBankList()
        {
            return new System.Collections.Generic.List<BankInfoItem>
            {
                new BankInfoItem { Code = "MB", Name = "MBBank - Ngân Hàng Quân Đội", ShortName = "MBBank" },
                new BankInfoItem { Code = "VCB", Name = "Vietcombank - Ngân Hàng Ngoại Thương VN", ShortName = "Vietcombank" },
                new BankInfoItem { Code = "TCB", Name = "Techcombank - Ngân Hàng Kỹ Thương VN", ShortName = "Techcombank" },
                new BankInfoItem { Code = "BIDV", Name = "BIDV - Ngân Hàng Đầu Tư & Phát Triển VN", ShortName = "BIDV" },
                new BankInfoItem { Code = "CTG", Name = "VietinBank - Ngân Hàng Công Thương VN", ShortName = "VietinBank" },
                new BankInfoItem { Code = "ACB", Name = "ACB - Ngân Hàng Á Châu", ShortName = "ACB" },
                new BankInfoItem { Code = "VPB", Name = "VPBank - Ngân Hàng Việt Nam Thịnh Vượng", ShortName = "VPBank" },
                new BankInfoItem { Code = "TPB", Name = "TPBank - Ngân Hàng Tiên Phong", ShortName = "TPBank" },
                new BankInfoItem { Code = "VBA", Name = "Agribank - Ngân Hàng NN & PTNT VN", ShortName = "Agribank" },
                new BankInfoItem { Code = "STB", Name = "Sacombank - Ngân Hàng Sài Gòn Thương Tín", ShortName = "Sacombank" },
                new BankInfoItem { Code = "OCB", Name = "OCB - Ngân Hàng Phương Đông", ShortName = "OCB" },
                new BankInfoItem { Code = "SHB", Name = "SHB - Ngân Hàng Sài Gòn - Hà Nội", ShortName = "SHB" },
                new BankInfoItem { Code = "VIB", Name = "VIB - Ngân Hàng Quốc Tế", ShortName = "VIB" },
                new BankInfoItem { Code = "MSB", Name = "MSB - Ngân Hàng Hàng Hải", ShortName = "MSB" }
            };
        }

        /// <summary>
        /// Sinh mã VietQR chuyển khoản chiều đi (Outbound QR) để thủ thư quét chuyển trả tiền cho độc giả
        /// </summary>
        public static Bitmap GenerateOutboundVietQRBitmap(
            string bankName, 
            string accountNo, 
            string accountHolder, 
            decimal amount, 
            string transferContent, 
            string requestCode = "")
        {
            int width = 360;
            int height = 440;
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // 1. Nền trắng bo góc
                g.Clear(Color.FromArgb(248, 250, 252));

                using (var penBorder = new Pen(Color.FromArgb(226, 232, 240), 2f))
                {
                    g.DrawRectangle(penBorder, 1, 1, width - 2, height - 2);
                }

                // 2. Header Banner màu cam/đỏ hoàn tiền (Refund Header)
                using (var headerBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, width, 65),
                    Color.FromArgb(180, 83, 9),
                    Color.FromArgb(217, 119, 6),
                    LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(headerBrush, 0, 0, width, 65);
                }

                using (var fontHeader = new Font("Segoe UI", 11, FontStyle.Bold))
                using (var fontSubHeader = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (var textBrush = new SolidBrush(Color.White))
                {
                    g.DrawString("VIETQR HOÀN TRẢ TIỀN CỌC ĐỘC GIẢ", fontHeader, textBrush, new PointF(15, 12));
                    string sub = string.IsNullOrWhiteSpace(requestCode) ? "CHUYỂN KHOẢN TRẢ TIỀN THẺ" : $"YÊU CẦU #{requestCode}";
                    g.DrawString(sub, fontSubHeader, textBrush, new PointF(15, 36));
                }

                // 3. Khung vẽ mô phỏng mã QR Code trung tâm (190x190 px)
                int qrX = (width - 190) / 2;
                int qrY = 80;
                int qrSize = 190;

                using (var qrBgBrush = new SolidBrush(Color.White))
                using (var qrBorderPen = new Pen(Color.FromArgb(203, 213, 225), 1.5f))
                {
                    g.FillRectangle(qrBgBrush, qrX, qrY, qrSize, qrSize);
                    g.DrawRectangle(qrBorderPen, qrX, qrY, qrSize, qrSize);
                }

                DrawFinderPattern(g, qrX + 12, qrY + 12, 36);
                DrawFinderPattern(g, qrX + qrSize - 48, qrY + 12, 36);
                DrawFinderPattern(g, qrX + 12, qrY + qrSize - 48, 36);

                int seed = Math.Abs((transferContent + accountNo + amount).GetHashCode());
                var rand = new Random(seed);
                using (var pixelBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    int cellSize = 5;
                    for (int r = 0; r < 26; r++)
                    {
                        for (int c = 0; c < 26; c++)
                        {
                            bool inFinder1 = r < 9 && c < 9;
                            bool inFinder2 = r < 9 && c > 16;
                            bool inFinder3 = r > 16 && c < 9;
                            bool inCenterLogo = r >= 10 && r <= 15 && c >= 10 && c <= 15;

                            if (!inFinder1 && !inFinder2 && !inFinder3 && !inCenterLogo)
                            {
                                if (rand.Next(100) < 45)
                                {
                                    g.FillRectangle(pixelBrush, qrX + 15 + c * cellSize + 2, qrY + 15 + r * cellSize + 2, cellSize - 1, cellSize - 1);
                                }
                            }
                        }
                    }

                    // Logo nhỏ ở giữa QR
                    int logoX = qrX + (qrSize - 44) / 2;
                    int logoY = qrY + (qrSize - 44) / 2;
                    g.FillRectangle(new SolidBrush(Color.White), logoX, logoY, 44, 44);
                    g.DrawRectangle(new Pen(Color.FromArgb(217, 119, 6), 1.5f), logoX, logoY, 44, 44);
                    using (var logoFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                    {
                        g.DrawString("VIET\nQR", logoFont, new SolidBrush(Color.FromArgb(217, 119, 6)), new RectangleF(logoX, logoY + 6, 44, 30), new StringFormat { Alignment = StringAlignment.Center });
                    }
                }

                // 4. Thông tin thụ hưởng của độc giả phía dưới QR
                int infoY = qrY + qrSize + 12;
                using (var fontLabel = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (var fontValue = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (var fontAmount = new Font("Segoe UI", 12f, FontStyle.Bold))
                using (var brushMuted = new SolidBrush(Color.FromArgb(100, 116, 139)))
                using (var brushDark = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (var brushRefund = new SolidBrush(Color.FromArgb(180, 83, 9)))
                using (var brushWarn = new SolidBrush(Color.FromArgb(185, 28, 28)))
                {
                    g.DrawString($"Ngân hàng: {bankName}", fontValue, brushDark, new PointF(18, infoY));
                    g.DrawString($"STK: {accountNo}", fontValue, brushRefund, new PointF(18, infoY + 18));
                    g.DrawString($"Chủ TK: {accountHolder?.ToUpperInvariant()}", fontValue, brushDark, new PointF(18, infoY + 36));
                    g.DrawString($"Số tiền hoàn:", fontLabel, brushMuted, new PointF(18, infoY + 56));
                    g.DrawString($"{amount:N0} VNĐ", fontAmount, brushWarn, new PointF(100, infoY + 52));
                    
                    g.DrawString($"Nội dung:", fontLabel, brushMuted, new PointF(18, infoY + 76));
                    g.DrawString(transferContent, fontValue, brushDark, new PointF(75, infoY + 76));

                    g.DrawString("💡 Mở App Ngân hàng quét mã QR để chuyển khoản cho Độc giả", fontLabel, brushMuted, new PointF(10, infoY + 98));
                }
            }

            return bitmap;
        }

        private static void DrawFinderPattern(Graphics g, int x, int y, int size)
        {
            using (var outerBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (var innerWhiteBrush = new SolidBrush(Color.White))
            using (var centerBrush = new SolidBrush(Color.FromArgb(2, 132, 199)))
            {
                g.FillRectangle(outerBrush, x, y, size, size);
                g.FillRectangle(innerWhiteBrush, x + 5, y + 5, size - 10, size - 10);
                g.FillRectangle(centerBrush, x + 10, y + 10, size - 20, size - 20);
            }
        }
    }

    public class BankInfoItem
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }

        public override string ToString() => Name;
    }
}
