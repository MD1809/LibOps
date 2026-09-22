using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using LibOps.BusinessLogicLayer.BusinessServices;
using LibOps.BusinessLogicLayer.BusinessServices.Payment;
using LibOps.CommonUtilities.Payment;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.MvvmCore;

namespace LibOps.PresentationLayer.ViewModels
{
    public class VietQRQuickPayViewModel : ViewModelBase
    {
        private readonly IPaymentGatewayService _gatewayService;
        private readonly MemberService _memberService;
        private readonly ReaderPortalService _readerPortalService;

        private PaymentOrderDto _paymentOrder;
        private ImageSource _qrImage;
        private bool _isLoading = true;
        private bool _isSuccess;
        private bool _isExpired;
        private int _countdownSeconds = 300; // 5 phút
        private string _countdownText = "05:00";
        private string _statusMessage = "📡 Đang khởi tạo mã VietQR từ Cổng PayOS...";

        private DispatcherTimer _countdownTimer;
        private CancellationTokenSource _cts;

        public PaymentOrderDto PaymentOrder
        {
            get => _paymentOrder;
            set => SetProperty(ref _paymentOrder, value);
        }

        public ImageSource QrImage
        {
            get => _qrImage;
            set => SetProperty(ref _qrImage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        public bool IsExpired
        {
            get => _isExpired;
            set => SetProperty(ref _isExpired, value);
        }

        public int CountdownSeconds
        {
            get => _countdownSeconds;
            set => SetProperty(ref _countdownSeconds, value);
        }

        public string CountdownText
        {
            get => _countdownText;
            set => SetProperty(ref _countdownText, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand CancelCommand { get; }
        public ICommand SimulateSuccessCommand { get; }

        public event Action<bool?> RequestClose;

        public VietQRQuickPayViewModel(
            int memberId, 
            string memberCardCode, 
            string memberFullName, 
            decimal amount, 
            string actionType = "TOP_UP", 
            string customNote = null)
        {
            _gatewayService = new PayOSPaymentService();
            _memberService = new MemberService();
            _readerPortalService = new ReaderPortalService();

            CancelCommand = new RelayCommand(ExecuteCancel);
            SimulateSuccessCommand = new RelayCommand(ExecuteSimulateSuccess);

            // Khởi tạo và nạp mã QR
            InitializePaymentAsync(memberId, memberCardCode, memberFullName, amount, actionType, customNote);
        }

        private async void InitializePaymentAsync(
            int memberId, 
            string memberCardCode, 
            string memberFullName, 
            decimal amount, 
            string actionType, 
            string customNote)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "📡 Đang kết nối Cổng VietQR PayOS...";

                // Gọi SDK PayOS tạo đơn hàng
                PaymentOrder = await _gatewayService.CreatePaymentOrderAsync(
                    memberId, memberCardCode, memberFullName, amount, actionType, customNote);

                // Sinh ảnh QR Code từ chuỗi VietQR chuẩn EMVCo
                QrImage = VietQRGeneratorUtility.GenerateQrBitmapSource(PaymentOrder.QrCodeText, 8);

                IsLoading = false;
                StatusMessage = "📡 Đang chờ bạn quét mã và chuyển khoản...";

                // Khởi động bộ đếm ngược 5 phút
                StartCountdownTimer();

                // Bắt đầu vòng lặp Polling kiểm tra ngầm mỗi 2.5 giây
                _cts = new CancellationTokenSource();
                _ = PollPaymentStatusLoopAsync(_cts.Token);
            }
            catch (Exception)
            {
                IsLoading = false;
                
                // Fallback nếu Cổng PayOS trả về lỗi/chưa liên kết ngân hàng: Sinh mã VietQR chuẩn EMVCo NAPAS trực tiếp
                string actionPrefix = actionType == "RENEW" ? "GIAHAN" : (actionType == "FINE_PAYMENT" ? "PHAT" : "NAPTIEN");
                string transferContent = $"{memberCardCode.Trim().ToUpper()} {actionPrefix}";
                string fallbackEmvCoPayload = VietQRGeneratorUtility.BuildVietQrEmvCoPayload(
                    VietQRPaymentSimulatorUtility.DEFAULT_BANK_CODE, 
                    VietQRPaymentSimulatorUtility.DEFAULT_ACCOUNT_NUMBER, 
                    amount, 
                    transferContent);

                QrImage = VietQRGeneratorUtility.GenerateQrBitmapSource(fallbackEmvCoPayload, 8);

                StatusMessage = "📡 Chế độ VietQR Chuẩn NAPAS (Hỗ trợ quét qua tất cả App Ngân Hàng)";

                PaymentOrder = new PaymentOrderDto
                {
                    OrderCode = long.Parse(DateTime.Now.ToString("yyMMddHHmmss")),
                    MemberId = memberId,
                    MemberCardCode = memberCardCode,
                    MemberFullName = memberFullName,
                    Amount = amount,
                    Description = transferContent,
                    QrCodeText = fallbackEmvCoPayload,
                    AccountNumber = VietQRPaymentSimulatorUtility.DEFAULT_ACCOUNT_NUMBER,
                    AccountName = VietQRPaymentSimulatorUtility.DEFAULT_ACCOUNT_HOLDER,
                    BankName = VietQRPaymentSimulatorUtility.DEFAULT_BANK_NAME,
                    ActionType = actionType
                };
                StartCountdownTimer();
            }
        }

        private void StartCountdownTimer()
        {
            CountdownSeconds = 300;
            UpdateCountdownText();

            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _countdownTimer.Tick += (s, e) =>
            {
                CountdownSeconds--;
                UpdateCountdownText();

                if (CountdownSeconds <= 0)
                {
                    _countdownTimer.Stop();
                    _cts?.Cancel();
                    IsExpired = true;
                    StatusMessage = "Giao dịch đã hết hạn (5 phút). Vui lòng thực hiện lại!";
                }
            };
            _countdownTimer.Start();
        }

        private void UpdateCountdownText()
        {
            int min = CountdownSeconds / 60;
            int sec = CountdownSeconds % 60;
            CountdownText = $"{min:D2}:{sec:D2}";
        }

        private async Task PollPaymentStatusLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && CountdownSeconds > 0 && !IsSuccess)
            {
                try
                {
                    await Task.Delay(2500, token);
                    if (token.IsCancellationRequested) break;

                    string status = await _gatewayService.CheckPaymentStatusAsync(PaymentOrder.OrderCode);

                    if (status == "PAID")
                    {
                        await Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            await ProcessSuccessfulPaymentAsync();
                        });
                        break;
                    }
                }
                catch
                {
                    // Tiếp tục chờ nếu mạng chập chờn
                }
            }
        }

        private async Task ProcessSuccessfulPaymentAsync()
        {
            if (IsSuccess) return;

            _countdownTimer?.Stop();
            _cts?.Cancel();

            IsSuccess = true;
            StatusMessage = "🎉 ĐÃ NHẬN TIỀN THÀNH CÔNG! Đang cập nhật số dư...";

            int userId = AuthService.CurrentSession?.UserId ?? 1;

            if (PaymentOrder.ActionType == "RENEW")
            {
                // Gia hạn thẻ
                _memberService.RenewMemberCard(PaymentOrder.MemberId, userId, out _, out _, out _);
            }
            else
            {
                // Nạp tiền cọc (Thuật toán thác nước)
                _memberService.TopUpDeposit(
                    PaymentOrder.MemberId, 
                    PaymentOrder.Amount, 
                    userId, 
                    $"Nạp tiền qua Cổng VietQR PayOS (Mã GD: #{PaymentOrder.OrderCode})", 
                    out _);
            }

            // Đợi 1.5 giây để người dùng thấy hiệu ứng thành công màu xanh trước khi đóng
            await Task.Delay(1500);
            RequestClose?.Invoke(true);
        }

        private void ExecuteSimulateSuccess()
        {
            _ = ProcessSuccessfulPaymentAsync();
        }

        private void ExecuteCancel()
        {
            _countdownTimer?.Stop();
            _cts?.Cancel();
            RequestClose?.Invoke(false);
        }
    }
}
