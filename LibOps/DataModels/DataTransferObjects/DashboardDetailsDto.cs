using System;

namespace LibOps.DataModels.DataTransferObjects
{
    /// <summary>
    /// DTO mục biểu đồ Xu hướng Mượn - Trả theo ngày
    /// </summary>
    public class BorrowReturnTrendItemDto
    {
        public DateTime Date { get; set; }
        public string DayLabel { get; set; }
        public int BorrowCount { get; set; }
        public int ReturnCount { get; set; }
    }

    /// <summary>
    /// DTO mục biểu đồ Top đầu sách mượn nhiều nhất
    /// </summary>
    public class TopBorrowedBookItemDto
    {
        public int Rank { get; set; }
        public string BookTitle { get; set; }
        public string CategoryName { get; set; }
        public int BorrowCount { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// DTO mục danh sách sách quá hạn cần thu hồi gấp
    /// </summary>
    public class OverdueBorrowItemDto
    {
        public int SlipId { get; set; }
        public string SlipCode { get; set; }
        public string MemberCardCode { get; set; }
        public string MemberFullName { get; set; }
        public string BookTitle { get; set; }
        public DateTime DueDate { get; set; }
        public int OverdueDays { get; set; }
        public decimal EstimatedFine { get; set; }
        public string PhoneNumber { get; set; }
    }

    /// <summary>
    /// DTO mục danh sách yêu cầu chờ duyệt từ bạn đọc
    /// </summary>
    public class PendingReaderRequestItemDto
    {
        public int RequestId { get; set; }
        public string MemberCardCode { get; set; }
        public string MemberFullName { get; set; }
        public string RequestType { get; set; }
        public string RequestTypeDisplay
        {
            get
            {
                if (!string.IsNullOrEmpty(_requestTypeDisplay)) return _requestTypeDisplay;
                switch (RequestType)
                {
                    case LibOps.DataModels.Enums.ReaderRequestTypeEnum.CANCEL_CARD:
                        return "Hủy thẻ Và Hoàn cọc";
                    case LibOps.DataModels.Enums.ReaderRequestTypeEnum.REISSUE_CARD:
                        return "Báo mất Và Cấp lại thẻ";
                    case LibOps.DataModels.Enums.ReaderRequestTypeEnum.UPDATE_INFO:
                        return "Cập nhật thông tin";
                    case LibOps.DataModels.Enums.ReaderRequestTypeEnum.FEEDBACK_INQUIRY:
                        return "Góp ý Và Hỗ trợ";
                    default:
                        return RequestType;
                }
            }
            set => _requestTypeDisplay = value;
        }
        private string _requestTypeDisplay;

        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public string StatusDisplay
        {
            get
            {
                if (!string.IsNullOrEmpty(_statusDisplay)) return _statusDisplay;
                switch (Status)
                {
                    case LibOps.DataModels.Enums.ReaderRequestStatusEnum.PENDING:
                        return "Chờ duyệt";
                    case LibOps.DataModels.Enums.ReaderRequestStatusEnum.APPROVED:
                        return "Đã duyệt";
                    case LibOps.DataModels.Enums.ReaderRequestStatusEnum.REJECTED:
                        return "Từ chối";
                    case LibOps.DataModels.Enums.ReaderRequestStatusEnum.CANCELLED:
                        return "Đã hủy";
                    default:
                        return Status;
                }
            }
            set => _statusDisplay = value;
        }
        private string _statusDisplay;
    }

    /// <summary>
    /// DTO mục nhật ký hoạt động mượn/trả vừa diễn ra (Live feed)
    /// </summary>
    public class RecentActivityLogItemDto
    {
        public DateTime Timestamp { get; set; }
        public string FormattedTime { get; set; }
        public string ActivityType { get; set; }
        public string ActivityIcon { get; set; }
        public string MemberName { get; set; }
        public string BookTitle { get; set; }
        public string HandledBy { get; set; }
    }

    /// <summary>
    /// DTO mục top độc giả tích cực nhất thư viện
    /// </summary>
    public class TopActiveMemberItemDto
    {
        public int Rank { get; set; }
        public string RankIcon { get; set; }
        public string MemberCardCode { get; set; }
        public string FullName { get; set; }
        public string EmailOrPhone { get; set; }
        public int TotalBorrowCount { get; set; }
        public int ActiveBorrowCount { get; set; }
    }
}
