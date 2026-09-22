using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// DTO tra cứu sách tối ưu hiển thị cho Độc giả
    /// </summary>
    public class ReaderBookSearchDto
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string AuthorName { get; set; }
        public string CategoryName { get; set; }
        public string PublisherName { get; set; }
        public int? PublishYear { get; set; }
        public string Isbn { get; set; }
        public decimal Price { get; set; }
        public string ShelfLocation { get; set; }
        public string Summary { get; set; }
        public int TotalQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public string CoverImagePath { get; set; }
        public int BorrowCount { get; set; }
        public int CategoryId { get; set; }
        public int Rank { get; set; }
        public string RankBadge => Rank == 1 ? "#1" : (Rank == 2 ? "#2" : (Rank == 3 ? "#3" : $"#{Rank}"));

        public string FormattedPrice => $"{Price:N0} đ";

        public string AvailabilityStatusDisplay => AvailableQuantity > 0 
            ? $"Còn {AvailableQuantity}/{TotalQuantity} cuốn" 
            : $"Đang mượn hết (0/{TotalQuantity})";

        public bool IsAvailable => AvailableQuantity > 0;
        public bool HasCoverImage => !string.IsNullOrWhiteSpace(CoverImagePath);
    }

    /// <summary>
    /// DTO thể loại sách kèm số lượng đầu sách
    /// </summary>
    public class CategoryCountDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int BookCount { get; set; }
        public string DisplayText => $"{CategoryName} ({BookCount})";
    }

    /// <summary>
    /// DTO hiển thị danh sách sách độc giả đang mượn
    /// </summary>
    public class ReaderLoanDisplayDto
    {
        public int BorrowSlipId { get; set; }
        public int DetailId { get; set; }
        public string SlipCode { get; set; }
        public string Barcode { get; set; }
        public string BookTitle { get; set; }
        public string CategoryName { get; set; }
        public string ShelfLocation { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public int RenewalCount { get; set; }
        public int MaxRenewalCount { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsOverdue { get; set; }

        public string FormattedBorrowDate => BorrowDate.ToString("dd/MM/yyyy");
        public string FormattedDueDate => DueDate.ToString("dd/MM/yyyy");

        public string StatusDisplay
        {
            get
            {
                if (IsOverdue)
                {
                    return $"Quá hạn {Math.Abs(DaysRemaining)} ngày";
                }
                if (DaysRemaining == 0)
                {
                    return "Đến hạn hôm nay";
                }
                return $"Còn {DaysRemaining} ngày";
            }
        }

        public string RenewalStatusDisplay => $"Đã gia hạn {RenewalCount}/{MaxRenewalCount} lần";
        public bool CanRenew => !IsOverdue && RenewalCount < MaxRenewalCount;
    }

    /// <summary>
    /// DTO hiển thị lịch sử phạt Và bồi thường của độc giả
    /// </summary>
    public class ReaderFineHistoryDto
    {
        public int ReceiptId { get; set; }
        public string ReceiptCode { get; set; }
        public string SlipCode { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string Reason { get; set; }
        public string CollectedByStaffName { get; set; }
        public string Notes { get; set; }

        public string FormattedPaymentDate => PaymentDate.ToString("dd/MM/yyyy HH:mm");
        public string FormattedTotalAmount => $"{TotalAmount:N0} đ";

        public string PaymentMethodDisplay => PaymentMethod == "DEPOSIT_DEDUCTION" 
            ? "Khấu trừ cọc" 
            : "Tiền mặt tại quầy";
    }

    /// <summary>
    /// DTO hiển thị lịch sử trả sách của độc giả
    /// </summary>
    public class ReaderBorrowHistoryDto
    {
        public int DetailId { get; set; }
        public string SlipCode { get; set; }
        public string Barcode { get; set; }
        public string BookTitle { get; set; }
        public decimal Price { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime ActualReturnDate { get; set; }
        public int OverdueDays { get; set; }
        public decimal FineAmount { get; set; }
        public string BookCopyStatusAfterReturn { get; set; }
        public string ReceivedByStaffName { get; set; }
        public string ReturnConditionNote { get; set; }

        public string FormattedBorrowDate => BorrowDate.ToString("dd/MM/yyyy");
        public string FormattedDueDate => DueDate.ToString("dd/MM/yyyy");
        public string FormattedActualReturnDate => ActualReturnDate.ToString("dd/MM/yyyy");
        public string FormattedFineAmount => $"{FineAmount:N0} đ";

        public string BookCopyStatusDisplay
        {
            get
            {
                switch (BookCopyStatusAfterReturn)
                {
                    case "AVAILABLE": return "Nguyên vẹn";
                    case "DAMAGED": return "Hư hỏng";
                    case "LOST": return "Báo mất";
                    default: return BookCopyStatusAfterReturn ?? "Nguyên vẹn";
                }
            }
        }

        public string OverdueDaysDisplay => OverdueDays > 0 ? $"{OverdueDays} ngày" : "Đúng hạn";
        public string StatusDisplay => OverdueDays > 0 ? $"Trả trễ {OverdueDays} ngày" : "Đúng hạn";
    }

    /// <summary>
    /// DTO tổng hợp thông tin hồ sơ thẻ thư viện Và tài chính độc giả
    /// </summary>
    public class ReaderCardSummaryDto
    {
        public int MemberId { get; set; }
        public string MemberCardCode { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string IdentityCardNumber { get; set; }
        public string Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public decimal DepositBalance { get; set; }
        public decimal TotalDebt { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string CardStatus { get; set; }
        public int ActiveBorrowCount { get; set; }
        public int OverdueBorrowCount { get; set; }
        public bool HasPendingRequest { get; set; }
        public string PendingRequestType { get; set; }

        public bool IsActive => CardStatus == "ACTIVE" && ExpiryDate >= DateTime.Today;
        public bool IsExpired => ExpiryDate < DateTime.Today;
        public bool IsLocked => CardStatus == "LOCKED";
        public bool IsClosed => CardStatus == "CLOSED";

        public string CardStatusDisplay
        {
            get
            {
                if (IsClosed) return "Đã hủy thẻ";
                if (IsLocked) return "Bị tạm khóa";
                if (IsExpired) return "Hết hạn sử dụng";
                if (HasPendingRequest) return "Đang chờ duyệt yêu cầu";
                return "Đang hoạt động";
            }
        }
    }

    /// <summary>
    /// DTO danh sách yêu cầu tự phục vụ (Rút cọc / Hủy thẻ) hiển thị cho Độc giả Và Thủ thư
    /// </summary>
    public class ReaderRequestDisplayDto
    {
        public int RequestId { get; set; }
        public string RequestCode { get; set; }
        public int MemberId { get; set; }
        public string MemberCardCode { get; set; }
        public string MemberFullName { get; set; }
        public string FullName => MemberFullName;
        public string MemberPhone { get; set; }
        public string RequestType { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public string FormattedAmount => $"{Amount:N0} đ";
        public string PayoutMethod { get; set; }
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
        public string BankAccountHolder { get; set; }
        public string BankDetails => PayoutMethod == "BANK_TRANSFER" 
            ? $"{BankName} - {BankAccountNumber} ({BankAccountHolder})" 
            : "Nhận tiền mặt tại quầy";
        public string Reason { get; set; }
        public DateTime RequestDate { get; set; }
        public string FormattedRequestDate => RequestDate.ToString("dd/MM/yyyy HH:mm");
        public int? ProcessedByUserId { get; set; }
        public string ProcessedByStaffName { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string StaffNotes { get; set; }

        public string RequestTypeDisplay
        {
            get
            {
                switch (RequestType)
                {
                    case LibOps.DataModels.Enums.ReaderRequestTypeEnum.CANCEL_CARD:
                        return "Hủy thẻ & Hoàn cọc";
                    case LibOps.DataModels.Enums.ReaderRequestTypeEnum.REISSUE_CARD:
                        return "Báo mất & Cấp lại thẻ";
                    case LibOps.DataModels.Enums.ReaderRequestTypeEnum.UPDATE_INFO:
                        return "Cập nhật thông tin";
                    case LibOps.DataModels.Enums.ReaderRequestTypeEnum.FEEDBACK_INQUIRY:
                        return "Góp ý & Hỗ trợ";
                    default:
                        return RequestType;
                }
            }
        }

        public string RequestTypeName => RequestTypeDisplay;

        public string StatusDisplay
        {
            get
            {
                switch (Status)
                {
                    case LibOps.DataModels.Enums.ReaderRequestStatusEnum.PENDING:
                        return "Chờ duyệt";
                    case LibOps.DataModels.Enums.ReaderRequestStatusEnum.APPROVED:
                        return "Đã phê duyệt";
                    case LibOps.DataModels.Enums.ReaderRequestStatusEnum.REJECTED:
                        return "Từ chối";
                    case LibOps.DataModels.Enums.ReaderRequestStatusEnum.CANCELLED:
                        return "Đã hủy";
                    default:
                        return Status;
                }
            }
        }

        public string StatusText => StatusDisplay;

        public string PayoutMethodDisplay => PayoutMethod == "BANK_TRANSFER" 
            ? "Chuyển khoản ngân hàng" 
            : "Tiền mặt tại quầy";
    }

    /// <summary>
    /// DTO hiển thị toàn bộ lịch sử biến động dòng tiền (nạp, nộp phạt, gia hạn, hoàn cọc, nợ)
    /// </summary>
    public class ReaderCashFlowDisplayDto
    {
        public int TransactionId { get; set; }
        public string ReceiptCode { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string TransactionTypeDisplay { get; set; }
        public decimal Amount { get; set; }
        public bool IsInflow { get; set; }
        public decimal? BalanceAfter { get; set; }
        public string PerformedByStaffName { get; set; }
        public string Notes { get; set; }

        public string FormattedTransactionDate => TransactionDate.ToString("dd/MM/yyyy HH:mm");
        public string FormattedAmount => IsInflow ? $"+{Amount:N0} đ" : $"{Amount:N0} đ";
        public string FormattedBalanceAfter => BalanceAfter.HasValue ? $"{BalanceAfter.Value:N0} đ" : "--";
        public string AmountColorBrush => IsInflow ? "#10B981" : "#EF4444";
    }
}

