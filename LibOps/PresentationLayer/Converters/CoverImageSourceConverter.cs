using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace LibOps.PresentationLayer.Converters
{
    /// <summary>
    /// Chuyển đổi đường dẫn ảnh (URL online, đường dẫn tuyệt đối hoặc đường dẫn tương đối Assets/BookCovers/...)
    /// thành BitmapImage chuẩn cho WPF Image và ImageBrush.
    /// </summary>
    public class CoverImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            string path = value.ToString().Trim();
            if (string.IsNullOrWhiteSpace(path)) return null;

            try
            {
                // 1. Web URL (http:// hoặc https://)
                if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(path, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    return bmp;
                }

                // 2. Đường dẫn tuyệt đối đã tồn tại trên ổ đĩa
                if (Path.IsPathRooted(path) && File.Exists(path))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(path, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    return bmp;
                }

                // 3. Đường dẫn tương đối từ BaseDirectory (Assets/BookCovers/... hoặc bookCovers/...)
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = Path.Combine(baseDir, path.Replace('/', '\\'));
                if (File.Exists(fullPath))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    return bmp;
                }

                // 4. Fallback tìm trong Assets/BookCovers/<filename>
                string fileName = Path.GetFileName(path);
                string assetFallback = Path.Combine(baseDir, "Assets", "BookCovers", fileName);
                if (File.Exists(assetFallback))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(assetFallback, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    return bmp;
                }

                // 5. Fallback Pack URI
                string packUri = $"pack://application:,,,/{path.TrimStart('/')}";
                var packBmp = new BitmapImage();
                packBmp.BeginInit();
                packBmp.UriSource = new Uri(packUri, UriKind.Absolute);
                packBmp.CacheOption = BitmapCacheOption.OnLoad;
                packBmp.EndInit();
                return packBmp;
            }
            catch
            {
                // Bỏ qua lỗi và trả về null để giao diện tự kích hoạt placeholder
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
