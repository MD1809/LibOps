using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace LibOps.PresentationLayer.Views.Members
{
    public partial class ExpenseVoucherDialog : Window
    {
        private readonly Bitmap _voucherBitmap;

        public ExpenseVoucherDialog(Bitmap voucherBitmap)
        {
            InitializeComponent();
            _voucherBitmap = voucherBitmap;
            LoadBitmapToImage();
        }

        private void LoadBitmapToImage()
        {
            if (_voucherBitmap == null) return;

            using (var memory = new MemoryStream())
            {
                _voucherBitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                imgVoucher.Source = bitmapImage;
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(imgVoucher, "Phiếu Chi Hoàn Tiền Cọc - LibOps");
                    MessageBox.Show("Lệnh in đã được gửi thành công tới máy in.", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi gửi lệnh in: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_voucherBitmap == null) return;

                var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
                    FileName = $"PhieuChi_HoanCoc_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    _voucherBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                    MessageBox.Show("Đã lưu hình ảnh Phiếu Chi thành công!", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu ảnh: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
