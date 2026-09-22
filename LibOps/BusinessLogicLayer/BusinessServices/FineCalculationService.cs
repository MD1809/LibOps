using System;
using System.Drawing;
using LibOps.DataAccessLayer.DataRepositories;
using LibOps.DataAccessLayer.DatabaseTransactions;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;
using LibOps.BusinessLogicLayer.BusinessServices.Notification;

namespace LibOps.BusinessLogicLayer.BusinessServices
{
    /// <summary>
    /// Dịch vụ nghiệp vụ Tiếp nhận trả sách, Thẩm định tình trạng, Tính phạt và Xử lý thu/khấu trừ tiền phạt
    /// </summary>
    public class FineCalculationService
    {
        private readonly ReturnDetailRepository _returnRepo;
        private readonly FineReceiptRepository _fineRepo;
        private readonly BorrowSlipRepository _slipRepo;
        private readonly MemberRepository _memberRepo;
        private readonly DepositRepository _depositRepo;
        private readonly BookCopyRepository _copyRepo;
        private readonly BookRepository _bookRepo;
        private readonly SystemSettingsService _configService;

        public FineCalculationService()
        {
            _returnRepo = new ReturnDetailRepository();
            _fineRepo = new FineReceiptRepository();
            _slipRepo = new BorrowSlipRepository();
            _memberRepo = new MemberRepository();
            _depositRepo = new DepositRepository();
            _copyRepo = new BookCopyRepository();
            _bookRepo = new BookRepository();
            _configService = new SystemSettingsService();
        }

        public FineCalculationService(
            ReturnDetailRepository returnRepo,
            FineReceiptRepository fineRepo,
            BorrowSlipRepository slipRepo,
            MemberRepository memberRepo,
            DepositRepository depositRepo,
            BookCopyRepository copyRepo,
            BookRepository bookRepo)
        {
            _returnRepo = returnRepo;
            _fineRepo = fineRepo;
            _slipRepo = slipRepo;
            _memberRepo = memberRepo;
            _depositRepo = depositRepo;
            _copyRepo = copyRepo;
            _bookRepo = bookRepo;
            _configService = new SystemSettingsService();
        }

        /// <summary>
        /// Tra cứu thông tin lượt mượn gốc khi quét mã vạch cuốn sách cần trả
        /// </summary>
        public virtual ReturnBookLookupDto LookupBookForReturn(string barcode, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(barcode))
            {
                errorMessage = "Vui lòng quét hoặc nhập mã vạch cuốn sách.";
                return null;
            }

            var record = _returnRepo.GetActiveBorrowRecordByBarcode(barcode.Trim());
            if (record == null)
            {
                // Kiểm tra xem cuốn sách này có tồn tại trong hệ thống không
                var copy = _copyRepo.GetCopyByBarcode(barcode.Trim());
                if (copy == null)
                {
                    errorMessage = $"Không tìm thấy cuốn sách nào có mã vạch '{barcode.Trim()}'.";
                }
                else if (copy.Status == "AVAILABLE")
                {
                    errorMessage = $"Cuốn sách '{barcode.Trim()}' hiện đang ở trạng thái SẴN SÀNG trong kho (chưa có ai mượn).";
                }
                else
                {
                    errorMessage = $"Cuốn sách '{barcode.Trim()}' không nằm trong bất kỳ lượt mượn nào chưa trả.";
                }
                return null;
            }

            // Áp dụng đơn giá phạt trễ hạn động từ cấu hình hệ thống
            decimal finePerDay = _configService.GetFinePerOverdueDay();
            record.OverdueFine = record.OverdueDays * finePerDay;

            return record;
        }

        /// <summary>
        /// Tính toán chi tiết các khoản phạt và tổng tiền thanh toán
        /// </summary>
        public virtual void CalculateReturnFines(
            ReturnBookLookupDto lookup,
            string conditionType,
            out decimal conditionFine,
            out decimal totalFine)
        {
            conditionFine = 0;
            if (lookup == null)
            {
                totalFine = 0;
                return;
            }

            decimal damagedRate = _configService.GetDamagedBookFineRate();
            decimal lostRate = _configService.GetLostBookFineRate();

            switch (conditionType)
            {
                case "DAMAGED":
                    conditionFine = Math.Round(lookup.BookPrice * damagedRate, 0);
                    break;
                case "LOST":
                    conditionFine = Math.Round(lookup.BookPrice * lostRate, 0);
                    break;
                default: // AVAILABLE
                    conditionFine = 0;
                    break;
            }

            totalFine = lookup.OverdueFine + conditionFine;
        }

