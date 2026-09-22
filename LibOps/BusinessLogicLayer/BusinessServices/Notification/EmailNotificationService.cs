using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using LibOps.CommonUtilities.Constants;
using LibOps.DataAccessLayer.DataRepositories;

namespace LibOps.BusinessLogicLayer.BusinessServices.Notification
{
    /// <summary>
    /// Triển khai dịch vụ gửi email thông báo tự động (Non-blocking / Fire & Forget)
    /// </summary>
    public class EmailNotificationService : IEmailNotificationService
    {
        private static IEmailNotificationService _instance;
        public static IEmailNotificationService Instance => _instance ?? (_instance = new EmailNotificationService());

        private readonly SystemSettingRepository _settingRepo;

        public EmailNotificationService(SystemSettingRepository settingRepo = null)
        {
            _settingRepo = settingRepo ?? new SystemSettingRepository();
        }

        #region Helper: Lấy cấu hình SMTP từ CSDL

        private bool IsEmailNotificationEnabled()
        {
            try
            {
                string val = _settingRepo.GetSettingValue(SystemConstantDefinition.SettingKeys.SystemEmailNotificationEnabled, "true");
                return !string.Equals(val, "false", StringComparison.OrdinalIgnoreCase) && val != "0";
            }
            catch
            {
                return true;
            }
        }

        private (string host, int port, bool enableSsl, string user, string pass, string fromName) GetSmtpConfig()
        {
            string host = (_settingRepo.GetSettingValue(SystemConstantDefinition.SettingKeys.SmtpHost, "smtp.gmail.com") ?? "smtp.gmail.com").Trim();
            string portStr = _settingRepo.GetSettingValue(SystemConstantDefinition.SettingKeys.SmtpPort, "587");
            int.TryParse(portStr, out int port);
            if (port <= 0) port = 587;

            string sslStr = _settingRepo.GetSettingValue(SystemConstantDefinition.SettingKeys.SmtpEnableSsl, "true");
            bool enableSsl = !string.Equals(sslStr, "false", StringComparison.OrdinalIgnoreCase) && sslStr != "0";

            string user = (_settingRepo.GetSettingValue(SystemConstantDefinition.SettingKeys.SmtpUsername, "") ?? "").Trim();
            string rawPass = _settingRepo.GetSettingValue(SystemConstantDefinition.SettingKeys.SmtpPassword, "") ?? "";
            string pass = rawPass.Replace(" ", "").Trim();
            string fromName = _settingRepo.GetSettingValue(SystemConstantDefinition.SettingKeys.SmtpFromName, "LibOps - Thư Viện Hiện Đại");

            return (host, port, enableSsl, user, pass, fromName);
        }

        #endregion

        #region Core Gửi Email Ngầm (Fire and Forget)

        private void SendEmailInBackground(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains("@"))
            {
                return; // Độc giả không có email hoặc email không hợp lệ -> bỏ qua an toàn
            }

            if (!IsEmailNotificationEnabled())
            {
                return; // Tính năng gửi mail đang tắt -> bỏ qua
            }

            // Chạy trong Task.Run để giải phóng UI thread tức thì
            Task.Run(() =>
            {
                try
                {
                    var config = GetSmtpConfig();
                    if (string.IsNullOrWhiteSpace(config.host) || string.IsNullOrWhiteSpace(config.user))
                    {
                        return;
                    }

                    using (var mail = new MailMessage())
                    {
                        mail.From = new MailAddress(config.user, config.fromName, Encoding.UTF8);
                        mail.To.Add(new MailAddress(toEmail.Trim()));
                        mail.Subject = subject;
                        mail.SubjectEncoding = Encoding.UTF8;
                        mail.Body = htmlBody;
                        mail.BodyEncoding = Encoding.UTF8;
                        mail.IsBodyHtml = true;

                        using (var smtp = new SmtpClient(config.host, config.port))
                        {
                            smtp.EnableSsl = config.enableSsl;
                            smtp.UseDefaultCredentials = false;
                            smtp.Credentials = new NetworkCredential(config.user, config.pass);
                            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                            smtp.Timeout = 15000; // 15s timeout
                            smtp.Send(mail);
                        }
                    }

                    Debug.WriteLine($"[EmailService] Đã gửi thành công email '{subject}' tới '{toEmail}'");
                }
                catch (Exception ex)
                {
                    // Ghi nhận log nhẹ nhàng, không gây ảnh hưởng đến ứng dụng
                    Debug.WriteLine($"[EmailService] Không thể gửi email tới '{toEmail}': {ex.Message}");
                }
            });
        }

