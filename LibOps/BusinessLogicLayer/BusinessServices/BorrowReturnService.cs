using System;
using System.Collections.Generic;
using System.Drawing;
using LibOps.DataAccessLayer.DataRepositories;
using LibOps.DataAccessLayer.DatabaseTransactions;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;
using LibOps.BusinessLogicLayer.BusinessServices.Notification;

namespace LibOps.BusinessLogicLayer.BusinessServices
{
    /// <summary>
    /// Dịch vụ nghiệp vụ Quản lý Mượn & Trả Sách (Borrow & Return Workflow)
    /// </summary>
    public class BorrowReturnService
    {
        private readonly BorrowSlipRepository _slipRepo;
        private readonly BorrowDetailRepository _detailRepo;
        private readonly MemberRepository _memberRepo;
        private readonly BookRepository _bookRepo;
        private readonly BookCopyRepository _copyRepo;
        private readonly CategoryRepository _catRepo;
        private readonly SystemSettingsService _configService;

        public const int MAX_BOOKS_PER_MEMBER = 5;
        public const int DEFAULT_BORROW_DAYS = 14;

        public BorrowReturnService()
        {
            _slipRepo = new BorrowSlipRepository();
            _detailRepo = new BorrowDetailRepository();
            _memberRepo = new MemberRepository();
            _bookRepo = new BookRepository();
            _copyRepo = new BookCopyRepository();
            _catRepo = new CategoryRepository();
            _configService = new SystemSettingsService();
        }

        public BorrowReturnService(
            BorrowSlipRepository slipRepo,
            BorrowDetailRepository detailRepo,
            MemberRepository memberRepo,
            BookRepository bookRepo,
            BookCopyRepository copyRepo)
        {
            _slipRepo = slipRepo;
            _detailRepo = detailRepo;
            _memberRepo = memberRepo;
            _bookRepo = bookRepo;
            _copyRepo = copyRepo;
            _catRepo = new CategoryRepository();
            _configService = new SystemSettingsService();
        }

        public BorrowReturnService(
            BorrowSlipRepository slipRepo,
            BorrowDetailRepository detailRepo,
            MemberRepository memberRepo,
            BookRepository bookRepo,
            BookCopyRepository copyRepo,
            SystemSettingsService configService)
        {
            _slipRepo = slipRepo;
            _detailRepo = detailRepo;
            _memberRepo = memberRepo;
            _bookRepo = bookRepo;
            _copyRepo = copyRepo;
            _catRepo = new CategoryRepository();
            _configService = configService ?? new SystemSettingsService();
        }