        public virtual string GenerateNextFineReceiptCode()
        {
            DateTime today = DateTime.Today;
            int nextSeq = _fineRepo.GetNextReceiptSequenceForDay(today);
            return $"REC-FINE-{today:yyyyMMdd}-{nextSeq:D4}";
        }

        /// <summary>
        /// Thực thi Transaction tiếp nhận trả sách, cập nhật CSDL và xử lý thanh toán phạt
        /// </summary>
        public virtual bool ProcessReturnBook(
            ReturnBookLookupDto lookup,
            int receivedByUserId,
            string conditionType,
            string conditionNote,
            decimal overdueFine,
            decimal conditionFine,
            decimal totalFine,
            string paymentMethod,
            out string receiptCode,
            out string errorMessage)
        {
            receiptCode = string.Empty;
            errorMessage = string.Empty;

            if (lookup == null || lookup.BorrowSlipDetailId <= 0)
            {
                errorMessage = "Thông tin lượt mượn không hợp lệ.";
                return false;
            }

            var member = _memberRepo.GetMemberById(lookup.MemberId);
            if (member == null)
            {
                errorMessage = "Độc giả không tồn tại.";
                return false;
            }

            // Kiểm tra số dư nếu chọn khấu trừ tiền cọc (Quy tắc Phong tỏa Cọc An toàn khi Đang Mượn Sách)
            if (totalFine > 0 && paymentMethod == "DEPOSIT_DEDUCTION")
            {
                int currentBorrowing = _memberRepo.GetCurrentlyBorrowingCount(lookup.MemberId);
                int remainingBorrowCount = Math.Max(0, currentBorrowing - 1);
                decimal minDeposit = _configService.GetDefaultDeposit();
                decimal spendableDeposit = (remainingBorrowCount > 0) 
                    ? Math.Max(0, member.DepositBalance - minDeposit) 
                    : member.DepositBalance;

                if (totalFine > spendableDeposit)
                {
                    if (remainingBorrowCount > 0)
                    {
                        errorMessage = $"Độc giả vẫn còn {remainingBorrowCount} cuốn sách đang mượn, yêu cầu duy trì tối thiểu {minDeposit:N0} đ tiền cọc thế chân.\r\n" +
                                       $"Số dư cọc khả dụng để khấu trừ phạt: {spendableDeposit:N0} đ (Khoản phạt: {totalFine:N0} đ).\r\n" +
                                       $"Vui lòng chọn 'Thu Tiền Mặt Trực Tiếp', quét mã QR hoặc nạp thêm tiền!";
                    }
                    else
                    {
                        errorMessage = $"Số dư tiền cọc ({member.DepositBalance:N0} đ) không đủ để khấu trừ khoản phạt ({totalFine:N0} đ). Vui lòng chọn 'Thu Tiền Mặt Trực Tiếp' hoặc nạp thêm cọc!";
                    }
                    return false;
                }
            }

            if (totalFine > 0)
            {
                receiptCode = GenerateNextFineReceiptCode();
            }

            using (var scope = new TransactionExecutionScope())
            {
                try
                {
                    // 1. Ghi nhận ReturnSlipDetails
                    var returnDetail = new ReturnSlipDetailEntity
                    {
                        BorrowSlipDetailId = lookup.BorrowSlipDetailId,
                        ReceivedByUserId = receivedByUserId > 0 ? receivedByUserId : 1,
                        ActualReturnDate = DateTime.Now,
                        OverdueDays = lookup.OverdueDays,
                        FineAmount = totalFine,
                        ReturnConditionNote = conditionNote,
                        BookCopyStatusAfterReturn = conditionType
                    };
                    _returnRepo.InsertReturnSlipDetail(returnDetail, scope.Transaction);

                    // 2. Cập nhật trạng thái bản sao sách
                    _copyRepo.UpdateCopyStatus(lookup.CopyId, conditionType, scope.Transaction);

                    // 3. Đồng bộ lại số lượng tồn kho của đầu sách
                    _bookRepo.UpdateBookCopyCounts(lookup.BookId, scope.Transaction);

                    // 4. Nếu tất cả sách trong phiếu mượn đã được trả, đổi trạng thái phiếu mượn sang RETURNED
                    if (_returnRepo.AreAllDetailsReturned(lookup.BorrowSlipId, scope.Transaction))
                    {
                        _slipRepo.UpdateBorrowSlipStatus(lookup.BorrowSlipId, "RETURNED", scope.Transaction);
                    }
                    else
                    {
                        _slipRepo.UpdateBorrowSlipStatus(lookup.BorrowSlipId, "PARTIALLY_RETURNED", scope.Transaction);
                    }

                    // 5. Xử lý phạt tiền (nếu có)
                    if (totalFine > 0)
                    {
                        string fineReason = $"Phạt trả sách: {lookup.Title} (Mã vạch: {lookup.Barcode})";
                        if (lookup.OverdueDays > 0) fineReason += $" | Trễ {lookup.OverdueDays} ngày ({overdueFine:N0} đ)";
                        if (conditionType == "DAMAGED") fineReason += $" | Hư hỏng nhẹ ({conditionFine:N0} đ)";
                        if (conditionType == "LOST") fineReason += $" | Làm mất sách ({conditionFine:N0} đ)";

                        // Ghi nhận Biên lai thu tiền phạt
                        var receipt = new FineReceiptEntity
                        {
                            ReceiptCode = receiptCode,
                            MemberId = lookup.MemberId,
                            BorrowSlipId = lookup.BorrowSlipId,
                            CollectedByUserId = receivedByUserId > 0 ? receivedByUserId : 1,
                            TotalAmount = totalFine,
                            PaymentMethod = paymentMethod,
                            Reason = fineReason,
                            PaymentDate = DateTime.Now,
                            Notes = $"Xử lý trả sách ngày {DateTime.Now:dd/MM/yyyy HH:mm}"
                        };
                        _fineRepo.InsertFineReceipt(receipt, scope.Transaction);

                        // Nếu khấu trừ tiền cọc, cập nhật số dư độc giả và ghi DepositTransactions
                        if (paymentMethod == "DEPOSIT_DEDUCTION")
                        {
                            decimal newBalance = member.DepositBalance - totalFine;
                            _memberRepo.UpdateDepositBalance(lookup.MemberId, newBalance, scope.Transaction);

                            var depTrans = new DepositTransactionEntity
                            {
                                MemberId = lookup.MemberId,
                                HandledByUserId = receivedByUserId > 0 ? receivedByUserId : 1,
                                TransactionType = "FINE_DEDUCTION",
                                Amount = totalFine,
                                BalanceAfter = newBalance,
                                ReceiptCode = receiptCode,
                                TransactionDate = DateTime.Now,
                                Notes = $"Khấu trừ tiền cọc nộp phạt cho phiếu mượn {lookup.SlipCode}"
                            };
                            _depositRepo.InsertTransaction(depTrans, scope.Transaction);
                        }
                        // Nếu ghi nhận nợ vào tài khoản (độc giả chưa có tiền nộp ngay)
                        else if (paymentMethod == "DEBT")
                        {
                            decimal newDebt = member.TotalDebt + totalFine;
                            _memberRepo.UpdateTotalDebt(lookup.MemberId, newDebt, scope.Transaction);

                            var depTrans = new DepositTransactionEntity
                            {
                                MemberId = lookup.MemberId,
                                HandledByUserId = receivedByUserId > 0 ? receivedByUserId : 1,
                                TransactionType = "FINE_DEBT",
                                Amount = totalFine,
                                BalanceAfter = member.DepositBalance,
                                ReceiptCode = receiptCode,
                                TransactionDate = DateTime.Now,
                                Notes = $"Ghi nợ tiền phạt {totalFine:N0} đ cho phiếu mượn {lookup.SlipCode} (Tổng nợ: {newDebt:N0} đ - Tạm khóa mượn sách)"
                            };
                            _depositRepo.InsertTransaction(depTrans, scope.Transaction);
                        }
                    }

                    scope.Commit();

                    // 1. Gửi email xác nhận hoàn trả sách (Non-blocking)
                    try
                    {
                        int currentBorrowing = _memberRepo.GetCurrentlyBorrowingCount(lookup.MemberId);
                        var returnedItems = new System.Collections.Generic.List<ReturnedBookItemDto>
                        {
                            new ReturnedBookItemDto
                            {
                                Barcode = lookup.Barcode,
                                Title = lookup.Title,
                                ConditionType = conditionType,
                                ConditionNote = conditionNote,
                                OverdueDays = lookup.OverdueDays,
                                OverdueFine = overdueFine,
                                ConditionFine = conditionFine
                            }
                        };

                        string payMethodDisplay = null;
                        if (totalFine > 0)
                        {
                            payMethodDisplay = paymentMethod == "DEPOSIT_DEDUCTION" 
                                ? "Khấu trừ quỹ cọc" 
                                : (paymentMethod == "DEBT" ? "Ghi nhận nợ" : "Tiền mặt / Chuyển khoản trực tiếp");
                        }

                        EmailNotificationService.Instance.SendBookReturnReceiptEmailAsync(
                            member.Email,
                            member.FullName,
                            member.MemberCardCode,
                            DateTime.Now,
                            returnedItems,
                            currentBorrowing,
                            totalFine,
                            payMethodDisplay,
                            receiptCode
                        );
                    }
                    catch { }

                    // 2. Gửi email biên lai điện tử thu tiền phạt vi phạm nếu có phạt (Non-blocking)
                    if (totalFine > 0 && !string.IsNullOrWhiteSpace(receiptCode))
                    {
                        string payMethodDisplay = paymentMethod == "DEPOSIT_DEDUCTION" 
                            ? "Khấu trừ quỹ cọc" 
                            : (paymentMethod == "DEBT" ? "Ghi nhận nợ" : "Tiền mặt / Chuyển khoản trực tiếp");

                        string fineReason = $"Phạt trả sách: {lookup.Title} (Mã vạch: {lookup.Barcode})";
                        if (lookup.OverdueDays > 0) fineReason += $" | Trễ {lookup.OverdueDays} ngày ({overdueFine:N0} đ)";
                        if (conditionType == "DAMAGED") fineReason += $" | Hư hỏng ({conditionFine:N0} đ)";
                        if (conditionType == "LOST") fineReason += $" | Làm mất sách ({conditionFine:N0} đ)";

                        EmailNotificationService.Instance.SendFineOrFeeReceiptEmailAsync(
                            member.Email,
                            member.FullName,
                            member.MemberCardCode,
                            receiptCode,
                            "Phạt vi phạm & Bồi thường sách",
                            totalFine,
                            payMethodDisplay,
                            fineReason,
                            DateTime.Now
                        );
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    scope.Rollback();
                    errorMessage = "Lỗi khi xử lý trả sách: " + ex.Message;
                    return false;
                }
            }
        }

        /// <summary>
        /// Tra cứu toàn bộ các cuốn sách đang mượn chưa trả của một độc giả (theo Mã thẻ, SĐT hoặc Tên)
        /// </summary>
        public virtual System.Collections.Generic.List<ReturnBookLookupDto> GetActiveBorrowingBooksByMember(string keyword)
        {
            var list = _returnRepo.GetActiveBorrowRecordsByMember(keyword);
            decimal finePerDay = _configService.GetFinePerOverdueDay();
            foreach (var item in list)
            {
                item.OverdueFine = item.OverdueDays * finePerDay;
            }
            return list;
        }

        /// <summary>
        /// Tính toán tổng tiền phạt cho toàn bộ giỏ sách trả (Bulk Return Cart)
        /// </summary>
        public virtual void CalculateBulkReturnFines(
            System.Collections.Generic.IEnumerable<BulkReturnItemDto> cart,
            out decimal totalOverdueFine,
            out decimal totalConditionFine,
            out decimal totalPayable)
        {
            totalOverdueFine = 0;
            totalConditionFine = 0;
            totalPayable = 0;

            if (cart == null) return;

            decimal finePerDay = _configService.GetFinePerOverdueDay();
            decimal damagedRate = _configService.GetDamagedBookFineRate();
            decimal lostRate = _configService.GetLostBookFineRate();

            foreach (var item in cart)
            {
                item.OverdueFine = item.OverdueDays * finePerDay;
                if (item.ConditionType == "DAMAGED")
                {
                    item.ConditionFine = Math.Round(item.BookPrice * damagedRate, 0);
                }
                else if (item.ConditionType == "LOST")
                {
                    item.ConditionFine = Math.Round(item.BookPrice * lostRate, 0);
                }
                else
                {
                    item.ConditionFine = 0;
                }

                totalOverdueFine += item.OverdueFine;
                totalConditionFine += item.ConditionFine;
            }

            totalPayable = totalOverdueFine + totalConditionFine;
        }

        /// <summary>
        /// Xử lý trả nhiều cuốn sách cùng lúc trong 1 giao dịch an toàn (Bulk Return Transaction)
        /// </summary>
        public virtual bool ProcessBulkReturnBooks(
            System.Collections.Generic.IList<BulkReturnItemDto> cart,
            int receivedByUserId,
            string paymentMethod,
            out string receiptCode,
            out string errorMessage)
        {
            receiptCode = string.Empty;
            errorMessage = string.Empty;

            if (cart == null || cart.Count == 0)
            {
                errorMessage = "Danh sách sách trả rỗng. Vui lòng quét hoặc chọn ít nhất 1 cuốn sách!";
                return false;
            }

            CalculateBulkReturnFines(cart, out decimal totalOverdueFine, out decimal totalConditionFine, out decimal totalPayable);

            // Kiểm tra thông tin độc giả (lấy theo cuốn sách đầu tiên)
            int memberId = cart[0].MemberId;
            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Không tìm thấy hồ sơ độc giả trong hệ thống!";
                return false;
            }

            // Kiểm tra số dư cọc nếu chọn khấu trừ tiền cọc (Quy tắc Phong tỏa Cọc An toàn khi Đang Mượn Sách)
            if (totalPayable > 0 && paymentMethod == "DEPOSIT_DEDUCTION")
            {
                int currentBorrowing = _memberRepo.GetCurrentlyBorrowingCount(memberId);
                int returnedCount = cart.Count;
                int remainingBorrowCount = Math.Max(0, currentBorrowing - returnedCount);
                decimal minDeposit = _configService.GetDefaultDeposit();
                decimal spendableDeposit = (remainingBorrowCount > 0) 
                    ? Math.Max(0, member.DepositBalance - minDeposit) 
                    : member.DepositBalance;

                if (totalPayable > spendableDeposit)
                {
                    if (remainingBorrowCount > 0)
                    {
                        errorMessage = $"Độc giả vẫn còn {remainingBorrowCount} cuốn sách đang mượn, yêu cầu duy trì tối thiểu {minDeposit:N0} đ tiền cọc thế chân.\r\n" +
                                       $"Số dư cọc khả dụng để khấu trừ phạt: {spendableDeposit:N0} đ (Tổng phạt: {totalPayable:N0} đ).\r\n" +
                                       $"Vui lòng chọn 'Thu Tiền Mặt Trực Tiếp', quét mã QR hoặc nạp thêm tiền!";
                    }
                    else
                    {
                        errorMessage = $"Số dư tiền cọc của độc giả ({member.DepositBalance:N0} đ) không đủ để khấu trừ tổng khoản phạt ({totalPayable:N0} đ). Vui lòng chọn 'Thu Tiền Mặt Trực Tiếp' hoặc 'Ghi Nhận Nợ'!";
                    }
                    return false;
                }
            }

            if (totalPayable > 0)
            {
                receiptCode = GenerateNextFineReceiptCode();
            }

            using (var scope = new TransactionExecutionScope())
            {
                try
                {
                    var affectedSlips = new System.Collections.Generic.HashSet<int>();

                    // 1. Duyệt và ghi nhận trả cho từng cuốn sách
                    foreach (var item in cart)
                    {
                        var returnDetail = new ReturnSlipDetailEntity
                        {
                            BorrowSlipDetailId = item.BorrowSlipDetailId,
                            ReceivedByUserId = receivedByUserId > 0 ? receivedByUserId : 1,
                            ActualReturnDate = DateTime.Now,
                            OverdueDays = item.OverdueDays,
                            FineAmount = item.TotalFine,
                            ReturnConditionNote = item.ConditionNote,
                            BookCopyStatusAfterReturn = item.ConditionType
                        };
                        _returnRepo.InsertReturnSlipDetail(returnDetail, scope.Transaction);

                        // Cập nhật trạng thái bản sao sách
                        _copyRepo.UpdateCopyStatus(item.CopyId, item.ConditionType, scope.Transaction);

                        // Đồng bộ số lượng tồn kho đầu sách
                        _bookRepo.UpdateBookCopyCounts(item.BookId, scope.Transaction);

                        affectedSlips.Add(item.BorrowSlipId);
                    }

                    // 2. Cập nhật trạng thái cho toàn bộ các phiếu mượn liên quan
                    foreach (int slipId in affectedSlips)
                    {
                        if (_returnRepo.AreAllDetailsReturned(slipId, scope.Transaction))
                        {
                            _slipRepo.UpdateBorrowSlipStatus(slipId, "RETURNED", scope.Transaction);
                        }
                        else
                        {
                            _slipRepo.UpdateBorrowSlipStatus(slipId, "PARTIALLY_RETURNED", scope.Transaction);
                        }
                    }

                    // 3. Xử lý tài chính và ghi nhận Biên lai thu phạt tổng hợp (nếu có phạt)
                    if (totalPayable > 0)
                    {
                        var reasonBuilder = new System.Text.StringBuilder();
                        reasonBuilder.Append($"Phạt trả {cart.Count} cuốn sách (Mã: {receiptCode})");
                        if (totalOverdueFine > 0) reasonBuilder.Append($" | Phạt trễ hạn: {totalOverdueFine:N0} đ");
                        if (totalConditionFine > 0) reasonBuilder.Append($" | Phạt hỏng/mất: {totalConditionFine:N0} đ");

                        int primarySlipId = cart[0].BorrowSlipId;

                        var receipt = new FineReceiptEntity
                        {
                            ReceiptCode = receiptCode,
                            MemberId = memberId,
                            BorrowSlipId = primarySlipId,
                            CollectedByUserId = receivedByUserId > 0 ? receivedByUserId : 1,
                            TotalAmount = totalPayable,
                            PaymentMethod = paymentMethod,
                            Reason = reasonBuilder.ToString(),
                            PaymentDate = DateTime.Now,
                            Notes = $"Xử lý trả {cart.Count} cuốn sách ngày {DateTime.Now:dd/MM/yyyy HH:mm}"
                        };
                        _fineRepo.InsertFineReceipt(receipt, scope.Transaction);

                        // Khấu trừ tiền cọc
                        if (paymentMethod == "DEPOSIT_DEDUCTION")
                        {
                            decimal newBalance = member.DepositBalance - totalPayable;
                            _memberRepo.UpdateDepositBalance(memberId, newBalance, scope.Transaction);

                            var depTrans = new DepositTransactionEntity
                            {
                                MemberId = memberId,
                                HandledByUserId = receivedByUserId > 0 ? receivedByUserId : 1,
                                TransactionType = "FINE_DEDUCTION",
                                Amount = totalPayable,
                                BalanceAfter = newBalance,
                                ReceiptCode = receiptCode,
                                TransactionDate = DateTime.Now,
                                Notes = $"Khấu trừ tiền cọc nộp phạt cho giao dịch trả {cart.Count} cuốn sách ({receiptCode})"
                            };
                            _depositRepo.InsertTransaction(depTrans, scope.Transaction);
                        }
                        // Ghi nhận nợ vào tài khoản
                        else if (paymentMethod == "DEBT")
                        {
                            decimal newDebt = member.TotalDebt + totalPayable;
                            _memberRepo.UpdateTotalDebt(memberId, newDebt, scope.Transaction);

                            var depTrans = new DepositTransactionEntity
                            {
                                MemberId = memberId,
                                HandledByUserId = receivedByUserId > 0 ? receivedByUserId : 1,
                                TransactionType = "FINE_DEBT",
                                Amount = totalPayable,
                                BalanceAfter = member.DepositBalance,
                                ReceiptCode = receiptCode,
                                TransactionDate = DateTime.Now,
                                Notes = $"Ghi nợ tiền phạt {totalPayable:N0} đ cho giao dịch trả {cart.Count} cuốn sách ({receiptCode}) - Tổng nợ: {newDebt:N0} đ"
                            };
                            _depositRepo.InsertTransaction(depTrans, scope.Transaction);
                        }
                    }

                    scope.Commit();

                    // 1. Gửi email xác nhận hoàn trả sách hàng loạt (Non-blocking)
                    try
                    {
                        int currentBorrowing = _memberRepo.GetCurrentlyBorrowingCount(memberId);
                        var returnedItems = new System.Collections.Generic.List<ReturnedBookItemDto>();
                        foreach (var item in cart)
                        {
                            returnedItems.Add(new ReturnedBookItemDto
                            {
                                Barcode = item.Barcode,
                                Title = item.Title,
                                ConditionType = item.ConditionType,
                                ConditionNote = item.ConditionNote,
                                OverdueDays = item.OverdueDays,
                                OverdueFine = item.OverdueFine,
                                ConditionFine = item.ConditionFine
                            });
                        }

                        string payMethodDisplay = null;
                        if (totalPayable > 0)
                        {
                            payMethodDisplay = paymentMethod == "DEPOSIT_DEDUCTION" 
                                ? "Khấu trừ quỹ cọc" 
                                : (paymentMethod == "DEBT" ? "Ghi nhận nợ" : "Tiền mặt / Chuyển khoản trực tiếp");
                        }

                        EmailNotificationService.Instance.SendBookReturnReceiptEmailAsync(
                            member.Email,
                            member.FullName,
                            member.MemberCardCode,
                            DateTime.Now,
                            returnedItems,
                            currentBorrowing,
                            totalPayable,
                            payMethodDisplay,
                            receiptCode
                        );
                    }
                    catch { }

                    // 2. Gửi email biên lai điện tử thu tiền phạt vi phạm (Non-blocking)
                    if (totalPayable > 0 && !string.IsNullOrWhiteSpace(receiptCode))
                    {
                        string payMethodDisplay = paymentMethod == "DEPOSIT_DEDUCTION" 
                            ? "Khấu trừ quỹ cọc" 
                            : (paymentMethod == "DEBT" ? "Ghi nhận nợ" : "Tiền mặt / Chuyển khoản trực tiếp");

                        var bulkReasonBuilder = new System.Text.StringBuilder();
                        bulkReasonBuilder.Append($"Phạt trả {cart.Count} cuốn sách");
                        if (totalOverdueFine > 0) bulkReasonBuilder.Append($" | Phạt trễ hạn: {totalOverdueFine:N0} đ");
                        if (totalConditionFine > 0) bulkReasonBuilder.Append($" | Phạt hỏng/mất: {totalConditionFine:N0} đ");

                        EmailNotificationService.Instance.SendFineOrFeeReceiptEmailAsync(
                            member.Email,
                            member.FullName,
                            member.MemberCardCode,
                            receiptCode,
                            "Phạt vi phạm & Bồi thường sách",
                            totalPayable,
                            payMethodDisplay,
                            bulkReasonBuilder.ToString(),
                            DateTime.Now
                        );
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    scope.Rollback();
                    errorMessage = "Lỗi khi xử lý giao dịch trả sách hàng loạt: " + ex.Message;
                    return false;
                }
            }
        }

        /// <summary>
        /// Tạo mẫu in Biên Lai Thu Tiền Phạt & Đền Bù cho 1 cuốn sách dạng Bitmap nhiệt
        /// </summary>
        public virtual Bitmap GenerateFineReceiptBitmap(
            string receiptCode,
            string memberCardCode,
            string memberName,
            string bookTitle,
            string barcode,
            int overdueDays,
            decimal overdueFine,
            string conditionDesc,
            decimal conditionFine,
            decimal totalFine,
            string paymentMethodDesc,
            string librarianName)
        {
            var cart = new System.Collections.Generic.List<BulkReturnItemDto>
            {
                new BulkReturnItemDto
                {
                    Title = bookTitle ?? "--",
                    Barcode = barcode ?? "--",
                    OverdueDays = overdueDays,
                    OverdueFine = overdueFine,
                    ConditionType = conditionFine > 0 ? "DAMAGED" : "AVAILABLE",
                    ConditionFine = conditionFine,
                    ConditionNote = conditionDesc
                }
            };

            return GenerateCombinedFineReceiptBitmap(
                receiptCode,
                memberCardCode,
                memberName,
                cart,
                overdueFine,
                conditionFine,
                totalFine,
                paymentMethodDesc,
                librarianName
            );
        }

        /// <summary>
        /// Tạo mẫu in Biên Lai Thu Tiền Phạt & Đền Bù tổng hợp (nhiều cuốn sách) dạng Bitmap nhiệt
        /// </summary>
        public virtual Bitmap GenerateCombinedFineReceiptBitmap(
            string receiptCode,
            string memberCardCode,
            string memberName,
            System.Collections.Generic.IList<BulkReturnItemDto> cart,
            decimal totalOverdueFine,
            decimal totalConditionFine,
            decimal totalPayable,
            string paymentMethodDesc,
            string librarianName)
        {
            int width = 320;
            int itemRowHeight = 32;
            int baseHeight = 320;
            int height = baseHeight + (cart != null ? cart.Count * itemRowHeight : 0);
            var bmp = new Bitmap(width, height);

            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);

                using (var fontHeader = new Font("Segoe UI", 10F, FontStyle.Bold))
                using (var fontSub = new Font("Segoe UI", 8F))
                using (var fontBold = new Font("Segoe UI", 8.5F, FontStyle.Bold))
                using (var fontRegular = new Font("Segoe UI", 8F))
                using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (var redBrush = new SolidBrush(Color.FromArgb(220, 38, 38)))
                {
                    g.DrawString("HỆ THỐNG THƯ VIỆN LIBOPS", fontHeader, brush, 80, 10);
                    g.DrawString("BIÊN LAI THU TIỀN PHẠT & BỒI THƯỜNG", fontBold, brush, 30, 30);
                    g.DrawString($"Số biên lai: {receiptCode}", fontSub, brush, 15, 52);
                    g.DrawString($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", fontSub, brush, 15, 68);
                    g.DrawString($"Độc giả: {memberCardCode} - {memberName}", fontBold, brush, 15, 84);

                    using (var pen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                    {
                        g.DrawLine(pen, 15, 102, width - 15, 102);
                    }

                    int currentY = 108;
                    g.DrawString("DANH SÁCH SÁCH TRẢ & MỨC PHẠT:", fontBold, brush, 15, currentY);
                    currentY += 18;

                    if (cart != null)
                    {
                        for (int i = 0; i < cart.Count; i++)
                        {
                            var item = cart[i];
                            string condText = item.ConditionType == "AVAILABLE" ? "Nguyên vẹn" : (item.ConditionType == "DAMAGED" ? "Hỏng nhẹ" : "Làm mất");
                            string titleDisplay = item.Title.Length > 24 ? item.Title.Substring(0, 22) + "..." : item.Title;

                            g.DrawString($"{i + 1}. {titleDisplay} ({item.Barcode})", fontRegular, brush, 15, currentY);
                            g.DrawString($"[{condText}] Phạt: {item.TotalFine:N0} đ", fontBold, item.TotalFine > 0 ? redBrush : brush, 180, currentY);
                            currentY += 24;
                        }
                    }

                    using (var pen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                    {
                        g.DrawLine(pen, 15, currentY, width - 15, currentY);
                    }
                    currentY += 8;

                    g.DrawString($"Tổng phạt quá hạn: {totalOverdueFine:N0} VNĐ", fontRegular, brush, 15, currentY);
                    currentY += 18;
                    g.DrawString($"Tổng phạt hỏng/mất: {totalConditionFine:N0} VNĐ", fontRegular, brush, 15, currentY);
                    currentY += 20;

                    g.DrawString("TỔNG THANH TOÁN:", fontBold, brush, 15, currentY);
                    g.DrawString($"{totalPayable:N0} VNĐ", new Font("Segoe UI", 11F, FontStyle.Bold), redBrush, 180, currentY - 2);
                    currentY += 26;

                    g.DrawString($"Hình thức: {paymentMethodDesc}", fontRegular, brush, 15, currentY);
                    currentY += 18;
                    g.DrawString($"Thủ thư thu: {librarianName}", fontSub, brush, 15, currentY);
                    currentY += 25;

                    g.DrawString("Người nộp tiền", fontBold, brush, 30, currentY);
                    g.DrawString("Người thu tiền", fontBold, brush, 210, currentY);
                    currentY += 18;
                    g.DrawString("(Ký, họ tên)", fontSub, brush, 35, currentY);
                    g.DrawString("(Ký, họ tên)", fontSub, brush, 215, currentY);
                }

                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1))
                {
                    g.DrawRectangle(pen, 0, 0, width - 1, height - 1);
                }
            }

            return bmp;
        }
    }
}

