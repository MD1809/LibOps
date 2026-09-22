using System;
using System.Collections.Generic;
using System.Drawing;
using LibOps.CommonUtilities.Security;
using LibOps.DataAccessLayer.DataRepositories;
using LibOps.DataAccessLayer.DatabaseConnection;
using LibOps.DataAccessLayer.DatabaseTransactions;
using LibOps.DataModels.DataTransferObjects;
using LibOps.DataModels.Entities;
using LibOps.BusinessLogicLayer.BusinessServices.Notification;

namespace LibOps.BusinessLogicLayer.BusinessServices
{
    /// <summary>
    /// Dịch vụ nghiệp vụ Quản lý Độc Giả, Thẻ Thư Viện và Tiền Cọc Thế Chân
    /// </summary>
    public class MemberService
    {
        public const string DEFAULT_READER_PASSWORD = "LibOps@123";

        private readonly MemberRepository _memberRepo;
        private readonly DepositRepository _depositRepo;
        private readonly FineReceiptRepository _fineRepo;
        private readonly ServiceFeeRepository _serviceFeeRepo;
        private readonly BookService _bookService;
        private readonly UserRepository _userAccountRepo;

        public MemberService()
        {
            _memberRepo = new MemberRepository();
            _depositRepo = new DepositRepository();
            _fineRepo = new FineReceiptRepository();
            _serviceFeeRepo = new ServiceFeeRepository();
            _bookService = new BookService();
            _userAccountRepo = new UserRepository();
        }

        public MemberService(MemberRepository memberRepo, DepositRepository depositRepo)
        {
            _memberRepo = memberRepo;
            _depositRepo = depositRepo;
            _fineRepo = new FineReceiptRepository();
            _serviceFeeRepo = new ServiceFeeRepository();
            _bookService = new BookService();
            _userAccountRepo = new UserRepository();
        }

        public virtual string GenerateNextMemberCardCode()
        {
            int currentYear = DateTime.Now.Year;
            int nextSeq = _memberRepo.GetNextCardSequenceForYear(currentYear);
            string code = $"DG{currentYear}{nextSeq:D5}";

            while (_memberRepo.IsCardCodeExists(code))
            {
                nextSeq++;
                code = $"DG{currentYear}{nextSeq:D5}";
            }

            return code;
        }

        public virtual List<MemberGridDisplayDto> GetAllMembers(string keyword = null, string cardStatus = null)
        {
            return _memberRepo.GetAllMembers(keyword, cardStatus);
        }

        public virtual MemberEntity GetMemberById(int memberId)
        {
            return _memberRepo.GetMemberById(memberId);
        }

        public virtual MemberEntity GetMemberByCardCode(string cardCode)
        {
            return _memberRepo.GetMemberByCardCode(cardCode);
        }

        public virtual int GetCurrentlyBorrowingCount(int memberId)
        {
            return _memberRepo.GetCurrentlyBorrowingCount(memberId);
        }

