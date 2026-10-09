using System;
using System.Windows;
using System.Windows.Media;
using LibOps.DataModels.DataTransferObjects;

namespace LibOps.PresentationLayer.Views
{
    public partial class ReaderRequestDetailDialog : Window
    {
        private readonly ReaderRequestDisplayDto _request;

        public ReaderRequestDetailDialog(ReaderRequestDisplayDto request)
        {
            InitializeComponent();
            _request = request ?? throw new ArgumentNullException(nameof(request));

            LoadData();
        }

        private void LoadData()
        {
            txtRequestCode.Text = $"Mã YC: #{_request.RequestCode}";
            txtRequestDate.Text = $"Ngày gửi: {_request.FormattedRequestDate}";
            txtRequestType.Text = _request.RequestTypeDisplay;
            txtStatus.Text = _request.StatusDisplay;

            // Xử lý Badge trạng thái
            bool isPending = string.Equals(_request.Status, "PENDING", StringComparison.OrdinalIgnoreCase);
            bool isApproved = string.Equals(_request.Status, "APPROVED", StringComparison.OrdinalIgnoreCase);

            if (isPending)
            {
                badgeStatus.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199)); // Vàng nhạt
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                pnlPendingNotice.Visibility = Visibility.Visible;
                pnlProcessedResult.Visibility = Visibility.Collapsed;
            }
            else
            {
                pnlPendingNotice.Visibility = Visibility.Collapsed;
                pnlProcessedResult.Visibility = Visibility.Visible;

                if (isApproved)
                {
                    badgeStatus.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)); // Xanh lá
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
                }
                else
                {
                    badgeStatus.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)); // Đỏ
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                }

                string staff = string.IsNullOrWhiteSpace(_request.ProcessedByStaffName) ? "Thủ thư Thư viện" : _request.ProcessedByStaffName;
                string date = _request.ProcessedDate.HasValue ? _request.ProcessedDate.Value.ToString("dd/MM/yyyy HH:mm") : "---";
                txtProcessedInfo.Text = $"Đã xử lý bởi: {staff} • Ngày: {date}";
                txtStaffNotesFull.Text = string.IsNullOrWhiteSpace(_request.StaffNotes) ? "Không có ghi chú thêm." : _request.StaffNotes;
            }

            // Xử lý loại yêu cầu HỦY THẺ & HOÀN CỌC
            if (string.Equals(_request.RequestType, "CANCEL_CARD", StringComparison.OrdinalIgnoreCase))
            {
                pnlCancelCard.Visibility = Visibility.Visible;
                txtRefundAmount.Text = $"{_request.Amount:N0} đ";

                bool isCash = string.Equals(_request.PayoutMethod, "CASH", StringComparison.OrdinalIgnoreCase);
                if (isCash)
                {
                    txtPayoutMethod.Text = "Hình thức: Tiền mặt tại quầy";
                    pnlBankTransferInfo.Visibility = Visibility.Collapsed;
                    pnlCashInfo.Visibility = Visibility.Visible;
                }
                else
                {
                    txtPayoutMethod.Text = "Hình thức: Chuyển khoản ngân hàng";
                    pnlBankTransferInfo.Visibility = Visibility.Visible;
                    pnlCashInfo.Visibility = Visibility.Collapsed;

                    string bankName = string.IsNullOrWhiteSpace(_request.BankName) ? "Ngân hàng TMCP" : _request.BankName;
                    string accountNo = string.IsNullOrWhiteSpace(_request.BankAccountNumber) ? "Chưa cập nhật" : _request.BankAccountNumber;
                    string accountHolder = string.IsNullOrWhiteSpace(_request.BankAccountHolder) ? _request.MemberFullName : _request.BankAccountHolder;

                    txtBankName.Text = $"Ngân hàng: {bankName}";
                    txtAccountNo.Text = $"Số tài khoản: {accountNo}";
                    txtAccountHolder.Text = $"Chủ tài khoản: {accountHolder.ToUpperInvariant()}";
                }
            }
            else
            {
                pnlCancelCard.Visibility = Visibility.Collapsed;
            }

            // Toàn văn nội dung độc giả đã gửi
            txtReasonFull.Text = string.IsNullOrWhiteSpace(_request.Reason) ? "(Không có nội dung)" : _request.Reason;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
