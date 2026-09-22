using System;
using System.Windows;
using System.Windows.Media;
using LibOps.DataModels.DataTransferObjects;
using LibOps.PresentationLayer.Services;

namespace LibOps.PresentationLayer.Views
{
    public partial class RequestDetailDialog : Window
    {
        private readonly ReaderRequestDisplayDto _request;

        public bool IsApproved { get; private set; }
        public bool IsRejected { get; private set; }
        public string StaffNotes { get; private set; } = string.Empty;

        public RequestDetailDialog(ReaderRequestDisplayDto request)
        {
            InitializeComponent();
            _request = request ?? throw new ArgumentNullException(nameof(request));

            LoadRequestData();
        }

        private void LoadRequestData()
        {
            txtMemberName.Text = $"Độc giả: {_request.MemberFullName} ({_request.MemberCardCode})";
            txtRequestCodeAndDate.Text = $"Mã YC: #{_request.RequestCode} • Gửi lúc: {_request.FormattedRequestDate}";
            txtRequestType.Text = _request.RequestTypeDisplay;
            txtStatus.Text = _request.StatusDisplay;

            bool isPending = string.Equals(_request.Status, "PENDING", StringComparison.OrdinalIgnoreCase);

            // Cập nhật giao diện theo trạng thái PENDING hay Đã xử lý
            if (isPending)
            {
                badgeStatus.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199)); // Vàng
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                btnApprove.Visibility = Visibility.Visible;
                btnReject.Visibility = Visibility.Visible;
                txtStaffNotes.IsReadOnly = false;
                pnlProcessedHistory.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnApprove.Visibility = Visibility.Collapsed;
                btnReject.Visibility = Visibility.Collapsed;
                txtStaffNotes.IsReadOnly = true;
                txtStaffNotes.Text = string.IsNullOrWhiteSpace(_request.StaffNotes) ? "Không có ghi chú" : _request.StaffNotes;