        /// <summary>
        /// Kiểm tra 5 điều kiện cho phép mượn sách của độc giả
        /// </summary>
        public virtual bool ValidateBorrowEligibility(
            string memberCardCode, 
            int newItemsCount, 
            out string errorMessage, 
            out MemberEntity member, 
            out int currentBorrowCount)
        {
            errorMessage = string.Empty;
            currentBorrowCount = 0;
            member = null;

            if (string.IsNullOrWhiteSpace(memberCardCode))
            {
                errorMessage = "Vui lòng quét hoặc nhập mã thẻ độc giả.";
                return false;
            }

            member = _memberRepo.GetMemberByCardCode(memberCardCode);
            if (member == null)
            {
                errorMessage = $"Không tìm thấy thông tin độc giả với mã thẻ '{memberCardCode}'.";
                return false;
            }

            currentBorrowCount = _memberRepo.GetCurrentlyBorrowingCount(member.MemberId);

            // Điều kiện 1: Trạng thái thẻ & hạn sử dụng
            if (member.CardStatus != "ACTIVE")
            {
                errorMessage = $"Thẻ độc giả đang ở trạng thái '{member.CardStatus}' (Không hoạt động). Không thể mượn sách!";
                return false;
            }

            if (member.ExpiryDate.Date < DateTime.Today)
            {
                errorMessage = $"Thẻ độc giả đã hết hạn sử dụng vào ngày {member.ExpiryDate:dd/MM/yyyy}. Vui lòng gia hạn thẻ!";
                return false;
            }

            // Điều kiện 2: Kiểm tra công nợ và ngưỡng tiền cọc thế chân tối thiểu (1 Ví + Ngưỡng Ký Quỹ)
            if (member.TotalDebt > 0)
            {
                errorMessage = $"Tài khoản độc giả đang có khoản nợ phạt chưa thanh toán ({member.TotalDebt:N0} VNĐ). Vui lòng thanh toán hết nợ trước khi mượn sách!";
                return false;
            }

            decimal minDeposit = _configService.GetDefaultDeposit();
            if (member.DepositBalance < minDeposit)
            {
                errorMessage = $"Số dư tiền cọc ({member.DepositBalance:N0} VNĐ) không đạt ngưỡng ký quỹ tối thiểu ({minDeposit:N0} VNĐ). Vui lòng nạp thêm tiền cọc trước khi mượn sách!";
                return false;
            }

            // Điều kiện 3: Kiểm tra sách quá hạn chưa trả
            int overdueCount = _slipRepo.GetOverdueBorrowCountForMember(member.MemberId);
            if (overdueCount > 0)
            {
                errorMessage = $"Độc giả đang có {overdueCount} cuốn sách QUÁ HẠN chưa trả. Phải hoàn tất trả sách và nộp phạt trước khi mượn tiếp!";
                return false;
            }

            // Điều kiện 4: Hạn mức số lượng sách
            if (currentBorrowCount + newItemsCount > MAX_BOOKS_PER_MEMBER)
            {
                int remainingAllowed = Math.Max(0, MAX_BOOKS_PER_MEMBER - currentBorrowCount);
                errorMessage = $"Độc giả đang mượn {currentBorrowCount} cuốn. Chỉ được phép mượn thêm tối đa {remainingAllowed} cuốn (Hạn mức: {MAX_BOOKS_PER_MEMBER} cuốn/người)!";
                return false;
            }

            // Điều kiện 5: Kiểm tra yêu cầu rút tiền cọc hoặc hủy thẻ đang chờ xử lý (PENDING)
            if (_memberRepo.HasPendingReaderRequest(member.MemberId))
            {
                errorMessage = "Độc giả đang có yêu cầu (Rút tiền cọc / Hủy thẻ) ở trạng thái chờ duyệt. Không thể mượn sách mới cho đến khi yêu cầu được xử lý hoặc hủy!";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tra cứu và kiểm tra tính khả dụng của bản sao sách khi quét mã vạch
        /// </summary>
        public virtual BorrowItemDisplayDto GetBookCopyForBorrow(string barcode, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(barcode))
            {
                errorMessage = "Vui lòng quét hoặc nhập mã vạch cuốn sách.";
                return null;
            }

            var copy = _copyRepo.GetCopyByBarcode(barcode.Trim());
            if (copy == null)
            {
                errorMessage = $"Không tìm thấy cuốn sách nào có mã vạch '{barcode.Trim()}'.";
                return null;
            }

            if (copy.Status != "AVAILABLE")
            {
                string statusDesc = copy.Status;
                switch (copy.Status)
                {
                    case "BORROWED": statusDesc = "ĐÃ ĐƯỢC MƯỢN BỞI NGƯỜI KHÁC"; break;
                    case "DAMAGED": statusDesc = "ĐANG BỊ HƯ HỎNG / BẢO TRÌ"; break;
                    case "LOST": statusDesc = "ĐÃ BỊ MẤT"; break;
                }
                errorMessage = $"Cuốn sách '{barcode}' không sẵn sàng để mượn (Trạng thái: {statusDesc}).";
                return null;
            }

            var book = _bookRepo.GetBookById(copy.BookId);
            if (book == null)
            {
                errorMessage = "Không tìm thấy thông tin đầu sách gốc.";
                return null;
            }

            var category = _catRepo.GetCategoryById(book.CategoryId);
            string categoryName = category?.CategoryName ?? "Chung";

            return new BorrowItemDisplayDto
            {
                CopyId = copy.CopyId,
                BookId = book.BookId,
                Barcode = copy.Barcode,
                Title = book.Title,
                CategoryName = categoryName,
                ShelfLocation = book.ShelfLocation,
                Price = book.Price,
                ConditionNote = string.IsNullOrWhiteSpace(copy.ConditionNote) ? "Nguyên vẹn" : copy.ConditionNote
            };
        }

        public virtual string GenerateNextBorrowSlipCode()
        {
            DateTime today = DateTime.Today;
            int nextSeq = _slipRepo.GetNextSlipSequenceForDay(today);
            return $"PM{today:yyyyMMdd}{nextSeq:D4}";
        }

        /// <summary>
        /// Thực thi Transaction tạo phiếu mượn sách tại quầy
        /// </summary>
        public virtual bool CreateBorrowSlip(
            int memberId,
            int createdByUserId,
            DateTime borrowDate,
            DateTime dueDate,
            List<BorrowItemDisplayDto> items,
            string notes,
            out string slipCode,
            out string errorMessage)
        {
            slipCode = string.Empty;
            errorMessage = string.Empty;

            if (memberId <= 0)
            {
                errorMessage = "Độc giả không hợp lệ.";
                return false;
            }

            if (items == null || items.Count == 0)
            {
                errorMessage = "Danh sách sách mượn không được để trống.";
                return false;
            }

            if (items.Count > MAX_BOOKS_PER_MEMBER)
            {
                errorMessage = $"Số lượng sách vượt quá hạn mức cho phép (Tối đa {MAX_BOOKS_PER_MEMBER} cuốn).";
                return false;
            }

            slipCode = GenerateNextBorrowSlipCode();

            var slip = new BorrowSlipEntity
            {
                SlipCode = slipCode,
                MemberId = memberId,
                CreatedByUserId = createdByUserId > 0 ? createdByUserId : 1,
                BorrowDate = borrowDate,
                DueDate = dueDate,
                Status = "BORROWING",
                Notes = notes
            };

            using (var scope = new TransactionExecutionScope())
            {
                try
                {
                    // 1. Thêm bản ghi BorrowSlips
                    int slipId = _slipRepo.InsertBorrowSlip(slip, scope.Transaction);
                    if (slipId <= 0)
                    {
                        scope.Rollback();
                        errorMessage = "Lỗi hệ thống: Không thể tạo phiếu mượn sách.";
                        return false;
                    }

                    // 2. Thêm từng dòng BorrowSlipDetails & Cập nhật trạng thái bản sao, trừ tồn kho
                    foreach (var item in items)
                    {
                        var detail = new BorrowSlipDetailEntity
                        {
                            BorrowSlipId = slipId,
                            CopyId = item.CopyId,
                            BorrowConditionNote = string.IsNullOrWhiteSpace(item.ConditionNote) ? "Nguyên vẹn" : item.ConditionNote.Trim()
                        };

                        _detailRepo.InsertBorrowSlipDetail(detail, scope.Transaction);

                        // Cập nhật bản sao thành BORROWED
                        _copyRepo.UpdateCopyStatus(item.CopyId, "BORROWED", scope.Transaction);

                        // Đồng bộ lại số lượng khả dụng của đầu sách
                        _bookRepo.UpdateBookCopyCounts(item.BookId, scope.Transaction);
                    }

                    scope.Commit();

                    // Tự động gửi email xác nhận mượn sách kèm danh sách sách & thời hạn trả (Non-blocking)
                    try
                    {
                        var member = _memberRepo.GetMemberById(memberId);
                        if (member != null && !string.IsNullOrWhiteSpace(member.Email))
                        {
                            EmailNotificationService.Instance.SendBookBorrowReceiptEmailAsync(
                                member.Email,
                                member.FullName,
                                member.MemberCardCode,
                                slipCode,
                                borrowDate,
                                dueDate,
                                items,
                                notes
                            );
                        }
                    }
                    catch
                    {
                        // Bỏ qua lỗi gửi mail để không ảnh hưởng đến nghiệp vụ mượn sách
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    scope.Rollback();
                    errorMessage = "Lỗi khi lưu phiếu mượn: " + ex.Message;
                    return false;
                }
            }
        }

        /// <summary>
        /// Tạo hình ảnh Phiếu Mượn Sách dạng Bitmap chuẩn in nhiệt
        /// </summary>
        public virtual Bitmap GenerateBorrowSlipReceiptBitmap(
            string slipCode,
            string memberCardCode,
            string memberName,
            string phone,
            DateTime borrowDate,
            DateTime dueDate,
            List<BorrowItemDisplayDto> items,
            string librarianName)
        {
            int width = 360;
            int baseHeight = 360 + (items != null ? items.Count * 45 : 0);
            var bmp = new Bitmap(width, baseHeight);

            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);

                using (var fontTitle = new Font("Segoe UI", 11F, FontStyle.Bold))
                using (var fontSub = new Font("Segoe UI", 8.5F))
                using (var fontBold = new Font("Segoe UI", 9F, FontStyle.Bold))
                using (var fontRegular = new Font("Segoe UI", 8.5F))
                using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    g.DrawString("HỆ THỐNG THƯ VIỆN LIBOPS", fontTitle, brush, 60, 10);
                    g.DrawString("PHIẾU MƯỢN SÁCH TẠI QUẦY", fontBold, brush, 90, 32);
                    g.DrawString($"Mã phiếu: {slipCode}", fontBold, brush, 15, 60);
                    g.DrawString($"Ngày mượn: {borrowDate:dd/MM/yyyy HH:mm}", fontSub, brush, 15, 78);
                    g.DrawString($"HẠN TRẢ SÁCH: {dueDate:dd/MM/yyyy}", fontBold, brush, 15, 96);

                    using (var pen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                    {
                        g.DrawLine(pen, 15, 118, width - 15, 118);
                    }

                    g.DrawString($"Độc giả: {memberCardCode} - {memberName}", fontBold, brush, 15, 128);
                    g.DrawString($"Số điện thoại: {phone}", fontSub, brush, 15, 146);

                    using (var pen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                    {
                        g.DrawLine(pen, 15, 168, width - 15, 168);
                    }

                    g.DrawString("DANH SÁCH SÁCH MƯỢN:", fontBold, brush, 15, 178);

                    int currentY = 200;
                    if (items != null)
                    {
                        for (int i = 0; i < items.Count; i++)
                        {
                            var item = items[i];
                            g.DrawString($"{i + 1}. {item.Title}", fontBold, brush, 15, currentY);
                            g.DrawString($"   Mã vạch: {item.Barcode} | Kệ: {item.ShelfLocation}", fontSub, brush, 15, currentY + 18);
                            currentY += 40;
                        }
                    }

                    using (var pen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                    {
                        g.DrawLine(pen, 15, currentY + 10, width - 15, currentY + 10);
                    }

                    currentY += 20;
                    g.DrawString("LƯU Ý: Vui lòng bảo quản sách cẩn thận và trả đúng hạn.", fontSub, brush, 15, currentY);
                    g.DrawString("Phạt quá hạn: 5.000 VNĐ / ngày / cuốn sách.", fontSub, brush, 15, currentY + 16);

                    currentY += 40;
                    g.DrawString($"Thủ thư: {librarianName}", fontSub, brush, 15, currentY);
                    g.DrawString("Người mượn sách", fontBold, brush, 20, currentY + 25);
                    g.DrawString("Thủ thư lập phiếu", fontBold, brush, 220, currentY + 25);
                    g.DrawString("(Ký, ghi rõ họ tên)", fontSub, brush, 22, currentY + 45);
                    g.DrawString("(Ký, ghi rõ họ tên)", fontSub, brush, 225, currentY + 45);
                }

                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1))
                {
                    g.DrawRectangle(pen, 0, 0, width - 1, baseHeight - 1);
                }
            }

            return bmp;
        }