        #endregion

        #region 4 Kịch Bản Nghiệp Vụ

        /// <summary>
        /// Kịch bản 1: Chào mừng & Cấp thẻ độc giả mới
        /// </summary>
        public void SendWelcomeNewMemberEmailAsync(
            string toEmail, 
            string memberName, 
            string memberCardCode, 
            DateTime expiryDate, 
            decimal initialDeposit, 
            string rawPassword)
        {
            string subject = $"[LibOps] Chúc Mừng Kích Hoạt Thẻ Thư Viện {memberCardCode} & Thông Tin Đăng Nhập";
            string body = HtmlEmailTemplateBuilder.BuildWelcomeNewMemberHtml(
                memberName, 
                memberCardCode, 
                expiryDate, 
                initialDeposit, 
                rawPassword);

            SendEmailInBackground(toEmail, subject, body);
        }

        /// <summary>
        /// Kịch bản 2: Thông báo biến động số dư tiền cọc & nạp tiền
        /// </summary>
        public void SendDepositBalanceChangeEmailAsync(
            string toEmail, 
            string memberName, 
            string memberCardCode, 
            string transactionCode, 
            decimal depositAmount, 
            decimal debtPaid, 
            decimal newDepositBalance, 
            DateTime transactionDate)
        {
            string subject = $"[LibOps] Thông Báo Biến Động Số Dư Tiền Cọc (+{depositAmount:N0} đ) - Thẻ {memberCardCode}";
            string body = HtmlEmailTemplateBuilder.BuildDepositBalanceChangeHtml(
                memberName, 
                memberCardCode, 
                transactionCode, 
                depositAmount, 
                debtPaid, 
                newDepositBalance, 
                transactionDate);

            SendEmailInBackground(toEmail, subject, body);
        }

        /// <summary>
        /// Kịch bản 3: Biên lai thu tiền phạt vi phạm & phí dịch vụ
        /// </summary>
        public void SendFineOrFeeReceiptEmailAsync(
            string toEmail, 
            string memberName, 
            string memberCardCode, 
            string receiptCode, 
            string feeOrFineType, 
            decimal amount, 
            string paymentMethodDisplay, 
            string detailDescription, 
            DateTime receiptDate)
        {
            string subject = $"[LibOps] Biên Lai Điện Tử Thu Tiền Phạt / Phí Dịch Vụ - #{receiptCode}";
            string body = HtmlEmailTemplateBuilder.BuildFineOrFeeReceiptHtml(
                memberName, 
                memberCardCode, 
                receiptCode, 
                feeOrFineType, 
                amount, 
                paymentMethodDisplay, 
                detailDescription, 
                receiptDate);

            SendEmailInBackground(toEmail, subject, body);
        }

        /// <summary>
        /// Kịch bản 4: Thông báo xác nhận tất toán hoàn cọc khi hủy thẻ
        /// </summary>
        public void SendCardClosureRefundEmailAsync(
            string toEmail, 
            string memberName, 
            string memberCardCode, 
            string requestCode, 
            decimal refundAmount, 
            string payoutMethod, 
            string bankName, 
            string accountNo, 
            string accountHolder, 
            DateTime closureDate)
        {
            string subject = $"[LibOps] Xác Nhận Tất Toán Hoàn Tiền Cọc ({refundAmount:N0} đ) & Đóng Thẻ {memberCardCode}";
            string body = HtmlEmailTemplateBuilder.BuildCardClosureRefundHtml(
                memberName, 
                memberCardCode, 
                requestCode, 
                refundAmount, 
                payoutMethod, 
                bankName, 
                accountNo, 
                accountHolder, 
                closureDate);

            SendEmailInBackground(toEmail, subject, body);
        }

