namespace LibOps.DataModels.Enums
{
    /// <summary>
    /// Loại yêu cầu tự phục vụ của Độc giả (Reader Request Types)
    /// </summary>
    public static class ReaderRequestTypeEnum
    {
        /// <summary> Hủy thẻ thư viện và hoàn trả tiền ký quỹ </summary>
        public const string CANCEL_CARD = "CANCEL_CARD";

        /// <summary> Báo mất thẻ và xin cấp lại thẻ mới </summary>
        public const string REISSUE_CARD = "REISSUE_CARD";

        /// <summary> Yêu cầu cập nhật thông tin cá nhân/liên hệ </summary>
        public const string UPDATE_INFO = "UPDATE_INFO";

        /// <summary> Góp ý, phản ánh và yêu cầu hỗ trợ </summary>
        public const string FEEDBACK_INQUIRY = "FEEDBACK_INQUIRY";
    }

    /// <summary>
    /// Trạng thái xử lý yêu cầu tự phục vụ (Reader Request Status)
    /// </summary>
    public static class ReaderRequestStatusEnum
    {
        /// <summary> Chờ thủ thư tiếp nhận và phê duyệt </summary>
        public const string PENDING = "PENDING";

        /// <summary> Yêu cầu đã được phê duyệt và hoàn tất xử lý </summary>
        public const string APPROVED = "APPROVED";

        /// <summary> Yêu cầu bị từ chối phê duyệt </summary>
        public const string REJECTED = "REJECTED";

        /// <summary> Yêu cầu đã bị hủy bỏ </summary>
        public const string CANCELLED = "CANCELLED";
    }

    /// <summary>
    /// Phương thức nhận tiền hoàn trả khi hủy thẻ
    /// </summary>
    public static class PayoutMethodEnum
    {
        /// <summary> Chuyển khoản ngân hàng </summary>
        public const string BANK_TRANSFER = "BANK_TRANSFER";

        /// <summary> Nhận tiền mặt trực tiếp tại quầy </summary>
        public const string CASH = "CASH";
    }
}
