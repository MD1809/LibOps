using LibOps.PresentationLayer.ViewModels;

namespace LibOps.PresentationLayer.Services
{
    /// <summary>
    /// Giao diện trừu tượng hóa toàn bộ việc hiển thị MessageBox và Dialog Windows trong ứng dụng MVVM
    /// </summary>
    public interface IDialogService
    {
        // 1. Nhóm thông báo cơ bản (Message / Alert / Confirm)
        void ShowInformation(string message, string title = "Thông Báo");
        void ShowWarning(string message, string title = "Cảnh Báo");
        void ShowError(string message, string title = "Lỗi");
        bool ShowConfirmation(string message, string title = "Xác Nhận");

        // 2. Nhóm cửa sổ chuyên biệt (Domain Dialog Windows)
        bool? ShowStaffDetailDialog(StaffDetailViewModel viewModel);
        bool? ShowBookAddEditDialog(BookAddEditViewModel viewModel);
        bool? ShowBookDetailDialog(BookDetailDialogViewModel viewModel);
        bool? ShowMemberDetailDialog(MemberDetailViewModel viewModel);
        bool? ShowMemberDepositDialog(MemberDepositViewModel viewModel);
        bool? ShowMemberClosureDialog(MemberClosureViewModel viewModel);
        bool? ShowChangePasswordDialog(ChangePasswordViewModel viewModel);
        void ShowBookCopyManagementDialog(BookCopyBarcodeViewModel viewModel);
        void ShowBarcodePreviewDialog(string barcode, object imageSource);
        bool? ShowSelectBorrowingBookDialog(SelectBorrowingBookViewModel viewModel);
        bool? ShowExpiredDebtSettlementDialog(ExpiredDebtViewModel viewModel);
        bool? ShowVietQRQuickPayDialog(VietQRQuickPayViewModel viewModel);
        bool? ShowMemberRenewCardDialog(MemberRenewCardViewModel viewModel);
        bool? ShowMemberReactivateDialog(MemberReactivateViewModel viewModel);
        bool? ShowQuickAddMetadataDialog(QuickAddMetadataViewModel viewModel);
    }
}