                if (string.Equals(_request.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                {
                    badgeStatus.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)); // Xanh lá
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
                }
                else
                {
                    badgeStatus.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)); // Đỏ
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                }

                pnlProcessedHistory.Visibility = Visibility.Visible;
                string staff = string.IsNullOrWhiteSpace(_request.ProcessedByStaffName) ? "Thủ thư" : _request.ProcessedByStaffName;
                string date = _request.ProcessedDate.HasValue ? _request.ProcessedDate.Value.ToString("dd/MM/yyyy HH:mm") : "---";
                txtProcessedByInfo.Text = $"Đã xử lý bởi: {staff} • Ngày xử lý: {date}";
            }

            // Hiển thị khung nội dung chi tiết theo từng loại yêu cầu
            if (_request.RequestType == LibOps.DataModels.Enums.ReaderRequestTypeEnum.CANCEL_CARD)
            {
                pnlCancelCard.Visibility = Visibility.Visible;
                txtRefundAmount.Text = $"{_request.Amount:N0} đ";

                bool isCash = string.Equals(_request.PayoutMethod, "CASH", StringComparison.OrdinalIgnoreCase);
                if (isCash)
                {
                    txtPayoutMethodSummary.Text = "Hình thức: Tiền mặt tại quầy thư viện";
                    pnlBankTransferInfo.Visibility = Visibility.Collapsed;
                    pnlCashInfo.Visibility = Visibility.Visible;
                    txtApproveBtnText.Text = "Chi Tiền Mặt & Phê Duyệt";
                }
                else
                {
                    txtPayoutMethodSummary.Text = "Hình thức: Chuyển khoản ngân hàng";
                    pnlBankTransferInfo.Visibility = Visibility.Visible;
                    pnlCashInfo.Visibility = Visibility.Collapsed;
                    txtApproveBtnText.Text = "Đã Chuyển Khoản & Phê Duyệt";

                    string bankName = string.IsNullOrWhiteSpace(_request.BankName) ? "Ngân hàng TMCP" : _request.BankName;
                    string accountNo = string.IsNullOrWhiteSpace(_request.BankAccountNumber) ? "Chưa cập nhật" : _request.BankAccountNumber;
                    string accountHolder = string.IsNullOrWhiteSpace(_request.BankAccountHolder) ? _request.MemberFullName : _request.BankAccountHolder;

                    txtBankName.Text = $"Ngân hàng: {bankName}";
                    txtAccountNo.Text = $"Số TK: {accountNo}";
                    txtAccountHolder.Text = $"Chủ TK: {accountHolder.ToUpperInvariant()}";
                    txtTransferContent.Text = $"HOAN COC THE {_request.MemberCardCode}";
                }

                if (isPending)
                {
                    txtStaffNotes.Text = "Đã kiểm tra đủ điều kiện và hoàn trả tiền cọc thành công";
                }
            }
            else if (_request.RequestType == "REISSUE_CARD")
            {
                pnlReissueCardInfo.Visibility = Visibility.Visible;
                txtReissueReason.Text = string.IsNullOrWhiteSpace(_request.Reason) ? "Báo mất thẻ thư viện" : _request.Reason;
                txtApproveBtnText.Text = "Mở Khóa / Cấp Thẻ & Duyệt";

                if (isPending)
                {
                    txtStaffNotes.Text = "Đã tiếp nhận báo mất thẻ. Thẻ đã được kích hoạt lại (hoặc cấp phôi mới), mời độc giả đến quầy nhận thẻ.";
                }
            }
            else if (_request.RequestType == "UPDATE_INFO")
            {
                pnlUpdateInfoSection.Visibility = Visibility.Visible;
                txtUpdateDetails.Text = string.IsNullOrWhiteSpace(_request.Reason) ? "Không có thông tin chi tiết" : _request.Reason;
                txtApproveBtnText.Text = "Duyệt Cập Nhật Hồ Sơ";

                if (isPending)
                {
                    txtStaffNotes.Text = "Đã kiểm tra và cập nhật thông tin hồ sơ độc giả thành công.";
                }
            }
            else if (_request.RequestType == "FEEDBACK_INQUIRY")
            {
                pnlFeedbackSection.Visibility = Visibility.Visible;
                txtFeedbackDetails.Text = string.IsNullOrWhiteSpace(_request.Reason) ? "Không có nội dung" : _request.Reason;
                txtApproveBtnText.Text = "Phản Hồi & Phê Duyệt";

                if (isPending)
                {
                    txtStaffNotes.Text = "Cảm ơn bạn đã đóng góp ý kiến. Ban quản lý thư viện đã ghi nhận và phản hồi như sau: ";
                }
            }
        }

        private void CopyAccount_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_request.BankAccountNumber))
            {
                try
                {
                    Clipboard.SetText(_request.BankAccountNumber);
                    DialogService.ShowInformation($"Đã sao chép Số tài khoản: {_request.BankAccountNumber}", "Đã Sao Chép");
                }
                catch
                {
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            StaffNotes = txtStaffNotes.Text?.Trim();
            if (string.IsNullOrWhiteSpace(StaffNotes))
            {
                DialogService.ShowWarning("Vui lòng nhập lý do từ chối vào ô phản hồi để độc giả được biết!", "Thiếu Lý Do");
                txtStaffNotes.Focus();
                return;
            }

            if (!DialogService.ShowConfirmation($"Bạn có chắc chắn muốn TỪ CHỐI yêu cầu #{_request.RequestCode}?", "Xác Nhận Từ Chối"))
            {
                return;
            }

            IsRejected = true;
            DialogResult = true;
            Close();
        }

        private void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            StaffNotes = txtStaffNotes.Text?.Trim();
            if (string.IsNullOrWhiteSpace(StaffNotes))
            {
                DialogService.ShowWarning("Vui lòng nhập ý kiến xử lý / phản hồi của thủ thư!", "Thiếu Thông Tin");
                txtStaffNotes.Focus();
                return;
            }

            IsApproved = true;
            DialogResult = true;
            Close();
        }
    }
}
