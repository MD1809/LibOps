using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using LibOps.CommonUtilities.Formatting;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataAccessLayer.DataRepositories;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;
using LibOps.BusinessLogicLayer.BusinessServices.Notification;

namespace LibOps.BusinessLogicLayer.BusinessServices
{
    /// <summary>
    /// Dịch vụ nghiệp vụ dành cho Thủ thư xử lý yêu cầu Rút tiền cọc & Hủy thẻ của Độc giả
    /// </summary>
    public class ReaderRequestService
    {
        private readonly ReaderRequestRepository _requestRepo;
        private readonly MemberRepository _memberRepo;
        private readonly DepositRepository _depositRepo;
        private readonly UserRepository _userRepo;

        public ReaderRequestService()
        {
            _requestRepo = new ReaderRequestRepository();
            _memberRepo = new MemberRepository();
            _depositRepo = new DepositRepository();
            _userRepo = new UserRepository();
        }

        public ReaderRequestService(
            ReaderRequestRepository requestRepo,
            MemberRepository memberRepo,
            DepositRepository depositRepo,
            UserRepository userRepo)
        {
            _requestRepo = requestRepo;
            _memberRepo = memberRepo;
            _depositRepo = depositRepo;
            _userRepo = userRepo;
        }

        public virtual List<ReaderRequestDisplayDto> GetPendingReaderRequests()
        {
            return _requestRepo.GetAllRequests("PENDING", null, null);
        }

        public virtual List<ReaderRequestDisplayDto> GetAllReaderRequests(string status = null, string requestType = null, string keyword = null)
        {
            return _requestRepo.GetAllRequests(status, requestType, keyword);
        }

        /// <summary>
        /// Thủ thư phê duyệt yêu cầu Rút cọc / Hủy thẻ và thực thi giao dịch tài chính
        /// </summary>
        public virtual bool ApproveRefundRequest(
            int requestId, 
            int librarianUserId, 
            string staffNotes, 
            out Bitmap expenseReceiptBitmap, 
            out string errorMessage)
        {
            expenseReceiptBitmap = null;
            errorMessage = string.Empty;

            var request = _requestRepo.GetRequestById(requestId);
            if (request == null)
            {
                errorMessage = "Không tìm thấy thông tin yêu cầu cần duyệt.";
                return false;
            }

            if (request.Status != "PENDING")
            {
                errorMessage = $"Yêu cầu đã được xử lý từ trước với trạng thái '{request.Status}'.";
                return false;
            }

            var member = _memberRepo.GetMemberById(request.MemberId);
            if (member == null)
            {
                errorMessage = "Không tìm thấy hồ sơ độc giả liên quan.";
                return false;
            }

            // 1. Kiểm tra an toàn lần cuối: độc giả không được giữ bất kỳ cuốn sách nào
            int borrowingCount = _memberRepo.GetCurrentlyBorrowingCount(member.MemberId);
            if (borrowingCount > 0)
            {
                errorMessage = $"Độc giả hiện đang mượn {borrowingCount} cuốn sách chưa trả. Không thể phê duyệt hoàn cọc!";
                return false;
            }

            decimal refundAmount = request.Amount;
            if (refundAmount > member.DepositBalance)
            {
                refundAmount = Math.Max(0, member.DepositBalance);
            }

            decimal newBalance = member.DepositBalance - refundAmount;
            string receiptCode = $"PC{DateTime.Now:yyyyMMddHHmmss}";

            using (var conn = DatabaseConnectionHelper.CreateAndOpenConnection())
            using (var trans = conn.BeginTransaction())
            {
                try
                {
                    // 2. Khấu trừ số dư cọc
                    _memberRepo.UpdateDepositBalance(member.MemberId, newBalance, trans);

                    // 3. Ghi log giao dịch hoàn cọc nếu có hoàn tiền
                    if (refundAmount > 0)
                    {
                        var tx = new DepositTransactionEntity
                        {
                            MemberId = member.MemberId,
                            HandledByUserId = librarianUserId,
                            TransactionType = "REFUND",
                            Amount = -refundAmount,
                            BalanceAfter = newBalance,
                            ReceiptCode = receiptCode,
                            TransactionDate = DateTime.Now,
                            Notes = $"Hoàn tiền cọc theo yêu cầu #{request.RequestCode} ({request.PayoutMethod})"
                        };
                        _depositRepo.InsertTransaction(tx, trans);
                    }

                    // 4. Nếu là Hủy thẻ: đóng thẻ, xóa nợ và vô hiệu hóa tài khoản đăng nhập
                    if (request.RequestType == "CANCEL_CARD")
                    {
                        _memberRepo.UpdateCardStatus(member.MemberId, "CLOSED", trans);
                        _memberRepo.UpdateDepositAndDebt(member.MemberId, 0, 0, trans);

                        var readerUser = _userRepo.GetUserByMemberId(member.MemberId, trans);
                        if (readerUser != null)
                        {
                            _userRepo.UpdateUserStatus(readerUser.UserId, false, trans);
                        }
                    }

                    // 5. Cập nhật trạng thái yêu cầu sang APPROVED
                    string finalNotes = string.IsNullOrWhiteSpace(staffNotes) ? "Thủ thư đã duyệt hoàn tiền" : staffNotes;
                    _requestRepo.UpdateRequestStatus(requestId, "APPROVED", librarianUserId, finalNotes, trans);

                    trans.Commit();

                    // Gửi email xác nhận tất toán hoàn cọc / hủy thẻ tự động (Non-blocking)
                    EmailNotificationService.Instance.SendCardClosureRefundEmailAsync(
                        member.Email,
                        member.FullName,
                        member.MemberCardCode,
                        receiptCode,
                        refundAmount,
                        request.PayoutMethod,
                        request.BankName,
                        request.BankAccountNumber,
                        request.BankAccountHolder,
                        DateTime.Now
                    );

                    // 6. Sinh ảnh Phiếu Chi GDI+ Bitmap
                    var librarianUser = _userRepo.GetUserById(librarianUserId);
                    string staffName = librarianUser != null ? librarianUser.FullName : "Thủ thư";
                    expenseReceiptBitmap = GenerateExpenseReceiptBitmap(request, member, refundAmount, receiptCode, staffName);

                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    errorMessage = "Lỗi khi xử lý phê duyệt: " + ex.Message;
                    return false;
                }
            }
        }