        public virtual List<BorrowSlipGridDisplayDto> GetBorrowSlips(string keyword, string status, DateTime? fromDate, DateTime? toDate, string transactionType = "ALL")
        {
            return _slipRepo.GetBorrowSlips(keyword, status, fromDate, toDate, transactionType);
        }

        public virtual List<BorrowSlipItemDetailDto> GetBorrowSlipItems(int borrowSlipId)
        {
            return _slipRepo.GetBorrowSlipItems(borrowSlipId);
        }

        public virtual List<BorrowSlipItemDetailDto> GetReturnTransactionItem(int returnDetailId)
        {
            return _slipRepo.GetReturnTransactionItem(returnDetailId);
        }

        public virtual List<MemberBorrowHistoryDto> GetMemberBorrowHistory(int memberId)
        {
            return _slipRepo.GetMemberBorrowHistory(memberId);
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ khi độc giả yêu cầu gia hạn phiếu mượn sách
        /// </summary>
        public virtual bool ValidateBorrowRenewal(
            int borrowSlipId, 
            out string errorMessage, 
            out BorrowSlipEntity slip, 
            out DateTime newDueDate,
            out MemberEntity member)
        {
            errorMessage = string.Empty;
            newDueDate = DateTime.Today;
            member = null;

            slip = _slipRepo.GetBorrowSlipById(borrowSlipId);
            if (slip == null)
            {
                errorMessage = "Không tìm thấy thông tin phiếu mượn cần gia hạn.";
                return false;
            }

            if (slip.Status == "RETURNED")
            {
                errorMessage = "Phiếu mượn đã hoàn tất trả sách. Không thể gia hạn!";
                return false;
            }

            if (slip.DueDate.Date < DateTime.Today)
            {
                errorMessage = $"Phiếu mượn đã quá hạn vào ngày {slip.DueDate:dd/MM/yyyy}. Độc giả vui lòng trả sách tại quầy và nộp phạt theo quy định, không thể gia hạn!";
                return false;
            }

            int maxRenewals = _configService.GetMaxRenewalCount();
            if (slip.RenewalCount >= maxRenewals)
            {
                errorMessage = $"Phiếu mượn đã đạt giới hạn gia hạn tối đa ({slip.RenewalCount}/{maxRenewals} lần). Không thể gia hạn thêm!";
                return false;
            }

            member = _memberRepo.GetMemberById(slip.MemberId);
            if (member == null)
            {
                errorMessage = "Không tìm thấy thông tin độc giả sở hữu phiếu mượn.";
                return false;
            }

            if (member.CardStatus != "ACTIVE")
            {
                errorMessage = $"Thẻ độc giả đang ở trạng thái '{member.CardStatus}' (Không hoạt động). Không thể gia hạn mượn sách!";
                return false;
            }

            if (member.DepositBalance < 0)
            {
                errorMessage = $"Tài khoản độc giả đang có khoản nợ phí/phạt chưa thanh toán ({member.DepositBalance:N0} VNĐ). Vui lòng thanh toán hết nợ trước khi gia hạn!";
                return false;
            }

            int renewalDays = _configService.GetRenewalDays();
            newDueDate = slip.DueDate.AddDays(renewalDays);

            if (member.ExpiryDate.Date < newDueDate.Date)
            {
                errorMessage = $"Thẻ độc giả sẽ hết hạn vào ngày {member.ExpiryDate:dd/MM/yyyy}, trước ngày hẹn trả mới ({newDueDate:dd/MM/yyyy}). Vui lòng gia hạn thẻ độc giả trước!";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Thực hiện gia hạn phiếu mượn sách (+số ngày gia hạn theo cấu hình, tăng số lần gia hạn)
        /// </summary>
        public virtual bool RenewBorrowSlip(
            int borrowSlipId, 
            out string errorMessage, 
            out DateTime newDueDate)
        {
            errorMessage = string.Empty;
            newDueDate = DateTime.Today;

            if (!ValidateBorrowRenewal(borrowSlipId, out errorMessage, out var slip, out newDueDate, out var member))
            {
                return false;
            }

            int newRenewalCount = slip.RenewalCount + 1;
            int renewalDays = _configService.GetRenewalDays();
            string renewalNote = $"[Gia hạn lần {newRenewalCount}: +{renewalDays} ngày đến {newDueDate:dd/MM/yyyy}]";
            string updatedNotes = string.IsNullOrWhiteSpace(slip.Notes) 
                ? renewalNote 
                : $"{slip.Notes}; {renewalNote}";

            if (updatedNotes.Length > 250)
            {
                updatedNotes = updatedNotes.Substring(0, 250);
            }

            bool success = _slipRepo.UpdateBorrowSlipRenewal(borrowSlipId, newDueDate, newRenewalCount, updatedNotes);
            if (!success)
            {
                errorMessage = "Có lỗi xảy ra trong quá trình cập nhật gia hạn vào CSDL.";
                return false;
            }

            return true;
        }
    }
}
