using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibOps.PresentationLayer.ViewModels;
using LibOps.PresentationLayer.Views;
using LibOps.PresentationLayer.Views.Members;

namespace LibOps.PresentationLayer.Services
{
    /// <summary>
    /// Triển khai IDialogService thực tế trên môi trường WPF
    /// </summary>
    public class WpfDialogService : IDialogService
    {
        private Window GetActiveWindow()
        {
            if (Application.Current == null) return null;

            // Ưu tiên cửa sổ đang được active, sau đó đến MainWindow
            foreach (Window win in Application.Current.Windows)
            {
                if (win.IsActive) return win;
            }
            return Application.Current.MainWindow;
        }

        public void ShowInformation(string message, string title = "Thông Báo")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowWarning(string message, string title = "Cảnh Báo")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowError(string message, string title = "Lỗi")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool ShowConfirmation(string message, string title = "Xác Nhận")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public bool? ShowStaffDetailDialog(StaffDetailViewModel viewModel)
        {
            var dialog = new StaffDetailDialog(viewModel);
            SetOwner(dialog);
            return dialog.ShowDialog();
        }

        public bool? ShowBookAddEditDialog(BookAddEditViewModel viewModel)
        {
            var dialog = new BookAddEditDialog(viewModel);
            SetOwner(dialog);
            return dialog.ShowDialog();
        }

        public bool? ShowBookDetailDialog(BookDetailDialogViewModel viewModel)
        {
            var dialog = new BookDetailDialog(viewModel);
            SetOwner(dialog);
            return dialog.ShowDialog();
        }

        public bool? ShowMemberDetailDialog(MemberDetailViewModel viewModel)
        {
            var dialog = new MemberDetailDialog(viewModel);
            SetOwner(dialog);
            return dialog.ShowDialog();
        }

        public bool? ShowMemberDepositDialog(MemberDepositViewModel viewModel)
        {
            var dialog = new MemberDepositDialog(viewModel);
            SetOwner(dialog);
            return dialog.ShowDialog();
        }

        public bool? ShowMemberClosureDialog(MemberClosureViewModel viewModel)
        {
            var dialog = new MemberClosureDialog(viewModel);
            SetOwner(dialog);
            return dialog.ShowDialog();
        }

        public bool? ShowChangePasswordDialog(ChangePasswordViewModel viewModel)
        {
            var dialog = new ChangePasswordDialog(viewModel);
            SetOwner(dialog);
            return dialog.ShowDialog();
        }

        public void ShowBookCopyManagementDialog(BookCopyBarcodeViewModel viewModel)
        {
            var window = new Window
            {
                Title = "QUẢN LÝ BẢN SAO SÁCH & MÃ VẠCH (BARCODE)",
                Width = 980,
                Height = 650,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new BookCopyBarcodeView { DataContext = viewModel }
            };
            SetOwner(window);
            window.ShowDialog();
        }

        public void ShowBarcodePreviewDialog(string barcode, object imageSource)
        {
            var preview = new Window
            {
                Title = $"Xem trước Mã Vạch [{barcode}]",
                Width = 420,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new Image
                        {
                            Source = imageSource as ImageSource,
                            Stretch = Stretch.Uniform,
                            MaxHeight = 150
                        },
                        new TextBlock
                        {
                            Text = $"Mã vạch: {barcode} | Chuẩn Code128",
                            FontWeight = FontWeights.Bold,
                            FontSize = 12,
                            Margin = new Thickness(0, 10, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Center
                        }
                    }
                }
            };
            SetOwner(preview);
            preview.ShowDialog();
        }

        public bool? ShowSelectBorrowingBookDialog(SelectBorrowingBookViewModel viewModel)
        {
            var dialog = new SelectBorrowingBookDialog(viewModel);
            SetOwner(dialog);
            return dialog.ShowDialog();
        }

        public bool? ShowExpiredDebtSettlementDialog(ExpiredDebtViewModel viewModel)
        {
            var dialog = new ExpiredDebtDialog(viewModel);
            SetOwner(dialog);
            return dialog.ShowDialog();
        }

        public bool? ShowVietQRQuickPayDialog(VietQRQuickPayViewModel viewModel)
        {
            var dialog = new LibOps.PresentationLayer.Views.Payment.VietQRQuickPayDialog(viewModel);
            SetOwner(dialog);
            return dialog.ShowDialog();
        }

        public bool? ShowMemberRenewCardDialog(MemberRenewCardViewModel viewModel)
        {
            var dialog = new MemberRenewCardDialog(viewModel);
            SetOwner(dialog);
            return dialog.ShowDialog();
        }

        public bool? ShowMemberReactivateDialog(MemberReactivateViewModel viewModel)
        {
            var dialog = new MemberReactivateDialog(viewModel);
            SetOwner(dialog);
            return dialog.ShowDialog();
        }

        public bool? ShowQuickAddMetadataDialog(QuickAddMetadataViewModel viewModel)
        {
            var dialog = new QuickAddMetadataDialog { DataContext = viewModel };
            SetOwner(dialog);
            return dialog.ShowDialog();
        }

        private void SetOwner(Window window)
        {
            var owner = GetActiveWindow();
            if (owner != null && owner != window)
            {
                window.Owner = owner;
            }
        }
    }
}