        /// <summary>
        /// Thủ thư từ chối yêu cầu của Độc giả kèm lý do
        /// </summary>
        public virtual bool RejectRequest(int requestId, int librarianUserId, string reasonNote, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(reasonNote))
            {
                errorMessage = "Vui lòng nhập lý do từ chối để độc giả được biết.";
                return false;
            }

            var request = _requestRepo.GetRequestById(requestId);
            if (request == null)
            {
                errorMessage = "Không tìm thấy thông tin yêu cầu.";
                return false;
            }

            if (request.Status != "PENDING")
            {
                errorMessage = $"Yêu cầu đã ở trạng thái '{request.Status}'. Không thể thao tác!";
                return false;
            }

            bool ok = _requestRepo.UpdateRequestStatus(requestId, "REJECTED", librarianUserId, reasonNote.Trim());
            if (ok)
            {
                return true;
            }

            errorMessage = "Từ chối yêu cầu thất bại.";
            return false;
        }

        /// <summary>
        /// Phê duyệt và xử lý các yêu cầu chung (Báo mất cấp lại thẻ, Cập nhật thông tin, Góp ý)
        /// </summary>
        public virtual bool ApproveGeneralRequest(
            int requestId, 
            int librarianUserId, 
            string staffNotes, 
            out string errorMessage)
        {
            errorMessage = string.Empty;

            var request = _requestRepo.GetRequestById(requestId);
            if (request == null)
            {
                errorMessage = "Không tìm thấy thông tin yêu cầu cần duyệt.";
                return false;
            }

            if (request.Status != "PENDING")
            {
                errorMessage = $"Yêu cầu đã được xử lý từ trước với trạng thái '{request.Status}'.";
                return false;
            }

            var member = _memberRepo.GetMemberById(request.MemberId);
            if (member == null)
            {
                errorMessage = "Không tìm thấy hồ sơ độc giả liên quan.";
                return false;
            }

            using (var conn = DatabaseConnectionHelper.CreateAndOpenConnection())
            using (var trans = conn.BeginTransaction())
            {
                try
                {
                    // 1. Nếu là Cấp lại thẻ (REISSUE_CARD): Mở khóa thẻ lại sang ACTIVE
                    if (request.RequestType == "REISSUE_CARD")
                    {
                        if (member.CardStatus == "LOCKED")
                        {
                            _memberRepo.UpdateCardStatus(member.MemberId, "ACTIVE", trans);
                        }
                    }

                    // 2. Nếu là Cập nhật thông tin (UPDATE_INFO): Trích xuất thông tin mới nếu có
                    if (request.RequestType == "UPDATE_INFO" && !string.IsNullOrWhiteSpace(request.Reason))
                    {
                        // Parse định dạng [CẬP NHẬT] SĐT: ... | Email: ... | Địa chỉ: ...
                        try
                        {
                            string text = request.Reason;
                            string phone = member.PhoneNumber;
                            string email = member.Email;
                            string address = member.Address;

                            if (text.Contains("SĐT:"))
                            {
                                int pStart = text.IndexOf("SĐT:") + 4;
                                int pEnd = text.IndexOf("|", pStart);
                                if (pEnd > pStart)
                                {
                                    string pVal = text.Substring(pStart, pEnd - pStart).Trim();
                                    if (!string.IsNullOrWhiteSpace(pVal)) phone = pVal;
                                }
                            }
                            if (text.Contains("Email:"))
                            {
                                int eStart = text.IndexOf("Email:") + 6;
                                int eEnd = text.IndexOf("|", eStart);
                                if (eEnd > eStart)
                                {
                                    string eVal = text.Substring(eStart, eEnd - eStart).Trim();
                                    if (!string.IsNullOrWhiteSpace(eVal)) email = eVal;
                                }
                            }
                            if (text.Contains("Địa chỉ:"))
                            {
                                int aStart = text.IndexOf("Địa chỉ:") + 8;
                                int aEnd = text.IndexOf("\r\n", aStart);
                                if (aEnd < 0) aEnd = text.IndexOf("\n", aStart);
                                if (aEnd < 0) aEnd = text.Length;
                                string aVal = text.Substring(aStart, aEnd - aStart).Trim();
                                if (!string.IsNullOrWhiteSpace(aVal)) address = aVal;
                            }

                            member.PhoneNumber = phone;
                            member.Email = email;
                            member.Address = address;
                            _memberRepo.UpdateMember(member, trans);

                            // Đồng bộ sang tài khoản UserAccounts nếu có
                            _userRepo.UpdateUserContactByMemberId(member.MemberId, member.FullName, email, phone, trans);
                        }
                        catch
                        {
                            // Bỏ qua lỗi parse, vẫn duyệt yêu cầu
                        }
                    }

                    // 3. Cập nhật trạng thái yêu cầu sang APPROVED
                    string finalNotes = string.IsNullOrWhiteSpace(staffNotes) ? "Thủ thư đã phê duyệt và xử lý yêu cầu" : staffNotes.Trim();
                    _requestRepo.UpdateRequestStatus(requestId, "APPROVED", librarianUserId, finalNotes, trans);

                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    errorMessage = "Lỗi khi xử lý phê duyệt: " + ex.Message;
                    return false;
                }
            }
        }

