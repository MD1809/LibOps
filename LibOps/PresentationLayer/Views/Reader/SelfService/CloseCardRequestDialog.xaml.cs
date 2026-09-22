using System;
using System.Windows;
using LibOps.CommonUtilities.Payment;
using LibOps.PresentationLayer.Services;

namespace LibOps.PresentationLayer.Views
{
    public partial class CloseCardRequestDialog : Window
    {
        public string PayoutMethod { get; private set; } = "BANK_TRANSFER";
        public string BankName { get; private set; } = string.Empty;
        public string BankAccountNumber { get; private set; } = string.Empty;
        public string BankAccountHolder { get; private set; } = string.Empty;
        public string Reason { get; private set; } = string.Empty;

        public CloseCardRequestDialog(decimal depositBalance, string defaultHolderName = "")
        {
            InitializeComponent();
            txtRefundAmount.Text = $"{depositBalance:N0} đ";

            // Nạp danh sách ngân hàng
            var banks = VietQRPaymentSimulatorUtility.GetPopularBankList();
            cboBank.ItemsSource = banks;
            if (banks.Count > 0)
            {
                cboBank.SelectedIndex = 0;
            }

            if (!string.IsNullOrWhiteSpace(defaultHolderName))
            {
                txtAccountHolder.Text = defaultHolderName.Trim().ToUpperInvariant();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void PaymentMethod_Changed(object sender, RoutedEventArgs e)
        {
            if (pnlBankDetails == null || pnlCashNotice == null) return;

            if (rbBankTransfer.IsChecked == true)
            {
                pnlBankDetails.Visibility = Visibility.Visible;
                pnlCashNotice.Visibility = Visibility.Collapsed;
            }
            else
            {
                pnlBankDetails.Visibility = Visibility.Collapsed;
                pnlCashNotice.Visibility = Visibility.Visible;
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (rbBankTransfer.IsChecked == true)
            {
                PayoutMethod = "BANK_TRANSFER";
                if (cboBank.SelectedItem is BankInfoItem selectedBank)
                {
                    BankName = selectedBank.Name;
                }
                else
                {
                    DialogService.ShowWarning("Vui lòng chọn Ngân hàng thụ hưởng!", "Thiếu Thông Tin");
                    cboBank.Focus();
                    return;
                }

                BankAccountNumber = txtAccountNumber.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(BankAccountNumber))
                {
                    DialogService.ShowWarning("Vui lòng nhập Số tài khoản ngân hàng thụ hưởng!", "Thiếu Thông Tin");
                    txtAccountNumber.Focus();
                    return;
                }

                BankAccountHolder = txtAccountHolder.Text?.Trim().ToUpperInvariant() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(BankAccountHolder))
                {
                    DialogService.ShowWarning("Vui lòng nhập Tên chủ tài khoản thụ hưởng!", "Thiếu Thông Tin");
                    txtAccountHolder.Focus();
                    return;
                }
            }
            else
            {
                PayoutMethod = "CASH";
                BankName = "Tiền mặt tại quầy";
                BankAccountNumber = string.Empty;
                BankAccountHolder = string.Empty;
            }

            Reason = string.IsNullOrWhiteSpace(txtReason.Text) 
                ? "Độc giả yêu cầu hủy thẻ và nhận lại tiền cọc" 
                : txtReason.Text.Trim();

            DialogResult = true;
            Close();
        }
    }
}