        /// <summary>
        /// Kịch bản 5: Thông báo mượn sách thành công (kèm danh sách sách, thời gian mượn-trả)
        /// </summary>
        public void SendBookBorrowReceiptEmailAsync(
            string toEmail,
            string memberName,
            string memberCardCode,
            string slipCode,
            DateTime borrowDate,
            DateTime dueDate,
            System.Collections.Generic.List<LibOps.DataModels.DataTransferObjects.BorrowItemDisplayDto> items,
            string notes = null)
        {
            string subject = $"[LibOps] Xác Nhận Mượn Sách Thành Công - Phiếu Mượn #{slipCode} (Hạn trả: {dueDate:dd/MM/yyyy})";
            string body = HtmlEmailTemplateBuilder.BuildBookBorrowReceiptHtml(
                memberName,
                memberCardCode,
                slipCode,
                borrowDate,
                dueDate,
                items,
                notes);

            SendEmailInBackground(toEmail, subject, body);
        }

        /// <summary>
        /// Kịch bản 6: Thông báo xác nhận trả sách (kèm danh sách sách trả, số sách còn mượn, tiền phạt nếu có)
        /// </summary>
        public void SendBookReturnReceiptEmailAsync(
            string toEmail,
            string memberName,
            string memberCardCode,
            DateTime returnDate,
            System.Collections.Generic.List<LibOps.DataModels.DataTransferObjects.ReturnedBookItemDto> returnedBooks,
            int remainingBorrowCount,
            decimal totalFine = 0,
            string paymentMethod = null,
            string receiptCode = null)
        {
            int bookCount = returnedBooks?.Count ?? 1;
            string subject = $"[LibOps] Xác Nhận Hoàn Trả Tài Liệu ({bookCount} cuốn) - Thẻ {memberCardCode}";
            string body = HtmlEmailTemplateBuilder.BuildBookReturnReceiptHtml(
                memberName,
                memberCardCode,
                returnDate,
                returnedBooks,
                remainingBorrowCount,
                totalFine,
                paymentMethod,
                receiptCode);

            SendEmailInBackground(toEmail, subject, body);
        }

        #endregion

        #region Gửi Thử Nghiệm SMTP

        public async Task<bool> TestSmtpConnectionAsync(string testToEmail)
        {
            var config = GetSmtpConfig();
            return await TestSmtpConnectionAsync(
                testToEmail,
                config.host,
                config.port,
                config.enableSsl,
                config.user,
                config.pass,
                config.fromName
            );
        }

        public async Task<bool> TestSmtpConnectionAsync(
            string testToEmail, 
            string smtpHost, 
            int smtpPort, 
            bool enableSsl, 
            string smtpUser, 
            string smtpPass, 
            string fromName)
        {
            if (string.IsNullOrWhiteSpace(testToEmail) || !testToEmail.Contains("@"))
            {
                throw new ArgumentException("Email nhận thử nghiệm không hợp lệ!");
            }

            if (string.IsNullOrWhiteSpace(smtpHost))
            {
                throw new ArgumentException("Máy chủ SMTP không được để trống!");
            }

            if (string.IsNullOrWhiteSpace(smtpUser))
            {
                throw new ArgumentException("Tài khoản email gửi (Username) không được để trống!");
            }

            return await Task.Run(() =>
            {
                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(smtpUser, fromName ?? "LibOps Test", Encoding.UTF8);
                    mail.To.Add(new MailAddress(testToEmail.Trim()));
                    mail.Subject = "[LibOps] Kiểm Tra Kết Nối Máy Chủ Email (SMTP Connection Test)";
                    mail.SubjectEncoding = Encoding.UTF8;
                    mail.Body = HtmlEmailTemplateBuilder.BuildTestEmailHtml(fromName ?? "LibOps", DateTime.Now);
                    mail.BodyEncoding = Encoding.UTF8;
                    mail.IsBodyHtml = true;

                    using (var smtp = new SmtpClient(smtpHost, smtpPort))
                    {
                        smtp.EnableSsl = enableSsl;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(smtpUser, smtpPass);
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.Timeout = 10000; // 10s
                        smtp.Send(mail);
                    }
                }
                return true;
            });
        }

        #endregion
    }
}
