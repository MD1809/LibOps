using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Services
{
    /// <summary>
    /// Provider cung cấp thể hiện IDialogService mặc định cho toàn bộ ứng dụng và các phương thức static tiện ích
    /// </summary>
    public static class DialogService
    {
        private static IDialogService _current;

        public static IDialogService Current
        {
            get => _current ?? (_current = new WpfDialogService());
            set => _current = value;
        }

        // 1. Nhóm thông báo cơ bản
        public static void ShowInformation(string message, string title = "Thông Báo") => Current.ShowInformation(message, title);
        public static void ShowWarning(string message, string title = "Cảnh Báo") => Current.ShowWarning(message, title);
        public static void ShowError(string message, string title = "Lỗi") => Current.ShowError(message, title);
        public static bool ShowConfirmation(string message, string title = "Xác Nhận") => Current.ShowConfirmation(message, title);

        // 2. Nhóm cửa sổ chuyên biệt (Domain Dialog Windows)
        public static bool? ShowStaffDetailDialog(StaffDetailViewModel viewModel) => Current.ShowStaffDetailDialog(viewModel);
        public static bool? ShowBookAddEditDialog(BookAddEditViewModel viewModel) => Current.ShowBookAddEditDialog(viewModel);
        public static bool? ShowBookDetailDialog(BookDetailDialogViewModel viewModel) => Current.ShowBookDetailDialog(viewModel);
        public static bool? ShowMemberDetailDialog(MemberDetailViewModel viewModel) => Current.ShowMemberDetailDialog(viewModel);
        public static bool? ShowMemberDepositDialog(MemberDepositViewModel viewModel) => Current.ShowMemberDepositDialog(viewModel);
        public static bool? ShowMemberClosureDialog(MemberClosureViewModel viewModel) => Current.ShowMemberClosureDialog(viewModel);
        public static bool? ShowChangePasswordDialog(ChangePasswordViewModel viewModel) => Current.ShowChangePasswordDialog(viewModel);
        public static void ShowBookCopyManagementDialog(BookCopyBarcodeViewModel viewModel) => Current.ShowBookCopyManagementDialog(viewModel);
        public static void ShowBarcodePreviewDialog(string barcode, object imageSource) => Current.ShowBarcodePreviewDialog(barcode, imageSource);
        public static bool? ShowSelectBorrowingBookDialog(SelectBorrowingBookViewModel viewModel) => Current.ShowSelectBorrowingBookDialog(viewModel);
        public static bool? ShowExpiredDebtSettlementDialog(ExpiredDebtViewModel viewModel) => Current.ShowExpiredDebtSettlementDialog(viewModel);
        public static bool? ShowVietQRQuickPayDialog(VietQRQuickPayViewModel viewModel) => Current.ShowVietQRQuickPayDialog(viewModel);
        public static bool? ShowMemberRenewCardDialog(MemberRenewCardViewModel viewModel) => Current.ShowMemberRenewCardDialog(viewModel);
        public static bool? ShowMemberReactivateDialog(MemberReactivateViewModel viewModel) => Current.ShowMemberReactivateDialog(viewModel);
        public static bool? ShowQuickAddMetadataDialog(QuickAddMetadataViewModel viewModel) => Current.ShowQuickAddMetadataDialog(viewModel);
    }
}