        public virtual bool RegisterNewMember(MemberEntity member, decimal cardIssuanceFee, decimal initialDeposit, int handledByUserId, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (member == null)
            {
                errorMessage = "Dữ liệu độc giả không hợp lệ.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(member.FullName))
            {
                errorMessage = "Vui lòng nhập họ và tên độc giả.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(member.PhoneNumber))
            {
                errorMessage = "Vui lòng nhập số điện thoại liên hệ.";
                return false;
            }

            if (!LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidPhoneNumber(member.PhoneNumber))
            {
                errorMessage = "Số điện thoại không đúng định dạng (10 chữ số).";
                return false;
            }

            if (string.IsNullOrWhiteSpace(member.IdentityCardNumber))
            {
                errorMessage = "Vui lòng nhập số CCCD/CMND độc giả.";
                return false;
            }

            if (!LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidIdentityCard(member.IdentityCardNumber))
            {
                errorMessage = "Số CCCD/CMND không hợp lệ (9 hoặc 12 chữ số).";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(member.Email) && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidEmail(member.Email))
            {
                errorMessage = "Địa chỉ Email không đúng định dạng.";
                return false;
            }

            if (member.DateOfBirth.HasValue && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidDateOfBirth(member.DateOfBirth, out string dobErr))
            {
                errorMessage = dobErr;
                return false;
            }

            member.PhoneNumber = LibOps.CommonUtilities.Validation.InputValidationUtility.NormalizePhoneNumber(member.PhoneNumber);
            member.IdentityCardNumber = LibOps.CommonUtilities.Validation.InputValidationUtility.NormalizeIdentityCard(member.IdentityCardNumber);

            var existingMember = _memberRepo.GetMemberByIdentityCard(member.IdentityCardNumber);
            if (existingMember != null)
            {
                if (existingMember.CardStatus == "CLOSED")
                {
                    errorMessage = $"Số CCCD/CMND này thuộc về hồ sơ thẻ '{existingMember.MemberCardCode}' ({existingMember.FullName}) đã hủy. Vui lòng chọn độc giả này trên danh sách và sử dụng chức năng 'Tái Cấp Thẻ' để kích hoạt lại.";
                }
                else
                {
                    errorMessage = $"Số CCCD/CMND này đã được đăng ký cho độc giả '{existingMember.FullName}' (Mã thẻ: {existingMember.MemberCardCode}) trong hệ thống.";
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(member.MemberCardCode))
            {
                member.MemberCardCode = GenerateNextMemberCardCode();
            }

            if (_memberRepo.IsCardCodeExists(member.MemberCardCode))
            {
                errorMessage = "Mã thẻ độc giả này đã tồn tại.";
                return false;
            }

            if (cardIssuanceFee < 0)
            {
                errorMessage = "Phí phát hành thẻ không thể nhỏ hơn 0.";
                return false;
            }

            if (initialDeposit < 0)
            {
                errorMessage = "Số tiền cọc ban đầu không thể nhỏ hơn 0.";
                return false;
            }

            member.DepositBalance = initialDeposit;
            member.TotalDebt = 0;
            member.IssueDate = DateTime.Now;
            member.ExpiryDate = DateTime.Now.AddYears(1); // Thời hạn thẻ 1 năm
            member.CardStatus = "ACTIVE";

            using (var scope = new TransactionExecutionScope())
            {
                try
                {
                    int newMemberId = _memberRepo.InsertMember(member, scope.Transaction);
                    if (newMemberId <= 0)
                    {
                        scope.Rollback();
                        errorMessage = "Lỗi hệ thống: Không thể thêm mới hồ sơ độc giả.";
                        return false;
                    }
                    member.MemberId = newMemberId;

                    // 1. Ghi nhận thu phí phát hành thẻ (Doanh thu không hoàn lại vào ServiceFeeReceipts)
                    if (cardIssuanceFee > 0)
                    {
                        string feeReceiptCode = $"FEE-{DateTime.Now:yyyyMMddHHmmss}-{newMemberId:D5}";
                        var feeReceipt = new ServiceFeeReceiptEntity
                        {
                            MemberId = newMemberId,
                            ReceiptCode = feeReceiptCode,
                            FeeType = "CARD_ISSUANCE",
                            Amount = cardIssuanceFee,
                            PaymentMethod = "CASH",
                            PaymentDate = DateTime.Now,
                            CollectedByUserId = handledByUserId > 0 ? handledByUserId : 1,
                            Notes = "Thu phí phát hành thẻ độc giả mới (Phí dịch vụ không hoàn lại)"
                        };

                        _serviceFeeRepo.InsertReceipt(feeReceipt, scope.Transaction);
                    }

                    // 2. Nếu có thu tiền cọc, ghi nhận Transaction thu cọc ban đầu
                    if (initialDeposit > 0)
                    {
                        string receiptCode = $"REC-{DateTime.Now:yyyyMMddHHmmss}-{newMemberId:D5}";
                        var trans = new DepositTransactionEntity
                        {
                            MemberId = newMemberId,
                            HandledByUserId = handledByUserId > 0 ? handledByUserId : 1,
                            TransactionType = "INITIAL_DEPOSIT",
                            Amount = initialDeposit,
                            BalanceAfter = initialDeposit,
                            ReceiptCode = receiptCode,
                            TransactionDate = DateTime.Now,
                            Notes = "Thu tiền cọc thế chân khi đăng ký mở thẻ mới"
                        };

                        _depositRepo.InsertTransaction(trans, scope.Transaction);
                    }

                    // 3. Tự động tạo tài khoản đăng nhập Cổng Độc Giả (Reader Portal)
                    // Tài khoản: Mã thẻ (member.MemberCardCode) | Mật khẩu mặc định: DEFAULT_READER_PASSWORD (LibOps@123)
                    int readerRoleId = _userAccountRepo.GetRoleIdByName("READER", scope.Transaction);
                    string salt = PasswordHashUtility.GenerateSalt();
                    string hash = PasswordHashUtility.ComputeSha256Hash(DEFAULT_READER_PASSWORD, salt);

                    var readerUser = new UserAccountEntity
                    {
                        Username = member.MemberCardCode,
                        PasswordHash = hash,
                        PasswordSalt = salt,
                        FullName = member.FullName,
                        Email = member.Email,
                        PhoneNumber = member.PhoneNumber,
                        RoleId = readerRoleId,
                        MemberId = newMemberId,
                        IsActive = true
                    };

                    int newUserId = _userAccountRepo.InsertUser(readerUser, scope.Transaction);
                    if (newUserId <= 0)
                    {
                        scope.Rollback();
                        errorMessage = "Lỗi hệ thống: Không thể khởi tạo tài khoản đăng nhập cho độc giả.";
                        return false;
                    }

                    scope.Commit();

                    // Gửi email chào mừng & thông tin tài khoản tự động (Non-blocking)
                    EmailNotificationService.Instance.SendWelcomeNewMemberEmailAsync(
                        member.Email,
                        member.FullName,
                        member.MemberCardCode,
                        member.ExpiryDate,
                        initialDeposit,
                        DEFAULT_READER_PASSWORD
                    );

                    return true;
                }
                catch (Exception ex)
                {
                    scope.Rollback();
                    errorMessage = "Lỗi khi lưu dữ liệu độc giả: " + ex.Message;
                    return false;
                }
            }
        }

        public virtual bool RegisterNewMember(MemberEntity member, decimal initialDeposit, int handledByUserId, out string errorMessage)
        {
            return RegisterNewMember(member, 50000m, initialDeposit, handledByUserId, out errorMessage);
        }

        /// <summary>
        /// Tái cấp thẻ / Tái kích hoạt thẻ cho độc giả đã từng hủy thẻ (CLOSED) khi quay lại sử dụng
        /// </summary>
        public virtual bool ReactivateMemberCard(int memberId, decimal cardIssuanceFee, decimal initialDeposit, int handledByUserId, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (memberId <= 0)
            {
                errorMessage = "Mã độc giả không hợp lệ.";
                return false;
            }

            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Không tìm thấy thông tin hồ sơ độc giả.";
                return false;
            }

            if (member.CardStatus != "CLOSED")
            {
                errorMessage = $"Thẻ độc giả đang ở trạng thái '{member.CardStatus}', không cần tái kích hoạt.";
                return false;
            }

            if (member.TotalDebt > 0)
            {
                errorMessage = $"Độc giả còn khoản nợ phạt chưa thanh toán ({member.TotalDebt:N0} VNĐ). Vui lòng xử lý công nợ trước khi tái cấp thẻ!";
                return false;
            }

            if (cardIssuanceFee < 0)
            {
                errorMessage = "Phí phát hành lại thẻ không thể nhỏ hơn 0.";
                return false;
            }

            if (initialDeposit < 0)
            {
                errorMessage = "Số tiền cọc ban đầu không thể nhỏ hơn 0.";
                return false;
            }

            DateTime newExpiryDate = DateTime.Now.AddYears(1);

            using (var scope = new TransactionExecutionScope())
            {
                try
                {
                    // 1. Cập nhật trạng thái thẻ sang ACTIVE, gán cọc mới và gia hạn 1 năm
                    bool updateOk = _memberRepo.ReactivateMember(memberId, initialDeposit, newExpiryDate, scope.Transaction);
                    if (!updateOk)
                    {
                        scope.Rollback();
                        errorMessage = "Lỗi hệ thống: Không thể cập nhật trạng thái tái kích hoạt thẻ.";
                        return false;
                    }

                    // 2. Ghi nhận phí phát hành lại thẻ nếu có
                    if (cardIssuanceFee > 0)
                    {
                        string feeReceiptCode = $"FEE-{DateTime.Now:yyyyMMddHHmmss}-{memberId:D5}";
                        var feeReceipt = new ServiceFeeReceiptEntity
                        {
                            MemberId = memberId,
                            ReceiptCode = feeReceiptCode,
                            FeeType = "CARD_ISSUANCE",
                            Amount = cardIssuanceFee,
                            PaymentMethod = "CASH",
                            PaymentDate = DateTime.Now,
                            CollectedByUserId = handledByUserId > 0 ? handledByUserId : 1,
                            Notes = "Thu phí phát hành lại thẻ độc giả (Tái cấp thẻ sau khi hủy)"
                        };

                        _serviceFeeRepo.InsertReceipt(feeReceipt, scope.Transaction);
                    }

                    // 3. Ghi nhận nộp tiền cọc ban đầu nếu có
                    if (initialDeposit > 0)
                    {
                        string receiptCode = $"REC-{DateTime.Now:yyyyMMddHHmmss}-{memberId:D5}";
                        var trans = new DepositTransactionEntity
                        {
                            MemberId = memberId,
                            HandledByUserId = handledByUserId > 0 ? handledByUserId : 1,
                            TransactionType = "INITIAL_DEPOSIT",
                            Amount = initialDeposit,
                            BalanceAfter = initialDeposit,
                            ReceiptCode = receiptCode,
                            TransactionDate = DateTime.Now,
                            Notes = "Nộp tiền cọc thế chân khi tái cấp kích hoạt lại thẻ"
                        };

                        _depositRepo.InsertTransaction(trans, scope.Transaction);
                    }

                    // 4. Kích hoạt lại tài khoản đăng nhập Reader trong UserAccounts
                    _userAccountRepo.UpdateIsActiveByMemberId(memberId, true, scope.Transaction);

                    scope.Commit();

                    // Gửi email thông báo tái cấp thẻ thành công (Non-blocking)
                    EmailNotificationService.Instance.SendWelcomeNewMemberEmailAsync(
                        member.Email,
                        member.FullName,
                        member.MemberCardCode,
                        newExpiryDate,
                        initialDeposit,
                        DEFAULT_READER_PASSWORD
                    );

                    return true;
                }
                catch (Exception ex)
                {
                    scope.Rollback();
                    errorMessage = "Lỗi ngoại lệ khi tái cấp thẻ: " + ex.Message;
                    return false;
                }
            }
        }

        public virtual bool UpdateMember(MemberEntity member, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                if (member == null || member.MemberId <= 0)
                {
                    errorMessage = "Hồ sơ độc giả không hợp lệ.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(member.FullName))
                {
                    errorMessage = "Vui lòng nhập họ và tên độc giả.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(member.PhoneNumber))
                {
                    errorMessage = "Vui lòng nhập số điện thoại liên hệ.";
                    return false;
                }

                if (!LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidPhoneNumber(member.PhoneNumber))
                {
                    errorMessage = "Số điện thoại không đúng định dạng (10 chữ số).";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(member.IdentityCardNumber) && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidIdentityCard(member.IdentityCardNumber))
                {
                    errorMessage = "Số CCCD/CMND không hợp lệ (9 hoặc 12 chữ số).";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(member.Email) && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidEmail(member.Email))
                {
                    errorMessage = "Địa chỉ Email không đúng định dạng.";
                    return false;
                }

                if (member.DateOfBirth.HasValue && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidDateOfBirth(member.DateOfBirth, out string dobErr))
                {
                    errorMessage = dobErr;
                    return false;
                }

                member.PhoneNumber = LibOps.CommonUtilities.Validation.InputValidationUtility.NormalizePhoneNumber(member.PhoneNumber);
                if (!string.IsNullOrWhiteSpace(member.IdentityCardNumber))
                {
                    member.IdentityCardNumber = LibOps.CommonUtilities.Validation.InputValidationUtility.NormalizeIdentityCard(member.IdentityCardNumber);
                }

                if (!string.IsNullOrWhiteSpace(member.IdentityCardNumber) && _memberRepo.IsIdentityCardExists(member.IdentityCardNumber, member.MemberId))
                {
                    errorMessage = "Số CCCD/CMND này đã thuộc về độc giả khác.";
                    return false;
                }

                bool ok = _memberRepo.UpdateMember(member);
                if (!ok)
                {
                    errorMessage = "Lỗi hệ thống: Không thể cập nhật thông tin độc giả.";
                    return false;
                }

                // Đồng bộ thông tin Họ tên, Email, SĐT sang tài khoản đăng nhập (nếu có)
                _userAccountRepo.UpdateUserContactByMemberId(member.MemberId, member.FullName, member.Email, member.PhoneNumber);

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "Lỗi ngoại lệ khi cập nhật thông tin độc giả: " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Cấp lại mật khẩu mặc định (LibOps@123) hoặc mật khẩu mới cho độc giả
        /// </summary>
        public virtual bool ResetMemberPassword(int memberId, string rawPassword, out string errorMessage)
        {
            errorMessage = string.Empty;
            string pass = string.IsNullOrWhiteSpace(rawPassword) ? DEFAULT_READER_PASSWORD : rawPassword.Trim();
            string salt = PasswordHashUtility.GenerateSalt();
            string hash = PasswordHashUtility.ComputeSha256Hash(pass, salt);

            bool ok = _userAccountRepo.ResetPasswordByMemberId(memberId, hash, salt);
            if (!ok)
            {
                errorMessage = "Không tìm thấy tài khoản đăng nhập liên kết với độc giả này hoặc lỗi cập nhật CSDL.";
                return false;
            }
            return true;
        }

        public virtual bool TopUpDeposit(int memberId, decimal amount, int handledByUserId, string notes, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (memberId <= 0)
            {
                errorMessage = "Độc giả không hợp lệ.";
                return false;
            }

            if (amount <= 0)
            {
                errorMessage = "Số tiền nạp thêm cọc phải lớn hơn 0.";
                return false;
            }

            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Hồ sơ độc giả không tồn tại.";
                return false;
            }

            if (member.CardStatus == "CLOSED")
            {
                errorMessage = "Không thể nạp cọc cho thẻ đã bị hủy.";
                return false;
            }

            // Thuật toán nạp tiền thác nước (Waterfall Top-Up):
            decimal currentDebt = member.TotalDebt;
            decimal newDebt = currentDebt;
            decimal newBalance = member.DepositBalance;
            decimal debtPaid = 0;
            decimal depositAdded = 0;

            if (currentDebt > 0)
            {
                if (amount <= currentDebt)
                {
                    debtPaid = amount;
                    newDebt = currentDebt - amount;
                }
                else
                {
                    debtPaid = currentDebt;
                    depositAdded = amount - currentDebt;
                    newDebt = 0;
                    newBalance = member.DepositBalance + depositAdded;
                }
            }
            else
            {
                depositAdded = amount;
                newBalance = member.DepositBalance + amount;
            }

            string receiptCode = $"TOP-{DateTime.Now:yyyyMMddHHmmss}";
            string noteDetail;
            if (debtPaid > 0 && depositAdded > 0)
            {
                noteDetail = $"Nạp {amount:N0} đ (Khấu trừ hết nợ {debtPaid:N0} đ, nạp {depositAdded:N0} đ vào cọc)";
            }
            else if (debtPaid > 0)
            {
                noteDetail = $"Nạp {amount:N0} đ trừ nợ phạt (Nợ còn lại: {newDebt:N0} đ)";
            }
            else
            {
                noteDetail = string.IsNullOrWhiteSpace(notes) ? "Nạp bổ sung tiền cọc thế chân" : notes.Trim();
            }

            if (!string.IsNullOrWhiteSpace(notes) && debtPaid > 0)
            {
                noteDetail += $" | {notes.Trim()}";
            }

            using (var scope = new TransactionExecutionScope())
            {
                try
                {
                    _memberRepo.UpdateDepositAndDebt(memberId, newBalance, newDebt, scope.Transaction);

                    var trans = new DepositTransactionEntity
                    {
                        MemberId = memberId,
                        HandledByUserId = handledByUserId > 0 ? handledByUserId : 1,
                        TransactionType = debtPaid > 0 && depositAdded == 0 ? "DEBT_PAYMENT" : "TOP_UP",
                        Amount = amount,
                        BalanceAfter = newBalance,
                        ReceiptCode = receiptCode,
                        TransactionDate = DateTime.Now,
                        Notes = noteDetail
                    };

                    _depositRepo.InsertTransaction(trans, scope.Transaction);
                    scope.Commit();

                    // Gửi email thông báo biến động số dư / nạp tiền tự động (Non-blocking)
                    EmailNotificationService.Instance.SendDepositBalanceChangeEmailAsync(
                        member.Email,
                        member.FullName,
                        member.MemberCardCode,
                        receiptCode,
                        amount,
                        debtPaid,
                        newBalance,
                        DateTime.Now
                    );

                    return true;
                }
                catch (Exception ex)
                {
                    scope.Rollback();
                    errorMessage = "Lỗi hệ thống khi nạp tiền: " + ex.Message;
                    return false;
                }
            }
        }

        public virtual bool CloseMemberCard(int memberId, int handledByUserId, string closureReason, out string errorMessage)
        {
            return CloseMemberCard(memberId, handledByUserId, closureReason, "CASH", null, null, null, out _, out errorMessage);
        }

        public virtual bool CloseMemberCard(
            int memberId,
            int handledByUserId,
            string closureReason,
            string paymentMethod,
            string bankName,
            string accountNumber,
            string accountHolder,
            out string receiptCode,
            out string errorMessage)
        {
            receiptCode = string.Empty;
            errorMessage = string.Empty;

            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Độc giả không tồn tại.";
                return false;
            }

            if (member.CardStatus == "CLOSED")
            {
                errorMessage = "Thẻ này đã ở trạng thái Đã Hủy trước đó.";
                return false;
            }

            int borrowingCount = _memberRepo.GetCurrentlyBorrowingCount(memberId);
            if (borrowingCount > 0)
            {
                errorMessage = $"Không thể hủy thẻ vì độc giả hiện vẫn đang mượn {borrowingCount} cuốn sách chưa trả. Vui lòng nhận trả toàn bộ sách trước khi hủy thẻ!";
                return false;
            }

            // Thuật toán tính tiền hoàn cọc thực nhận: RefundAmount = DepositBalance - TotalDebt
            decimal refundAmount = member.DepositBalance - member.TotalDebt;
            if (refundAmount < 0)
            {
                errorMessage = $"Không thể hủy thẻ vì tiền cọc ({member.DepositBalance:N0} VNĐ) không đủ bù khoản nợ phạt ({member.TotalDebt:N0} VNĐ). Vui lòng nộp thêm {(-refundAmount):N0} VNĐ trước khi hủy thẻ!";
                return false;
            }

            receiptCode = $"REF-{DateTime.Now:yyyyMMddHHmmss}";
            string cleanReason = string.IsNullOrWhiteSpace(closureReason) ? "Độc giả yêu cầu hủy thẻ" : closureReason.Trim();
            string methodCode = string.Equals(paymentMethod, "BANK_TRANSFER", StringComparison.OrdinalIgnoreCase) ? "BANK_TRANSFER" : "CASH";
            string methodDisplay = methodCode == "BANK_TRANSFER" ? "Chuyển khoản" : "Tiền mặt";

            string noteDetail;
            if (methodCode == "BANK_TRANSFER" && !string.IsNullOrWhiteSpace(accountNumber))
            {
                string bankInfo = $"{bankName ?? "Ngân hàng"} - STK: {accountNumber} ({accountHolder ?? member.FullName})";
                if (member.TotalDebt > 0 && refundAmount > 0)
                {
                    noteDetail = $"Hủy thẻ: Cấn trừ nợ {member.TotalDebt:N0} đ, hoàn cọc chuyển khoản {refundAmount:N0} đ [{bankInfo}] | {cleanReason}";
                }
                else
                {
                    noteDetail = $"Hủy thẻ hoàn cọc chuyển khoản [{bankInfo}]: {cleanReason}";
                }
            }
            else
            {
                if (member.TotalDebt > 0 && refundAmount > 0)
                {
                    noteDetail = $"Hủy thẻ: Cấn trừ nợ {member.TotalDebt:N0} đ, hoàn cọc thực nhận {refundAmount:N0} đ ({methodDisplay}) | {cleanReason}";
                }
                else if (member.TotalDebt > 0)
                {
                    noteDetail = $"Hủy thẻ: Cấn trừ toàn bộ nợ {member.TotalDebt:N0} đ bằng tiền cọc (thực nhận 0 đ) | {cleanReason}";
                }
                else
                {
                    noteDetail = $"Hủy thẻ hoàn cọc ({methodDisplay}): {cleanReason}";
                }
            }

            using (var scope = new TransactionExecutionScope())
            {
                try
                {
                    _memberRepo.UpdateCardStatus(memberId, "CLOSED", scope.Transaction);
                    _memberRepo.UpdateDepositAndDebt(memberId, 0, 0, scope.Transaction);

                    var userRepo = new DataAccessLayer.DataRepositories.UserRepository();
                    var readerUser = userRepo.GetUserByMemberId(memberId, scope.Transaction);
                    if (readerUser != null)
                    {
                        userRepo.UpdateUserStatus(readerUser.UserId, false, scope.Transaction);
                    }

                    if (member.DepositBalance > 0 || refundAmount > 0)
                    {
                        var trans = new DepositTransactionEntity
                        {
                            MemberId = memberId,
                            HandledByUserId = handledByUserId > 0 ? handledByUserId : 1,
                            TransactionType = "REFUND",
                            Amount = refundAmount,
                            BalanceAfter = 0,
                            ReceiptCode = receiptCode,
                            TransactionDate = DateTime.Now,
                            Notes = noteDetail
                        };

                        _depositRepo.InsertTransaction(trans, scope.Transaction);
                    }

                    scope.Commit();

                    // Gửi email xác nhận tất toán hoàn cọc / hủy thẻ tự động (Non-blocking)
                    string emailBankName = methodCode == "BANK_TRANSFER" ? (bankName ?? "Chuyển khoản") : "Tại quầy thư viện";
                    string emailAccountNum = methodCode == "BANK_TRANSFER" ? (accountNumber ?? "-") : "-";
                    string emailAccountHolder = methodCode == "BANK_TRANSFER" ? (accountHolder ?? member.FullName) : member.FullName;

                    EmailNotificationService.Instance.SendCardClosureRefundEmailAsync(
                        member.Email,
                        member.FullName,
                        member.MemberCardCode,
                        receiptCode,
                        refundAmount,
                        methodCode,
                        emailBankName,
                        emailAccountNum,
                        emailAccountHolder,
                        DateTime.Now
                    );

                    return true;
                }
                catch (Exception ex)
                {
                    scope.Rollback();
                    errorMessage = "Lỗi khi hủy thẻ: " + ex.Message;
                    return false;
                }
            }
        }

        /// <summary>
        /// Kiểm tra điều kiện hoàn tiền cọc cho độc giả (chặn ngay trước khi mở form)
        /// </summary>
        public virtual bool ValidateDepositRefundEligibility(int memberId, out MemberEntity member, out int borrowingCount, out string errorMessage)
        {
            member = null;
            borrowingCount = 0;
            errorMessage = string.Empty;

            if (memberId <= 0)
            {
                errorMessage = "Vui lòng chọn độc giả cần thực hiện hoàn cọc.";
                return false;
            }

            member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Hồ sơ độc giả không tồn tại trong hệ thống.";
                return false;
            }

            borrowingCount = _memberRepo.GetCurrentlyBorrowingCount(memberId);
            if (borrowingCount > 0)
            {
                errorMessage = $"Không thể hoàn cọc! Độc giả đang giữ sách hoặc có nợ xấu.\r\n(Chi tiết: Độc giả hiện đang mượn {borrowingCount} cuốn sách chưa trả. Vui lòng nhận trả toàn bộ sách trước khi hoàn cọc!).";
                return false;
            }

            if (member.TotalDebt > 0)
            {
                errorMessage = $"Không thể hoàn cọc! Độc giả đang giữ sách hoặc có nợ xấu.\r\n(Chi tiết: Độc giả đang có khoản nợ phạt {member.TotalDebt:N0} VNĐ chưa thanh toán. Vui lòng nộp tiền trả nợ trước!).";
                return false;
            }

            if (member.DepositBalance <= 0)
            {
                errorMessage = "Số dư tiền cọc của độc giả hiện tại là 0 VNĐ. Không có khoản tiền cọc nào để hoàn lại!";
                return false;
            }

            if (member.CardStatus == "CLOSED")
            {
                errorMessage = "Thẻ này đã bị Hủy (CLOSED) và đã hoàn tất thủ tục hoàn cọc trước đó.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Thực thi giao dịch hoàn trả tiền cọc cho độc giả và sinh Phiếu Chi (Expense Voucher)
        /// </summary>
        public virtual bool ProcessDepositRefund(int memberId, int handledByUserId, decimal refundAmount, string paymentMethod, string reason, out string receiptCode, out string errorMessage)
        {
            receiptCode = string.Empty;
            errorMessage = string.Empty;

            if (!ValidateDepositRefundEligibility(memberId, out MemberEntity member, out _, out errorMessage))
            {
                return false;
            }

            if (refundAmount <= 0)
            {
                errorMessage = "Số tiền hoàn lại phải lớn hơn 0 VNĐ.";
                return false;
            }

            if (refundAmount > member.DepositBalance)
            {
                errorMessage = $"Số tiền hoàn trả ({refundAmount:N0} VNĐ) không thể vượt quá số dư cọc hiện tại ({member.DepositBalance:N0} VNĐ).";
                return false;
            }

            receiptCode = $"EXP-{DateTime.Now:yyyyMMddHHmmss}";
            string methodDisplay = paymentMethod == "BANK_TRANSFER" ? "Chuyển khoản" : "Tiền mặt";
            string cleanReason = string.IsNullOrWhiteSpace(reason) ? "Độc giả yêu cầu hoàn trả tiền cọc" : reason.Trim();
            string fullNotes = $"[Phiếu chi hoàn cọc - {methodDisplay}] {cleanReason}";

            using (var scope = new TransactionExecutionScope())
            {
                try
                {
                    decimal newBalance = member.DepositBalance - refundAmount;
                    _memberRepo.UpdateDepositBalance(memberId, newBalance, scope.Transaction);

                    var trans = new DepositTransactionEntity
                    {
                        MemberId = memberId,
                        HandledByUserId = handledByUserId > 0 ? handledByUserId : 1,
                        TransactionType = "REFUND",
                        Amount = refundAmount,
                        BalanceAfter = newBalance,
                        ReceiptCode = receiptCode,
                        TransactionDate = DateTime.Now,
                        Notes = fullNotes
                    };

                    _depositRepo.InsertTransaction(trans, scope.Transaction);
                    scope.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    scope.Rollback();
                    errorMessage = "Lỗi hệ thống khi thực hiện hoàn cọc: " + ex.Message;
                    return false;
                }
            }
        }

        /// <summary>
        /// Tạo ảnh Phiếu Chi Hoàn Tiền Cọc (Expense Voucher) chuẩn in ấn nhiệt GDI+
        /// </summary>
        public virtual Bitmap GenerateExpenseVoucherBitmap(string receiptCode, string memberCardCode, string fullName, string phone, decimal refundAmount, string paymentMethodName, string reason, string handledByName)
        {
            int width = 320;
            int height = 400;
            var bmp = new Bitmap(width, height);

            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);

                using (var fontHeader = new Font("Segoe UI", 10F, FontStyle.Bold))
                using (var fontSub = new Font("Segoe UI", 8F))
                using (var fontBold = new Font("Segoe UI", 9F, FontStyle.Bold))
                using (var fontLarge = new Font("Segoe UI", 11F, FontStyle.Bold))
                using (var fontRegular = new Font("Segoe UI", 8.5F))
                using (var fontItalic = new Font("Segoe UI", 8F, FontStyle.Italic))
                using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (var redBrush = new SolidBrush(Color.FromArgb(185, 28, 28)))
                {
                    g.DrawString("HỆ THỐNG THƯ VIỆN LIBOPS", fontHeader, brush, 60, 10);
                    g.DrawString("THƯ VIỆN ĐIỆN TỬ LIB-OPS", fontSub, brush, 88, 28);
                    g.DrawString("PHIẾU CHI HOÀN TRẢ TIỀN CỌC", fontLarge, redBrush, 35, 48);
                    g.DrawString($"Số phiếu: {receiptCode}", fontSub, brush, 15, 75);
                    g.DrawString($"Ngày lập: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", fontSub, brush, 15, 92);

                    using (var pen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                    {
                        g.DrawLine(pen, 15, 112, width - 15, 112);
                    }

                    g.DrawString("Độc giả:", fontRegular, brush, 15, 122);
                    g.DrawString($"{memberCardCode} - {fullName}", fontBold, brush, 85, 122);

                    g.DrawString("Số ĐT:", fontRegular, brush, 15, 145);
                    g.DrawString(phone ?? string.Empty, fontRegular, brush, 85, 145);

                    g.DrawString("Hình thức:", fontRegular, brush, 15, 168);
                    g.DrawString(paymentMethodName, fontBold, brush, 85, 168);

                    g.DrawString("Lý do chi:", fontRegular, brush, 15, 191);
                    g.DrawString(reason ?? "Rút tiền cọc thế chân", fontRegular, brush, new RectangleF(85, 191, width - 100, 35));

                    using (var pen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                    {
                        g.DrawLine(pen, 15, 230, width - 15, 230);
                    }

                    g.DrawString("SỐ TIỀN CHI:", fontBold, brush, 15, 242);
                    g.DrawString($"{refundAmount:N0} VNĐ", fontLarge, redBrush, 120, 240);

                    g.DrawString($"Thủ thư chi: {handledByName}", fontItalic, brush, 15, 275);

                    g.DrawString("Người nhận tiền", fontBold, brush, 20, 305);
                    g.DrawString("Thủ thư lập phiếu", fontBold, brush, 180, 305);
                    g.DrawString("(Ký, ghi rõ họ tên)", fontSub, brush, 25, 322);
                    g.DrawString("(Ký, ghi rõ họ tên)", fontSub, brush, 185, 322);
                }

                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1))
                {
                    g.DrawRectangle(pen, 0, 0, width - 1, height - 1);
                }
            }

            return bmp;
        }

        /// <summary>
        /// Tạo thẻ thư viện điện tử dạng Bitmap để in thẻ cho độc giả
        /// </summary>
        public virtual Bitmap GenerateMemberCardBitmap(string memberCardCode, string fullName, string phone, string issueDate)
        {
            int width = 360;
            int height = 220;
            var bmp = new Bitmap(width, height);

            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);

                // Header màu xanh dương đậm
                using (var headerBrush = new SolidBrush(Color.FromArgb(30, 58, 138)))
                {
                    g.FillRectangle(headerBrush, 0, 0, width, 45);
                }

                // Tiêu đề trường
                using (var fontUni = new Font("Segoe UI", 9F, FontStyle.Bold))
                using (var brushUni = new SolidBrush(Color.White))
                {
                    g.DrawString("HỆ THỐNG THƯ VIỆN LIBOPS", fontUni, brushUni, new PointF(12, 6));
                }
                using (var fontCardTitle = new Font("Segoe UI", 10.5F, FontStyle.Bold))
                using (var brushCardTitle = new SolidBrush(Color.FromArgb(254, 240, 138)))
                {
                    g.DrawString("THẺ ĐỘC GIẢ THƯ VIỆN", fontCardTitle, brushCardTitle, new PointF(12, 22));
                }

                // Thông tin độc giả
                using (var fontLabel = new Font("Segoe UI", 8.5F, FontStyle.Bold))
                using (var brushLabel = new SolidBrush(Color.FromArgb(100, 116, 139)))
                using (var fontValue = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                using (var brushValue = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    g.DrawString("Họ và tên:", fontLabel, brushLabel, 16, 55);
                    g.DrawString(fullName ?? string.Empty, fontValue, brushValue, 90, 53);

                    g.DrawString("Số ĐT:", fontLabel, brushLabel, 16, 75);
                    g.DrawString(phone ?? string.Empty, fontValue, brushValue, 90, 73);

                    g.DrawString("Ngày cấp:", fontLabel, brushLabel, 16, 95);
                    g.DrawString(issueDate ?? string.Empty, fontValue, brushValue, 90, 93);
                }

                // Vẽ Barcode mã thẻ bên dưới để quét mượn sách
                using (var fontCode = new Font("Consolas", 10F, FontStyle.Bold))
                using (var brushCode = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (var brushBar = new SolidBrush(Color.Black))
                {
                    int startX = 20;
                    int barHeight = 45;
                    int currentX = startX;

                    byte[] textBytes = System.Text.Encoding.ASCII.GetBytes(memberCardCode ?? "DG202600001");
                    for (int i = 0; i < textBytes.Length; i++)
                    {
                        int val = textBytes[i];
                        int barWidth = (val % 3) + 1;
                        int gap = ((val / 3) % 2) + 1;

                        g.FillRectangle(brushBar, currentX, 125, barWidth, barHeight);
                        currentX += barWidth + gap;

                        if (currentX > width - 25) break;
                    }

                    g.DrawString(memberCardCode ?? string.Empty, fontCode, brushCode, 20, 178);
                }

                // Viền ngoài
                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1))
                {
                    g.DrawRectangle(pen, 0, 0, width - 1, height - 1);
                }
            }

            return bmp;
        }

