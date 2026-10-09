using System;
using System.Windows;
using System.Windows.Controls;
using LibOps.PresentationLayer.Services;

namespace LibOps.PresentationLayer.Views
{
    public partial class CreateSelfServiceRequestDialog : Window
    {
        public string RequestType { get; private set; } = "REISSUE_CARD";
        public string Reason { get; private set; } = string.Empty;
        public string NewPhone { get; private set; } = string.Empty;
        public string NewEmail { get; private set; } = string.Empty;
        public string NewAddress { get; private set; } = string.Empty;
        public string FeedbackSubject { get; private set; } = string.Empty;
        public string FeedbackContent { get; private set; } = string.Empty;

        public CreateSelfServiceRequestDialog(string currentPhone = "", string currentEmail = "", string currentAddress = "")
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void RequestType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pnlReissueCard == null || pnlUpdateInfo == null || pnlFeedback == null) return;

            if (cboRequestType.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                string tag = item.Tag.ToString();
                RequestType = tag;

                pnlReissueCard.Visibility = tag == "REISSUE_CARD" ? Visibility.Visible : Visibility.Collapsed;
                pnlUpdateInfo.Visibility = tag == "UPDATE_INFO" ? Visibility.Visible : Visibility.Collapsed;
                pnlFeedback.Visibility = tag == "FEEDBACK_INQUIRY" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (RequestType == "REISSUE_CARD")
            {
                Reason = txtReissueReason.Text?.Trim();
                if (string.IsNullOrWhiteSpace(Reason))
                {
                    DialogService.ShowWarning("Vui lòng nhập lý do và thời gian mất thẻ!", "Thiếu Thông Tin");
                    txtReissueReason.Focus();
                    return;
                }
            }
            else if (RequestType == "UPDATE_INFO")
            {
                NewPhone = txtNewPhone.Text?.Trim() ?? string.Empty;
                NewEmail = txtNewEmail.Text?.Trim() ?? string.Empty;
                NewAddress = txtNewAddress.Text?.Trim() ?? string.Empty;
                Reason = txtUpdateReason.Text?.Trim() ?? "Cập nhật thông tin liên hệ mới";

                if (string.IsNullOrWhiteSpace(NewPhone) && string.IsNullOrWhiteSpace(NewEmail) && string.IsNullOrWhiteSpace(NewAddress))
                {
                    DialogService.ShowWarning("Vui lòng nhập ít nhất một thông tin cần thay đổi!", "Thiếu Thông Tin");
                    txtNewPhone.Focus();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(NewPhone) && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidPhoneNumber(NewPhone))
                {
                    DialogService.ShowWarning("Số điện thoại mới không đúng định dạng (10 chữ số)!", "Định Dạng Không Hợp Lệ");
                    txtNewPhone.Focus();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(NewEmail) && !LibOps.CommonUtilities.Validation.InputValidationUtility.IsValidEmail(NewEmail))
                {
                    DialogService.ShowWarning("Địa chỉ Email mới không đúng định dạng!", "Định Dạng Không Hợp Lệ");
                    txtNewEmail.Focus();
                    return;
                }
            }
            else if (RequestType == "FEEDBACK_INQUIRY")
            {
                FeedbackSubject = txtFeedbackSubject.Text?.Trim();
                FeedbackContent = txtFeedbackContent.Text?.Trim();

                if (string.IsNullOrWhiteSpace(FeedbackSubject))
                {
                    DialogService.ShowWarning("Vui lòng nhập Tiêu đề thắc mắc/góp ý!", "Thiếu Thông Tin");
                    txtFeedbackSubject.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(FeedbackContent))
                {
                    DialogService.ShowWarning("Vui lòng nhập Nội dung chi tiết!", "Thiếu Thông Tin");
                    txtFeedbackContent.Focus();
                    return;
                }
            }

            DialogResult = true;
            Close();
        }
    }
}
