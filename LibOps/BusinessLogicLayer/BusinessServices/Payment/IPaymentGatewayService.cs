using System.Threading.Tasks;
using LibOps.DataModels.DataTransferObjects;

namespace LibOps.BusinessLogicLayer.BusinessServices.Payment
{
    /// <summary>
    /// Giao diện chuẩn cho các cổng thanh toán ngân hàng (PayOS, SePay, Mock Gateway)
    /// </summary>
    public interface IPaymentGatewayService
    {
        /// <summary>
        /// Tạo đơn thanh toán trực tuyến và lấy chuỗi VietQR
        /// </summary>
        Task<PaymentOrderDto> CreatePaymentOrderAsync(int memberId, string memberCardCode, string memberFullName, decimal amount, string actionType, string customNote = null);

        /// <summary>
        /// Kiểm tra trạng thái đơn thanh toán (Polling từ Cổng PayOS)
        /// </summary>
        Task<string> CheckPaymentStatusAsync(long orderCode);

        /// <summary>
        /// Hủy đơn thanh toán khi hết hạn hoặc người dùng đóng form
        /// </summary>
        Task<bool> CancelPaymentOrderAsync(long orderCode, string cancellationReason = "Người dùng hủy giao dịch");
    }
}
