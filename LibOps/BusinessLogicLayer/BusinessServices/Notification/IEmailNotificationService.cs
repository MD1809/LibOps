using System;
using System.Threading.Tasks;

namespace LibOps.BusinessLogicLayer.BusinessServices.Notification
{
    /// <summary>
    /// Giao diện dịch vụ gửi email thông báo tự động (Non-blocking)
    /// </summary>
    public interface IEmailNotificationService
    {
        /// <summary>
        /// Kịch bản 1: Gửi thư chào mừng và thông tin tài khoản khi mở thẻ mới
        /// </summary>
        void SendWelcomeNewMemberEmailAsync(
            string toEmail, 
            string memberName, 
            string memberCardCode, 
            DateTime expiryDate, 
            decimal initialDeposit, 
            string rawPassword);

        /// <summary>
        /// Kịch bản 2: Gửi thông báo biến động số dư tiền cọc & nạp tiền (VietQR/Quầy)
        /// </summary>
        void SendDepositBalanceChangeEmailAsync(
            string toEmail, 
            string memberName, 
            string memberCardCode, 
            string transactionCode, 
            decimal depositAmount, 
            decimal debtPaid, 
            decimal newDepositBalance, 
            DateTime transactionDate);

        /// <summary>
        /// Kịch bản 3: Gửi biên lai thu tiền phạt vi phạm & phí dịch vụ
        /// </summary>
        void SendFineOrFeeReceiptEmailAsync(
            string toEmail, 
            string memberName, 
            string memberCardCode, 
            string receiptCode, 
            string feeOrFineType, 
            decimal amount, 
            string paymentMethodDisplay, 
            string detailDescription, 
            DateTime receiptDate);

        /// <summary>
        /// Kịch bản 4: Gửi thông báo xác nhận tất toán hoàn cọc khi hủy thẻ
        /// </summary>
        void SendCardClosureRefundEmailAsync(
            string toEmail, 
            string memberName, 
            string memberCardCode, 
            string requestCode, 
            decimal refundAmount, 
            string payoutMethod, 
            string bankName, 
            string accountNo, 
            string accountHolder, 
            DateTime closureDate);

        /// <summary>
        /// Kịch bản 5: Gửi thông báo xác nhận mượn sách thành công (kèm danh sách sách và thời hạn trả)
        /// </summary>
        void SendBookBorrowReceiptEmailAsync(
            string toEmail,
            string memberName,
            string memberCardCode,
            string slipCode,
            DateTime borrowDate,
            DateTime dueDate,
            System.Collections.Generic.List<LibOps.DataModels.DataTransferObjects.BorrowItemDisplayDto> items,
            string notes = null);

        /// <summary>
        /// Kịch bản 6: Gửi thông báo xác nhận hoàn trả sách (kèm danh sách sách trả, số sách còn mượn và tiền phạt nếu có)
        /// </summary>
        void SendBookReturnReceiptEmailAsync(
            string toEmail,
            string memberName,
            string memberCardCode,
            DateTime returnDate,
            System.Collections.Generic.List<LibOps.DataModels.DataTransferObjects.ReturnedBookItemDto> returnedBooks,
            int remainingBorrowCount,
            decimal totalFine = 0,
            string paymentMethod = null,
            string receiptCode = null);

        /// <summary>
        /// Gửi thử nghiệm kết nối SMTP theo cấu hình trong CSDL
        /// </summary>
        Task<bool> TestSmtpConnectionAsync(string testToEmail);

        /// <summary>
        /// Gửi thử nghiệm kết nối SMTP với các tham số cấu hình tùy chỉnh
        /// </summary>
        Task<bool> TestSmtpConnectionAsync(string testToEmail, string smtpHost, int smtpPort, bool enableSsl, string smtpUser, string smtpPass, string fromName);
    }
}
