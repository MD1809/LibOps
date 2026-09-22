using System;
using LibOps.DataAccessLayer.DataRepositories;
using LibOps.DataModels.Entities;

namespace LibOps.BusinessLogicLayer.BusinessValidators
{
    /// <summary>
    /// Bộ thẩm định các quy tắc nghiệp vụ tự phục vụ của Độc giả (Reader Actions)
    /// </summary>
    public class ReaderActionValidator
    {
        private readonly MemberRepository _memberRepo;
        private readonly BorrowSlipRepository _slipRepo;
        private readonly ReaderRequestRepository _requestRepo;

        public ReaderActionValidator()
        {
            _memberRepo = new MemberRepository();
            _slipRepo = new BorrowSlipRepository();
            _requestRepo = new ReaderRequestRepository();
        }

        public ReaderActionValidator(
            MemberRepository memberRepo,
            BorrowSlipRepository slipRepo,
            ReaderRequestRepository requestRepo)
        {
            _memberRepo = memberRepo;
            _slipRepo = slipRepo;
            _requestRepo = requestRepo;
        }

        /// <summary>
        /// Thẩm định điều kiện tự gia hạn mượn sách (+7 ngày)
        /// </summary>
        public virtual bool ValidateLoanRenewal(int borrowSlipId, int memberId, int maxRenewalCount, out string errorMessage)
        {
            errorMessage = string.Empty;

            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Không tìm thấy thông tin độc giả.";
                return false;
            }

            if (member.CardStatus != "ACTIVE" || member.ExpiryDate.Date < DateTime.Today)
            {
                errorMessage = "Thẻ độc giả đã hết hạn hoặc bị khóa. Vui lòng gia hạn hoặc liên hệ thủ thư!";
                return false;
            }

            if (member.TotalDebt > 0)
            {
                errorMessage = $"Độc giả đang có nợ phạt chưa thanh toán ({member.TotalDebt:N0} VNĐ). Không thể gia hạn!";
                return false;
            }

            var slip = _slipRepo.GetBorrowSlipById(borrowSlipId);
            if (slip == null || slip.MemberId != memberId)
            {
                errorMessage = "Không tìm thấy thông tin phiếu mượn tương ứng của bạn.";
                return false;
            }

            if (slip.Status == "RETURNED" || slip.Status == "CANCELLED")
            {
                errorMessage = "Phiếu mượn đã hoàn tất trả sách. Không thể gia hạn!";
                return false;
            }

            if (slip.DueDate.Date < DateTime.Today)
            {
                errorMessage = $"Phiếu mượn đã QUÁ HẠN từ ngày {slip.DueDate:dd/MM/yyyy}. Bạn phải mang sách đến quầy trả và nộp phạt!";
                return false;
            }

            if (slip.RenewalCount >= maxRenewalCount)
            {
                errorMessage = $"Phiếu mượn này đã đạt giới hạn gia hạn tối đa ({slip.RenewalCount}/{maxRenewalCount} lần).";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Thẩm định điều kiện gia hạn thẻ thư viện (thu phí thường niên)
        /// </summary>
        public virtual bool ValidateCardRenewalEligibility(int memberId, decimal annualFee, bool deductFromDeposit, out string errorMessage)
        {
            errorMessage = string.Empty;

            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Không tìm thấy hồ sơ độc giả.";
                return false;
            }

            if (member.CardStatus == "LOCKED")
            {
                errorMessage = "Thẻ thư viện của bạn đang bị TẠM KHÓA bởi Thủ thư. Không thể tự gia hạn!";
                return false;
            }

            if (member.CardStatus == "CLOSED")
            {
                errorMessage = "Thẻ thư viện của bạn ĐÃ HỦY. Vui lòng liên hệ quầy để đăng ký mở thẻ mới!";
                return false;
            }

            if (deductFromDeposit)
            {
                var settingRepo = new SystemSettingRepository();
                decimal minDeposit = 200000m;
                try
                {
                    string val = settingRepo.GetSettingValue("MEMBER_CARD_DEFAULT_DEPOSIT", "200000");
                    if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal res))
                    {
                        minDeposit = res;
                    }
                }
                catch { }

                decimal requiredBalance = minDeposit + annualFee;
                if (member.DepositBalance < requiredBalance)
                {
                    errorMessage = $"Số dư tiền cọc hiện tại ({member.DepositBalance:N0} VNĐ) không đủ để duy trì tiền cọc tối thiểu ({minDeposit:N0} VNĐ) sau khi trừ phí thường niên ({annualFee:N0} VNĐ). " +
                                   $"Yêu cầu số dư trong thẻ tối thiểu {requiredBalance:N0} VNĐ. Vui lòng chọn thanh toán trực tiếp qua mã VietQR hoặc nạp thêm tiền cọc!";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Thẩm định điều kiện gửi yêu cầu rút tiền cọc (Đã vô hiệu hóa theo quy định chính sách tiền cọc)
        /// </summary>
        public virtual bool ValidateRefundRequestEligibility(int memberId, decimal amount, out string errorMessage)
        {
            errorMessage = "Hệ thống không hỗ trợ chức năng rút bớt tiền cọc. Tiền cọc chỉ được hoàn trả khi độc giả làm thủ tục Thanh lý & Đóng thẻ thư viện.";
            return false;
        }

        /// <summary>
        /// Thẩm định điều kiện gửi yêu cầu hủy thẻ thư viện
        /// </summary>
        public virtual bool ValidateClosureRequestEligibility(int memberId, out string errorMessage)
        {
            errorMessage = string.Empty;

            var member = _memberRepo.GetMemberById(memberId);
            if (member == null)
            {
                errorMessage = "Không tìm thấy hồ sơ độc giả.";
                return false;
            }

            if (member.CardStatus == "CLOSED")
            {
                errorMessage = "Thẻ thư viện của bạn đã ở trạng thái ĐÃ HỦY từ trước!";
                return false;
            }

            int borrowingCount = _memberRepo.GetCurrentlyBorrowingCount(memberId);
            if (borrowingCount > 0)
            {
                errorMessage = $"Bạn đang giữ {borrowingCount} cuốn sách mượn chưa trả. Phải trả hết sách trước khi hủy thẻ!";
                return false;
            }

            if (member.TotalDebt > 0)
            {
                errorMessage = $"Tài khoản của bạn đang có khoản nợ phạt ({member.TotalDebt:N0} VNĐ). Vui lòng thanh toán nợ trước khi hủy thẻ!";
                return false;
            }

            if (_requestRepo.HasPendingRequest(memberId))
            {
                errorMessage = "Bạn đang có một yêu cầu khác đang ở trạng thái chờ duyệt. Không thể tạo thêm yêu cầu hủy thẻ!";
                return false;
            }

            return true;
        }
    }
}