        /// <summary>
        /// Tạo biên lai giao dịch thu / hoàn tiền cọc thế chân
        /// </summary>
        public virtual Bitmap GenerateDepositReceiptBitmap(string receiptCode, string memberCardCode, string fullName, string typeName, decimal amount, decimal balanceAfter, string handledByName)
        {
            int width = 300;
            int height = 340;
            var bmp = new Bitmap(width, height);

            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);

                using (var fontHeader = new Font("Segoe UI", 10F, FontStyle.Bold))
                using (var fontSub = new Font("Segoe UI", 8F))
                using (var fontBold = new Font("Segoe UI", 9F, FontStyle.Bold))
                using (var fontRegular = new Font("Segoe UI", 8.5F))
                using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    g.DrawString("HỆ THỐNG THƯ VIỆN LIBOPS", fontHeader, brush, 65, 10);

                    g.DrawString("BIÊN LAI GIAO DỊCH TIỀN CỌC", fontBold, brush, 55, 30);
                    g.DrawString($"Số: {receiptCode}", fontSub, brush, 15, 55);
                    g.DrawString($"Ngày: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", fontSub, brush, 15, 72);

                    using (var pen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                    {
                        g.DrawLine(pen, 15, 92, width - 15, 92);
                    }

                    g.DrawString("Mã độc giả:", fontRegular, brush, 15, 105);
                    g.DrawString(memberCardCode, fontBold, brush, 105, 105);

                    g.DrawString("Họ và tên:", fontRegular, brush, 15, 128);
                    g.DrawString(fullName, fontBold, brush, 105, 128);

                    g.DrawString("Loại GD:", fontRegular, brush, 15, 151);
                    g.DrawString(typeName, fontBold, brush, 105, 151);

                    g.DrawString("Số tiền:", fontRegular, brush, 15, 174);
                    g.DrawString($"{amount:N0} VNĐ", fontBold, brush, 105, 174);

                    g.DrawString("Số dư cọc:", fontRegular, brush, 15, 197);
                    g.DrawString($"{balanceAfter:N0} VNĐ", fontBold, brush, 105, 197);

                    using (var pen = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                    {
                        g.DrawLine(pen, 15, 222, width - 15, 222);
                    }

                    g.DrawString($"Thủ thư lập: {handledByName}", fontRegular, brush, 15, 235);
                    g.DrawString("Người nộp/nhận tiền", fontBold, brush, 15, 260);
                    g.DrawString("Người lập phiếu", fontBold, brush, 175, 260);
                    g.DrawString("(Ký, ghi rõ họ tên)", fontSub, brush, 20, 278);
                    g.DrawString("(Ký, ghi rõ họ tên)", fontSub, brush, 180, 278);
                }

                using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1))
                {
                    g.DrawRectangle(pen, 0, 0, width - 1, height - 1);
                }
            }

            return bmp;
        }

        public virtual List<MemberDepositHistoryDto> GetMemberDepositHistory(int memberId)
        {
            return _depositRepo.GetMemberDepositHistory(memberId);
        }

        public virtual List<MemberFineHistoryDto> GetMemberFineHistory(int memberId)
        {
            return _fineRepo.GetMemberFineHistory(memberId);
        }

        /// <summary>
        /// Tạm khóa hoặc mở khóa thẻ độc giả
        /// </summary>
        public virtual bool ToggleMemberCardStatus(int memberId, int handledByUserId, out string newStatus, out string message, out string errorMessage)
        {
            newStatus = string.Empty;
            message = string.Empty;
            errorMessage = string.Empty;

            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Độc giả không tồn tại.";
                return false;
            }

            if (member.CardStatus == "CLOSED")
            {
                errorMessage = "Không thể thay đổi trạng thái của thẻ đã bị hủy (CLOSED).";
                return false;
            }

            if (member.CardStatus == "ACTIVE")
            {
                newStatus = "LOCKED";
                bool ok = _memberRepo.UpdateCardStatus(memberId, "LOCKED");
                if (!ok)
                {
                    errorMessage = "Lỗi hệ thống: Không thể cập nhật trạng thái tạm khóa thẻ.";
                    return false;
                }
                message = $"Đã tạm khóa thẻ độc giả {member.MemberCardCode} - {member.FullName} thành công.";
                return true;
            }
            else // LOCKED hoặc trạng thái khác
            {
                newStatus = "ACTIVE";
                bool ok = _memberRepo.UpdateCardStatus(memberId, "ACTIVE");
                if (!ok)
                {
                    errorMessage = "Lỗi hệ thống: Không thể mở khóa thẻ.";
                    return false;
                }
                message = $"Đã mở khóa hoạt động trở lại cho thẻ độc giả {member.MemberCardCode} - {member.FullName} thành công.";
                return true;
            }
        }

        /// <summary>
        /// Gia hạn thời hạn thẻ độc giả thêm 1 năm (hoặc theo số ngày cấu hình hệ thống)
        /// </summary>
        public virtual bool RenewMemberCard(int memberId, int handledByUserId, out DateTime newExpiryDate, out string message, out string errorMessage)
        {
            newExpiryDate = DateTime.MinValue;
            message = string.Empty;
            errorMessage = string.Empty;

            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Độc giả không tồn tại.";
                return false;
            }

            if (member.CardStatus == "CLOSED")
            {
                errorMessage = "Không thể gia hạn thẻ đã bị hủy (CLOSED). Độc giả cần đăng ký thẻ mới!";
                return false;
            }

            if (member.DepositBalance < 0)
            {
                errorMessage = $"Độc giả đang có nợ phạt chưa thanh toán ({Math.Abs(member.DepositBalance):N0} VNĐ). Vui lòng nạp tiền thanh toán hết nợ trước khi gia hạn thẻ!";
                return false;
            }

            if (member.CardStatus == "LOCKED")
            {
                errorMessage = "Thẻ độc giả đang ở trạng thái Tạm Khóa (LOCKED). Vui lòng mở khóa thẻ trước khi thực hiện gia hạn!";
                return false;
            }

            var configService = new SystemSettingsService();
            int validityDays = configService.GetMemberValidityDays();
            if (validityDays <= 0) validityDays = 365;

            // Nếu thẻ còn hạn thì cộng tiếp từ ngày hết hạn cũ; nếu đã quá hạn thì tính từ ngày hiện tại + validityDays
            if (member.ExpiryDate > DateTime.Now)
            {
                newExpiryDate = member.ExpiryDate.AddDays(validityDays);
            }
            else
            {
                newExpiryDate = DateTime.Now.AddDays(validityDays);
            }

            bool ok = _memberRepo.UpdateExpiryDate(memberId, newExpiryDate);
            if (!ok)
            {
                errorMessage = "Lỗi hệ thống: Không thể cập nhật ngày hết hạn mới cho thẻ.";
                return false;
            }

            message = $"Gia hạn thẻ độc giả {member.MemberCardCode} - {member.FullName} thành công!\r\nHạn thẻ mới: {newExpiryDate:dd/MM/yyyy} (+{validityDays} ngày).";
            return true;
        }

        /// <summary>
        /// Lấy danh sách độc giả có thẻ hết hạn đang có nợ phạt
        /// </summary>
        public virtual List<ExpiredMemberDebtDto> GetExpiredMembersWithDebt()
        {
            return _memberRepo.GetExpiredMembersWithDebt();
        }

        /// <summary>
        /// Thực thi cấn trừ nợ phạt vào tiền cọc cho độc giả có thẻ hết hạn (Có chốt chặn an toàn: Bắt buộc số sách đang giữ = 0)
        /// </summary>
        public virtual bool SettleExpiredMemberDebt(int memberId, int handledByUserId, out string message, out string errorMessage)
        {
            message = string.Empty;
            errorMessage = string.Empty;

            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Không tìm thấy hồ sơ độc giả.";
                return false;
            }

            int borrowingCount = _memberRepo.GetCurrentlyBorrowingCount(memberId);
            if (borrowingCount > 0)
            {
                errorMessage = $"Độc giả {member.MemberCardCode} - {member.FullName} hiện vẫn đang giữ {borrowingCount} cuốn sách chưa trả!\r\n" +
                               "Hệ thống khóa thao tác cấn trừ cọc để bảo vệ tài sản của thư viện. Thủ thư bắt buộc phải thu hồi sách hoặc lập phiếu báo mất trước khi cấn trừ!";
                return false;
            }

            if (member.TotalDebt <= 0)
            {
                errorMessage = "Độc giả không có nợ phạt cần xử lý cấn trừ.";
                return false;
            }

            if (member.DepositBalance <= 0)
            {
                errorMessage = "Số dư tiền cọc của độc giả bằng 0 VNĐ, không còn tiền cọc để cấn trừ.";
                return false;
            }

            decimal settleAmount = Math.Min(member.DepositBalance, member.TotalDebt);
            decimal newBalance = member.DepositBalance - settleAmount;
            decimal newDebt = member.TotalDebt - settleAmount;
            string newStatus = (newDebt > 0) ? "LOCKED" : (member.CardStatus == "CLOSED" ? "CLOSED" : "EXPIRED");

            string receiptCode = $"REC-SETTLE-{DateTime.Now:yyyyMMddHHmmss}-{memberId:D4}";

            using (var scope = new TransactionExecutionScope())
            {
                try
                {
                    _memberRepo.UpdateDepositAndDebt(memberId, newBalance, newDebt, scope.Transaction);
                    _memberRepo.UpdateCardStatus(memberId, newStatus, scope.Transaction);

                    // 1. Ghi nhận giao dịch cấn trừ nợ vào sổ cái tiền cọc
                    var depTrans = new DepositTransactionEntity
                    {
                        MemberId = memberId,
                        HandledByUserId = handledByUserId > 0 ? handledByUserId : 1,
                        TransactionType = "DEBT_SETTLEMENT",
                        Amount = -settleAmount,
                        BalanceAfter = newBalance,
                        ReceiptCode = receiptCode,
                        TransactionDate = DateTime.Now,
                        Notes = $"Cấn trừ nợ phạt thẻ hết hạn {settleAmount:N0} đ (Nợ cũ: {member.TotalDebt:N0} đ -> Nợ mới: {newDebt:N0} đ | Cọc còn lại: {newBalance:N0} đ)"
                    };
                    _depositRepo.InsertTransaction(depTrans, scope.Transaction);

                    // 2. Ghi nhận biên lai thu phạt vào FineReceipts
                    var fineReceipt = new FineReceiptEntity
                    {
                        ReceiptCode = receiptCode,
                        MemberId = memberId,
                        BorrowSlipId = null,
                        CollectedByUserId = handledByUserId > 0 ? handledByUserId : 1,
                        TotalAmount = settleAmount,
                        PaymentMethod = "DEPOSIT_DEDUCTION",
                        Reason = $"Cấn trừ nợ phạt thẻ hết hạn vào tiền cọc ({member.MemberCardCode} - {member.FullName})",
                        PaymentDate = DateTime.Now,
                        Notes = $"Xử lý đối soát cấn trừ nợ ngày {DateTime.Now:dd/MM/yyyy HH:mm}"
                    };
                    _fineRepo.InsertFineReceipt(fineReceipt, scope.Transaction);

                    scope.Commit();

                    message = $"Đã cấn trừ thành công {settleAmount:N0} VNĐ cho độc giả {member.MemberCardCode} - {member.FullName}.\r\n" +
                              $"- Nợ còn lại: {newDebt:N0} VNĐ\r\n" +
                              $"- Tiền cọc còn lại: {newBalance:N0} VNĐ\r\n" +
                              $"- Trạng thái thẻ: {newStatus}";
                    return true;
                }
                catch (Exception ex)
                {
                    scope.Rollback();
                    errorMessage = "Lỗi hệ thống khi thực thi cấn trừ nợ: " + ex.Message;
                    return false;
                }
            }
        }

        /// <summary>
        /// Thực thi cấn trừ nợ tự động hàng loạt cho tất cả thẻ hết hạn (Tự động BỎ QUA các độc giả đang giữ sách)
        /// </summary>
        public virtual void SettleAllExpiredMembersDebt(
            int handledByUserId, 
            out int settledCount, 
            out int skippedBorrowingCount, 
            out int skippedZeroDepositCount, 
            out decimal totalSettledAmount, 
            out string summaryMessage)
        {
            settledCount = 0;
            skippedBorrowingCount = 0;
            skippedZeroDepositCount = 0;
            totalSettledAmount = 0;

            var list = GetExpiredMembersWithDebt();
            foreach (var item in list)
            {
                if (item.CurrentlyBorrowingCount > 0)
                {
                    skippedBorrowingCount++;
                    continue; // BẮT BUỘC BỎ QUA theo quy tắc bảo toàn cọc
                }

                if (item.DepositBalance <= 0)
                {
                    skippedZeroDepositCount++;
                    continue;
                }

                if (SettleExpiredMemberDebt(item.MemberId, handledByUserId, out _, out _))
                {
                    settledCount++;
                    totalSettledAmount += item.SettlementAmount;
                }
            }

            summaryMessage = $"Kết quả xử lý cấn trừ nợ hàng loạt:\r\n" +
                             $"- Đã cấn trừ thành công: {settledCount} độc giả (Tổng tiền thu hồi: {totalSettledAmount:N0} VNĐ)\r\n" +
                             $"- Bỏ qua (Đang giữ sách chưa trả): {skippedBorrowingCount} độc giả\r\n" +
                             $"- Bỏ qua (Hết tiền cọc): {skippedZeroDepositCount} độc giả";
        }
    }
}
