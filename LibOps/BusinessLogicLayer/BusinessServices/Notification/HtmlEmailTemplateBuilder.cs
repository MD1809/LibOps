using System;
using System.Collections.Generic;
using System.Text;
using LibOps.DataModels.DataTransferObjects;

namespace LibOps.BusinessLogicLayer.BusinessServices.Notification
{
    /// <summary>
    /// Bộ dựng mẫu Email HTML5 chuẩn văn bản thông báo học thuật / thư viện (Thuần Typography, không dùng Icon)
    /// </summary>
    public static class HtmlEmailTemplateBuilder
    {
        private static string WrapMasterTemplate(string title, string badgeText, string badgeBgColor, string badgeTextColor, string bodyContent)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html lang='vi'>");
            sb.Append("<head>");
            sb.Append("<meta charset='UTF-8'>");
            sb.Append("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.Append($"<title>{title}</title>");
            sb.Append("<style>");
            sb.Append("body { margin: 0; padding: 0; background-color: #F8FAFC; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased; color: #334155; }");
            sb.Append("table { border-collapse: collapse; width: 100%; }");
            sb.Append(".wrapper { width: 100%; padding: 24px 0; background-color: #F8FAFC; }");
            sb.Append(".email-card { max-width: 580px; margin: 0 auto; background: #FFFFFF; border-radius: 6px; overflow: hidden; border: 1px solid #E2E8F0; box-shadow: 0 1px 3px rgba(15, 23, 42, 0.05); }");
            sb.Append(".top-accent { height: 3px; background: #1E3A8A; width: 100%; }");
            sb.Append(".header { padding: 22px 28px 18px 28px; border-bottom: 1px solid #F1F5F9; }");
            sb.Append(".logo-text { font-size: 13.5px; font-weight: 700; letter-spacing: 0.8px; color: #0F172A; text-transform: uppercase; margin: 0; }");
            sb.Append(".logo-desc { font-size: 11px; color: #64748B; margin: 2px 0 0 0; }");
            sb.Append(".badge { display: inline-block; padding: 4px 10px; border-radius: 4px; font-size: 11.5px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; }");
            sb.Append(".content-body { padding: 28px; font-size: 13.5px; line-height: 1.65; color: #334155; }");
            sb.Append(".salutation { font-size: 14.5px; color: #0F172A; margin: 0 0 14px 0; }");
            sb.Append(".card-table { width: 100%; background: #F8FAFC; border: 1px solid #E2E8F0; border-radius: 6px; margin: 18px 0; }");
            sb.Append(".card-table td { padding: 9px 14px; font-size: 13px; border-bottom: 1px solid #EDF2F7; }");
            sb.Append(".card-table tr:last-child td { border-bottom: none; }");
            sb.Append(".label-col { color: #64748B; width: 42%; font-weight: 500; }");
            sb.Append(".value-col { color: #0F172A; text-align: right; }");
            sb.Append(".important-val { font-weight: 600; color: #0F172A; }");
            sb.Append(".notice-box { background: #F8FAFC; border-left: 3px solid #1E3A8A; padding: 12px 14px; border-radius: 0 4px 4px 0; margin: 18px 0; font-size: 12.5px; color: #475569; }");
            sb.Append(".footer { background: #F8FAFC; border-top: 1px solid #E2E8F0; padding: 20px 28px; text-align: left; font-size: 11.5px; color: #64748B; line-height: 1.5; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("<div class='wrapper'>");
            sb.Append("<div class='email-card'>");
            sb.Append("<div class='top-accent'></div>");

            // Header chuẩn thư viện (Thuần Text)
            sb.Append("<div class='header'>");
            sb.Append("<table style='width: 100%;'><tr>");
            sb.Append("<td style='vertical-align: middle;'>");
            sb.Append("<p class='logo-text'>THƯ VIỆN LIBOPS</p>");
            sb.Append("<p class='logo-desc'>Hệ Thống Thư Viện Hiện Đại</p>");
            sb.Append("</td>");
            sb.Append("<td style='text-align: right; vertical-align: middle;'>");
            sb.Append($"<span class='badge' style='background: {badgeBgColor}; color: {badgeTextColor};'>{badgeText}</span>");
            sb.Append("</td>");
            sb.Append("</tr></table>");
            sb.Append("</div>");

            // Body
            sb.Append("<div class='content-body'>");
            sb.Append(bodyContent);
            sb.Append("</div>");

            // Footer
            sb.Append("<div class='footer'>");
            sb.Append("<p style='margin: 0 0 4px 0;'>- Email này được gửi tự động từ <strong>Hệ Thống Thư Viện LibOps</strong>. Vui lòng không trả lời trực tiếp thư này.</p>");
            sb.Append("<p style='margin: 0;'>- Mọi yêu cầu hỗ trợ hoặc thắc mắc, quý bạn đọc vui lòng liên hệ tại quầy thủ thư hoặc gửi yêu cầu trên Cổng Tự Phục Vụ.</p>");
            sb.Append("</div>");

            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</body>");
            sb.Append("</html>");

            return sb.ToString();
        }

        /// <summary>
        /// Kịch bản 1: Chào mừng và cấp thẻ độc giả mới
        /// </summary>
        public static string BuildWelcomeNewMemberHtml(
            string memberName,
            string memberCardCode,
            DateTime expiryDate,
            decimal initialDeposit,
            string rawPassword)
        {
            var content = new StringBuilder();
            content.Append($"<p class='salutation'>Kính gửi Quý bạn đọc <strong>{memberName}</strong>,</p>");
            content.Append("<p>Hệ thống Thư viện LibOps xin thông báo thẻ độc giả điện tử của bạn đã được kích hoạt thành công. Bạn có thể sử dụng mã thẻ để tra cứu tài liệu, mượn sách và truy cập các dịch vụ tự phục vụ.</p>");

            content.Append("<table class='card-table'>");
            content.Append($"<tr><td class='label-col'>Mã thẻ độc giả:</td><td class='value-col'><span class='important-val' style='color: #1E3A8A;'>{memberCardCode}</span></td></tr>");
            content.Append($"<tr><td class='label-col'>Họ và tên:</td><td class='value-col'>{memberName}</td></tr>");
            content.Append($"<tr><td class='label-col'>Hạn sử dụng:</td><td class='value-col'>{expiryDate:dd/MM/yyyy}</td></tr>");
            content.Append($"<tr><td class='label-col'>Tiền ký quỹ ban đầu:</td><td class='value-col'><span class='important-val'>{initialDeposit:N0} VNĐ</span></td></tr>");
            content.Append("</table>");

            content.Append("<div class='notice-box'>");
            content.Append("<div style='margin-bottom: 6px; font-weight: 600; color: #0F172A;'>Thông tin đăng nhập Cổng Độc Giả:</div>");
            content.Append($"• Tên đăng nhập: <strong>{memberCardCode}</strong><br/>");
            content.Append($"• Mật khẩu khởi tạo: <strong>{rawPassword}</strong><br/>");
            content.Append("<span style='font-size: 11.5px; color: #64748B;'>Lưu ý: Quý bạn đọc vui lòng đăng nhập và đổi mật khẩu cá nhân trong lần đầu tiên sử dụng để bảo đảm an toàn tài khoản.</span>");
            content.Append("</div>");

            content.Append("<p style='margin-bottom: 0;'>Trân trọng cảm ơn quý bạn đọc đã đồng hành cùng Thư viện LibOps.</p>");

            return WrapMasterTemplate("Kích Hoạt Thẻ Độc Giả - LibOps", "Kích Hoạt Thẻ", "#EFF6FF", "#1E40AF", content.ToString());
        }

        /// <summary>
        /// Kịch bản 2: Thông báo biến động số dư tiền cọc / Nạp tiền
        /// </summary>
        public static string BuildDepositBalanceChangeHtml(
            string memberName,
            string memberCardCode,
            string transactionCode,
            decimal depositAmount,
            decimal debtPaid,
            decimal newDepositBalance,
            DateTime transactionDate)
        {
            var content = new StringBuilder();
            content.Append($"<p class='salutation'>Kính gửi Quý bạn đọc <strong>{memberName}</strong>,</p>");
            content.Append("<p>Hệ thống Thư viện LibOps xác nhận giao dịch nạp tiền ký quỹ vào tài khoản thẻ của bạn đã hoàn tất với thông tin chi tiết như sau:</p>");

            content.Append("<table class='card-table'>");
            content.Append($"<tr><td class='label-col'>Mã giao dịch:</td><td class='value-col'>#{transactionCode}</td></tr>");
            content.Append($"<tr><td class='label-col'>Mã thẻ độc giả:</td><td class='value-col'>{memberCardCode}</td></tr>");
            content.Append($"<tr><td class='label-col'>Thời gian thực hiện:</td><td class='value-col'>{transactionDate:dd/MM/yyyy HH:mm}</td></tr>");
            content.Append($"<tr><td class='label-col'>Số tiền nạp:</td><td class='value-col'><span class='important-val' style='color: #059669;'>+{depositAmount:N0} VNĐ</span></td></tr>");

            if (debtPaid > 0)
            {
                content.Append($"<tr><td class='label-col'>Khấu trừ nợ phạt:</td><td class='value-col'>-{debtPaid:N0} VNĐ</td></tr>");
            }

            content.Append($"<tr><td class='label-col'>Số dư ký quỹ khả dụng:</td><td class='value-col'><span class='important-val' style='color: #1E3A8A;'>{newDepositBalance:N0} VNĐ</span></td></tr>");
            content.Append("</table>");

            content.Append("<p style='font-size: 12.5px; color: #64748B; margin-bottom: 0;'>Quý bạn đọc có thể tra cứu lịch sử giao dịch và sao kê chi tiết bất cứ lúc nào trên trang <em>Thẻ Thư Viện</em> tại Cổng Độc Giả.</p>");

            return WrapMasterTemplate("Biến Động Số Dư - LibOps", "Biến Động Số Dư", "#EFF6FF", "#1E40AF", content.ToString());
        }

        /// <summary>
        /// Kịch bản 3: Biên lai thu tiền phạt và Phí dịch vụ
        /// </summary>
        public static string BuildFineOrFeeReceiptHtml(
            string memberName,
            string memberCardCode,
            string receiptCode,
            string feeOrFineType,
            decimal amount,
            string paymentMethodDisplay,
            string detailDescription,
            DateTime receiptDate)
        {
            var content = new StringBuilder();
            content.Append($"<p class='salutation'>Kính gửi Quý bạn đọc <strong>{memberName}</strong>,</p>");
            content.Append("<p>Thư viện LibOps xin gửi xác nhận biên lai điện tử cho khoản thu phí / bồi thường vi phạm đã được thanh toán hoàn tất:</p>");

            content.Append("<table class='card-table'>");
            content.Append($"<tr><td class='label-col'>Mã biên lai:</td><td class='value-col'><span class='important-val' style='color: #1E3A8A;'>{receiptCode}</span></td></tr>");
            content.Append($"<tr><td class='label-col'>Mã thẻ độc giả:</td><td class='value-col'>{memberCardCode}</td></tr>");
            content.Append($"<tr><td class='label-col'>Khoản thu:</td><td class='value-col'>{feeOrFineType}</td></tr>");
            content.Append($"<tr><td class='label-col'>Diễn giải nội dung:</td><td class='value-col'>{detailDescription}</td></tr>");
            content.Append($"<tr><td class='label-col'>Hình thức thanh toán:</td><td class='value-col'>{paymentMethodDisplay}</td></tr>");
            content.Append($"<tr><td class='label-col'>Thời gian thanh toán:</td><td class='value-col'>{receiptDate:dd/MM/yyyy HH:mm}</td></tr>");
            content.Append($"<tr><td class='label-col'>Tổng tiền đã thanh toán:</td><td class='value-col'><span class='important-val'>{amount:N0} VNĐ</span></td></tr>");
            content.Append("</table>");

            content.Append("<p style='font-size: 12.5px; color: #64748B; margin-bottom: 0;'>Biên lai điện tử này có giá trị xác nhận thanh toán tài chính tại Thư viện LibOps.</p>");

            return WrapMasterTemplate("Biên Lai Điện Tử - LibOps", "Biên Lai Điện Tử", "#F1F5F9", "#334155", content.ToString());
        }

        /// <summary>
        /// Kịch bản 4: Xác nhận tất toán hoàn cọc khi hủy thẻ
        /// </summary>
        public static string BuildCardClosureRefundHtml(
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
            var content = new StringBuilder();
            content.Append($"<p class='salutation'>Kính gửi Quý bạn đọc <strong>{memberName}</strong>,</p>");
            content.Append($"<p>Thủ tục tất toán và đóng thẻ độc giả của bạn (yêu cầu #{requestCode}) đã được Ban quản lý Thư viện LibOps phê duyệt và hoàn tất.</p>");

            content.Append("<table class='card-table'>");
            content.Append($"<tr><td class='label-col'>Mã thẻ độc giả:</td><td class='value-col'>{memberCardCode}</td></tr>");
            content.Append($"<tr><td class='label-col'>Trạng thái thẻ:</td><td class='value-col'>Đã đóng (Closed)</td></tr>");
            content.Append($"<tr><td class='label-col'>Thời điểm tất toán:</td><td class='value-col'>{closureDate:dd/MM/yyyy HH:mm}</td></tr>");
            content.Append($"<tr><td class='label-col'>Tiền cọc hoàn trả:</td><td class='value-col'><span class='important-val'>{refundAmount:N0} VNĐ</span></td></tr>");

            bool isBank = !string.Equals(payoutMethod, "CASH", StringComparison.OrdinalIgnoreCase);
            if (isBank)
            {
                content.Append($"<tr><td class='label-col'>Hình thức nhận:</td><td class='value-col'>Chuyển khoản ngân hàng</td></tr>");
                content.Append($"<tr><td class='label-col'>Ngân hàng thụ hưởng:</td><td class='value-col'>{bankName}</td></tr>");
                content.Append($"<tr><td class='label-col'>Số tài khoản:</td><td class='value-col'>{accountNo}</td></tr>");
                content.Append($"<tr><td class='label-col'>Chủ tài khoản:</td><td class='value-col'>{accountHolder?.ToUpperInvariant()}</td></tr>");
            }
            else
            {
                content.Append($"<tr><td class='label-col'>Hình thức nhận:</td><td class='value-col'>Tiền mặt tại quầy</td></tr>");
            }

            content.Append("</table>");

            content.Append("<p style='font-size: 12.5px; color: #64748B; margin-bottom: 0;'>Toàn bộ tài khoản và dịch vụ liên quan đã được đóng an toàn. Cảm ơn bạn đã sử dụng dịch vụ của Thư viện LibOps.</p>");

            return WrapMasterTemplate("Tất Toán Đóng Thẻ - LibOps", "Tất Toán Hoàn Cọc", "#F1F5F9", "#334155", content.ToString());
        }

        /// <summary>
        /// Kịch bản 5: Thông báo mượn sách thành công (Danh sách sách mượn, thời gian mượn-trả)
        /// </summary>
        public static string BuildBookBorrowReceiptHtml(
            string memberName,
            string memberCardCode,
            string slipCode,
            DateTime borrowDate,
            DateTime dueDate,
            List<BorrowItemDisplayDto> items,
            string notes = null)
        {
            var content = new StringBuilder();
            content.Append($"<p class='salutation'>Kính gửi Quý bạn đọc <strong>{memberName}</strong>,</p>");
            content.Append($"<p>Hệ thống Thư viện LibOps xác nhận bạn vừa hoàn tất thủ tục mượn tài liệu tại quầy. Dưới đây là thông tin phiếu mượn và danh sách sách bàn giao:</p>");

            content.Append("<table class='card-table'>");
            content.Append($"<tr><td class='label-col'>Mã phiếu mượn:</td><td class='value-col'><span class='important-val' style='color: #1E3A8A;'>#{slipCode}</span></td></tr>");
            content.Append($"<tr><td class='label-col'>Mã thẻ độc giả:</td><td class='value-col'>{memberCardCode}</td></tr>");
            content.Append($"<tr><td class='label-col'>Thời gian mượn:</td><td class='value-col'>{borrowDate:dd/MM/yyyy HH:mm}</td></tr>");
            content.Append($"<tr><td class='label-col'>Hạn trả tài liệu:</td><td class='value-col'><span class='important-val' style='color: #DC2626;'>{dueDate:dd/MM/yyyy}</span></td></tr>");
            content.Append($"<tr><td class='label-col'>Số lượng tài liệu:</td><td class='value-col'>{items?.Count ?? 0} cuốn</td></tr>");

            if (!string.IsNullOrWhiteSpace(notes))
            {
                content.Append($"<tr><td class='label-col'>Ghi chú:</td><td class='value-col'>{notes}</td></tr>");
            }
            content.Append("</table>");

            // Bảng danh sách sách mượn
            content.Append("<p style='font-size: 13px; font-weight: 600; color: #0F172A; margin: 20px 0 8px 0;'>DANH SÁCH TÀI LIỆU MƯỢN</p>");
            content.Append("<table style='width: 100%; border-collapse: collapse; border: 1px solid #E2E8F0; border-radius: 6px; overflow: hidden; margin-bottom: 16px;'>");
            content.Append("<thead>");
            content.Append("<tr style='background: #F8FAFC; text-align: left; font-size: 11.5px; color: #64748B;'>");
            content.Append("<th style='padding: 8px 10px; border-bottom: 1px solid #E2E8F0; width: 30px; text-align: center;'>#</th>");
            content.Append("<th style='padding: 8px 10px; border-bottom: 1px solid #E2E8F0; width: 90px;'>Mã Vạch</th>");
            content.Append("<th style='padding: 8px 10px; border-bottom: 1px solid #E2E8F0;'>Tên Sách</th>");
            content.Append("<th style='padding: 8px 10px; border-bottom: 1px solid #E2E8F0; width: 75px;'>Vị Trí</th>");
            content.Append("<th style='padding: 8px 10px; border-bottom: 1px solid #E2E8F0; width: 75px;'>Tình Trạng</th>");
            content.Append("</tr>");
            content.Append("</thead>");
            content.Append("<tbody>");

            if (items != null && items.Count > 0)
            {
                int stt = 1;
                foreach (var item in items)
                {
                    string rowBg = (stt % 2 == 0) ? "#FAFAFA" : "#FFFFFF";
                    content.Append($"<tr style='background: {rowBg}; font-size: 12.5px; border-bottom: 1px solid #F1F5F9;'>");
                    content.Append($"<td style='padding: 8px 10px; text-align: center; color: #64748B;'>{stt++}</td>");
                    content.Append($"<td style='padding: 8px 10px; font-family: monospace; color: #1E3A8A;'>{item.Barcode}</td>");
                    content.Append($"<td style='padding: 8px 10px; color: #0F172A;'>{item.Title}</td>");
                    content.Append($"<td style='padding: 8px 10px; color: #64748B;'>{item.ShelfLocation}</td>");
                    content.Append($"<td style='padding: 8px 10px; color: #64748B;'>{(string.IsNullOrWhiteSpace(item.ConditionNote) ? "Nguyên vẹn" : item.ConditionNote)}</td>");
                    content.Append("</tr>");
                }
            }

            content.Append("</tbody>");
            content.Append("</table>");

            // Lưu ý trả sách
            content.Append("<div class='notice-box'>");
            content.Append("<div style='margin-bottom: 4px; font-weight: 600; color: #0F172A;'>Lưu ý hạn trả tài liệu:</div>");
            content.Append($"- Quý độc giả vui lòng hoàn trả sách trước hoặc trong ngày <strong>{dueDate:dd/MM/yyyy}</strong>.<br/>");
            content.Append("- Có thể thực hiện gia hạn trực tuyến trên Cổng Độc Giả trước ngày hết hạn.<br/>");
            content.Append("- Vui lòng giữ gìn và bảo quản tài liệu cẩn thận trong suốt thời gian mượn.");
            content.Append("</div>");

            content.Append("<p style='margin-bottom: 0;'>Chúc quý bạn đọc có thời gian nghiên cứu và đọc sách hiệu quả.</p>");

            return WrapMasterTemplate("Xác Nhận Mượn Sách - LibOps", "Mượn Sách", "#EFF6FF", "#1E40AF", content.ToString());
        }

        /// <summary>
        /// Kịch bản 6: Thông báo xác nhận trả sách (Danh sách sách trả, số sách còn mượn, tiền phạt nếu có)
        /// </summary>
        public static string BuildBookReturnReceiptHtml(
            string memberName,
            string memberCardCode,
            DateTime returnDate,
            List<ReturnedBookItemDto> returnedBooks,
            int remainingBorrowCount,
            decimal totalFine = 0,
            string paymentMethod = null,
            string receiptCode = null)
        {
            var content = new StringBuilder();
            content.Append($"<p class='salutation'>Kính gửi Quý bạn đọc <strong>{memberName}</strong>,</p>");
            content.Append("<p>Hệ thống Thư viện LibOps xác nhận bạn vừa hoàn tất thủ tục hoàn trả tài liệu tại quầy. Dưới đây là thông tin biên nhận chi tiết:</p>");

            content.Append("<table class='card-table'>");
            content.Append($"<tr><td class='label-col'>Mã thẻ độc giả:</td><td class='value-col'>{memberCardCode}</td></tr>");
            content.Append($"<tr><td class='label-col'>Thời gian trả:</td><td class='value-col'>{returnDate:dd/MM/yyyy HH:mm}</td></tr>");
            content.Append($"<tr><td class='label-col'>Số lượng đã trả:</td><td class='value-col'><span class='important-val'>{returnedBooks?.Count ?? 0} cuốn</span></td></tr>");
            content.Append($"<tr><td class='label-col'>Sách còn đang mượn:</td><td class='value-col'><span class='important-val' style='color: #1E3A8A;'>{remainingBorrowCount} cuốn</span></td></tr>");

            if (totalFine > 0)
            {
                content.Append($"<tr><td class='label-col'>Tổng tiền phạt xử lý:</td><td class='value-col'><span class='important-val' style='color: #DC2626;'>{totalFine:N0} VNĐ</span></td></tr>");
                if (!string.IsNullOrWhiteSpace(paymentMethod))
                {
                    content.Append($"<tr><td class='label-col'>Hình thức thanh toán:</td><td class='value-col'>{paymentMethod}</td></tr>");
                }
                if (!string.IsNullOrWhiteSpace(receiptCode))
                {
                    content.Append($"<tr><td class='label-col'>Mã biên lai phạt:</td><td class='value-col'>#{receiptCode}</td></tr>");
                }
            }
            content.Append("</table>");

            // Bảng danh sách sách đã trả
            content.Append("<p style='font-size: 13px; font-weight: 600; color: #0F172A; margin: 20px 0 8px 0;'>DANH SÁCH TÀI LIỆU HOÀN TRẢ</p>");
            content.Append("<table style='width: 100%; border-collapse: collapse; border: 1px solid #E2E8F0; border-radius: 6px; overflow: hidden; margin-bottom: 16px;'>");
            content.Append("<thead>");
            content.Append("<tr style='background: #F8FAFC; text-align: left; font-size: 11.5px; color: #64748B;'>");
            content.Append("<th style='padding: 8px 10px; border-bottom: 1px solid #E2E8F0; width: 30px; text-align: center;'>#</th>");
            content.Append("<th style='padding: 8px 10px; border-bottom: 1px solid #E2E8F0; width: 90px;'>Mã Vạch</th>");
            content.Append("<th style='padding: 8px 10px; border-bottom: 1px solid #E2E8F0;'>Tên Sách</th>");
            content.Append("<th style='padding: 8px 10px; border-bottom: 1px solid #E2E8F0; width: 85px;'>Tình Trạng</th>");
            content.Append("<th style='padding: 8px 10px; border-bottom: 1px solid #E2E8F0; width: 80px; text-align: right;'>Tiền Phạt</th>");
            content.Append("</tr>");
            content.Append("</thead>");
            content.Append("<tbody>");

            if (returnedBooks != null && returnedBooks.Count > 0)
            {
                int stt = 1;
                foreach (var item in returnedBooks)
                {
                    string rowBg = (stt % 2 == 0) ? "#FAFAFA" : "#FFFFFF";
                    string statusDisplay = item.ConditionDisplay;
                    if (!string.IsNullOrWhiteSpace(item.ConditionNote) && item.ConditionNote != "Nguyên vẹn")
                    {
                        statusDisplay += $" ({item.ConditionNote})";
                    }

                    string fineText = item.TotalFine > 0 ? $"{item.TotalFine:N0} đ" : "-";
                    string fineStyle = item.TotalFine > 0 ? "color: #DC2626; font-weight: 600;" : "color: #64748B;";

                    content.Append($"<tr style='background: {rowBg}; font-size: 12.5px; border-bottom: 1px solid #F1F5F9;'>");
                    content.Append($"<td style='padding: 8px 10px; text-align: center; color: #64748B;'>{stt++}</td>");
                    content.Append($"<td style='padding: 8px 10px; font-family: monospace; color: #1E3A8A;'>{item.Barcode}</td>");
                    content.Append($"<td style='padding: 8px 10px; color: #0F172A;'>{item.Title}</td>");
                    content.Append($"<td style='padding: 8px 10px; color: #475569;'>{statusDisplay}</td>");
                    content.Append($"<td style='padding: 8px 10px; text-align: right; {fineStyle}'>{fineText}</td>");
                    content.Append("</tr>");
                }
            }

            content.Append("</tbody>");
            content.Append("</table>");

            // Lưu ý & thông tin trạng thái tài khoản
            content.Append("<div class='notice-box'>");
            content.Append("<div style='margin-bottom: 4px; font-weight: 600; color: #0F172A;'>Thông tin tài khoản mượn - trả:</div>");
            if (remainingBorrowCount > 0)
            {
                content.Append($"- Bạn hiện còn <strong>{remainingBorrowCount}</strong> cuốn sách đang mượn. Quý bạn đọc vui lòng chú ý theo dõi hạn trả trên Cổng Độc Giả.<br/>");
            }
            else
            {
                content.Append("- Bạn đã hoàn trả tất cả tài liệu đã mượn. Tài khoản mượn hiện ở trạng thái sẵn sàng cho các lượt mượn tiếp theo.<br/>");
            }
            content.Append("- Cảm ơn quý bạn đọc đã giữ gìn và hoàn trả tài liệu đúng quy định thư viện.");
            content.Append("</div>");

            content.Append("<p style='margin-bottom: 0;'>Chúc quý bạn đọc có thêm nhiều trải nghiệm đọc sách bổ ích cùng Thư viện LibOps.</p>");

            return WrapMasterTemplate("Xác Nhận Trả Sách - LibOps", "Trả Sách", "#EFF6FF", "#1E40AF", content.ToString());
        }

        /// <summary>
        /// Email thử nghiệm kết nối SMTP (Dành cho Quản trị viên)
        /// </summary>
        public static string BuildTestEmailHtml(string senderName, DateTime testDate)
        {
            var content = new StringBuilder();
            content.Append("<p class='salutation'>Kính gửi Quản Trị Viên Hệ Thống,</p>");
            content.Append("<p>Đây là thư thông báo kiểm tra kết nối từ <strong>Hệ Thống Thư Viện LibOps</strong>. Dịch vụ gửi email thông báo tự động (SMTP) của hệ thống đã được thiết lập chính xác và hoạt động bình thường.</p>");

            content.Append("<table class='card-table'>");
            content.Append($"<tr><td class='label-col'>Trạng thái kết nối:</td><td class='value-col'><span class='important-val' style='color: #059669;'>Hoạt động bình thường</span></td></tr>");
            content.Append($"<tr><td class='label-col'>Tên người gửi:</td><td class='value-col'>{senderName}</td></tr>");
            content.Append($"<tr><td class='label-col'>Thời gian gửi:</td><td class='value-col'>{testDate:dd/MM/yyyy HH:mm:ss}</td></tr>");
            content.Append("</table>");

            content.Append("<p style='margin-bottom: 0;'>Hệ thống đã sẵn sàng xử lý các thông báo tự động cho độc giả.</p>");

            return WrapMasterTemplate("Kiểm Tra Kết Nối SMTP - LibOps", "Kiểm Tra Kết Nối", "#F1F5F9", "#334155", content.ToString());
        }
    }
}