        /// <summary>
        /// Sinh mẫu in Phiếu Chi Hoàn Tiền Cọc chuẩn GDI+ Bitmap (340x440 px)
        /// </summary>
        public virtual Bitmap GenerateExpenseReceiptBitmap(
            ReaderRequestEntity request, 
            MemberEntity member, 
            decimal amount, 
            string receiptCode, 
            string librarianName)
        {
            int width = 340;
            int height = 440;
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                g.Clear(Color.White);

                using (var penBorder = new Pen(Color.FromArgb(148, 163, 184), 1.5f))
                {
                    g.DrawRectangle(penBorder, 2, 2, width - 4, height - 4);
                }

                using (var fontUni = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (var fontTitle = new Font("Segoe UI", 12f, FontStyle.Bold))
                using (var fontCode = new Font("Segoe UI", 8.5f, FontStyle.Italic))
                using (var fontRegular = new Font("Segoe UI", 9f, FontStyle.Regular))
                using (var fontBold = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (var fontAmount = new Font("Segoe UI", 12f, FontStyle.Bold))
                using (var brushDark = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (var brushPrimary = new SolidBrush(Color.FromArgb(2, 132, 199)))
                using (var brushDanger = new SolidBrush(Color.FromArgb(220, 38, 38)))
                using (var brushMuted = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    int y = 15;
                    g.DrawString("HỆ THỐNG THƯ VIỆN LIBOPS", fontUni, brushPrimary, new PointF(15, y));
                    y += 18;
                    g.DrawString("THƯ VIỆN ĐIỆN TỬ LIBOPS", fontUni, brushMuted, new PointF(15, y));
                    y += 25;

                    g.DrawLine(new Pen(Color.FromArgb(226, 232, 240), 1), 15, y, width - 15, y);
                    y += 12;

                    string titleText = request.RequestType == "CANCEL_CARD" ? "PHIẾU CHI HOÀN CỌC HỦY THẺ" : "PHIẾU CHI HOÀN TIỀN CỌC";
                    g.DrawString(titleText, fontTitle, brushDanger, new RectangleF(0, y, width, 25), new StringFormat { Alignment = StringAlignment.Center });
                    y += 26;

                    g.DrawString($"Số phiếu: {receiptCode} | Y/C: {request.RequestCode}", fontCode, brushMuted, new RectangleF(0, y, width, 20), new StringFormat { Alignment = StringAlignment.Center });
                    y += 24;

                    g.DrawString($"Họ tên độc giả: {member.FullName}", fontBold, brushDark, new PointF(15, y));
                    y += 20;
                    g.DrawString($"Mã thẻ: {member.MemberCardCode} | SĐT: {member.PhoneNumber}", fontRegular, brushDark, new PointF(15, y));
                    y += 20;
                    g.DrawString($"Hình thức nhận: {(request.PayoutMethod == "BANK_TRANSFER" ? "Chuyển khoản" : "Tiền mặt tại quầy")}", fontRegular, brushDark, new PointF(15, y));
                    y += 20;

                    if (request.PayoutMethod == "BANK_TRANSFER" && !string.IsNullOrWhiteSpace(request.BankAccountNumber))
                    {
                        g.DrawString($"STK: {request.BankAccountNumber} ({request.BankName})", fontRegular, brushDark, new PointF(15, y));
                        y += 20;
                        g.DrawString($"Chủ TK: {request.BankAccountHolder}", fontRegular, brushDark, new PointF(15, y));
                        y += 20;
                    }

                    g.DrawString($"Lý do: {request.Reason}", fontRegular, brushMuted, new PointF(15, y));
                    y += 25;

                    g.FillRectangle(new SolidBrush(Color.FromArgb(241, 245, 249)), 15, y, width - 30, 36);
                    g.DrawRectangle(new Pen(Color.FromArgb(203, 213, 225), 1), 15, y, width - 30, 36);
                    g.DrawString("Số tiền hoàn trả:", fontRegular, brushDark, new PointF(25, y + 8));
                    g.DrawString($"{amount:N0} VNĐ", fontAmount, brushDanger, new PointF(150, y + 6));
                    y += 48;

                    g.DrawString($"Ngày duyệt: {DateTime.Now:dd/MM/yyyy HH:mm}", fontRegular, brushMuted, new PointF(15, y));
                    y += 20;

                    // Chữ ký 2 bên
                    g.DrawString("Người nhận tiền\n(Ký, ghi rõ họ tên)", fontRegular, brushMuted, new RectangleF(15, y, 140, 40), new StringFormat { Alignment = StringAlignment.Center });
                    g.DrawString($"Thủ thư lập phiếu\n{librarianName}", fontBold, brushDark, new RectangleF(width - 155, y, 140, 40), new StringFormat { Alignment = StringAlignment.Center });
                }
            }

            return bitmap;
        }
    }
}
