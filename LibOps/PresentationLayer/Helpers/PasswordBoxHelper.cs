using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LibOps.PresentationLayer.Helpers
{
    /// <summary>
    /// Lớp tiện ích Attached Property cho PasswordBox giúp hiển thị placeholder và nút ẩn/hiện mật khẩu
    /// </summary>
    public static class PasswordBoxHelper
    {
        // 1. Thuộc tính đính kèm để kích hoạt tính năng hỗ trợ PasswordBox
        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached(
                "Attach",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnAttachChanged));

        // 2. Thuộc tính chỉ đọc để nhận biết ô mật khẩu có đang chứa văn bản hay không
        private static readonly DependencyPropertyKey HasTextPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "HasText",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false));

        public static readonly DependencyProperty HasTextProperty = HasTextPropertyKey.DependencyProperty;

        public static bool GetAttach(DependencyObject obj) => (bool)obj.GetValue(AttachProperty);
        public static void SetAttach(DependencyObject obj, bool value) => obj.SetValue(AttachProperty, value);

        public static bool GetHasText(DependencyObject obj) => (bool)obj.GetValue(HasTextProperty);
        private static void SetHasText(DependencyObject obj, bool value) => obj.SetValue(HasTextPropertyKey, value);

        private static void OnAttachChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                if ((bool)e.NewValue)
                {
                    passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
                    passwordBox.Loaded += PasswordBox_Loaded;
                    if (passwordBox.IsLoaded)
                    {
                        SetupPasswordBox(passwordBox);
                    }
                    UpdateHasText(passwordBox);
                }
                else
                {
                    passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                    passwordBox.Loaded -= PasswordBox_Loaded;
                }
            }
        }

        private static void PasswordBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                SetupPasswordBox(passwordBox);
                UpdateHasText(passwordBox);
            }
        }

        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                UpdateHasText(passwordBox);
            }
        }

        private static void UpdateHasText(PasswordBox passwordBox)
        {
            SetHasText(passwordBox, passwordBox.SecurePassword != null && passwordBox.SecurePassword.Length > 0);
        }

        private static void SetupPasswordBox(PasswordBox passwordBox)
        {
            if (passwordBox.Template == null) return;

            var toggleBtn = passwordBox.Template.FindName("btnToggleShowPassword", passwordBox) as ToggleButton;
            var revealTxt = passwordBox.Template.FindName("revealTextBox", passwordBox) as TextBox;
            var contentHost = passwordBox.Template.FindName("PART_ContentHost", passwordBox) as FrameworkElement;

            if (toggleBtn == null || revealTxt == null) return;

            // Flag to avoid recursive sync loops
            bool isSyncing = false;

            // Sync PasswordBox -> revealTextBox
            RoutedEventHandler onPasswordChanged = (s, e) =>
            {
                if (!isSyncing && revealTxt != null)
                {
                    isSyncing = true;
                    revealTxt.Text = passwordBox.Password;
                    UpdateHasText(passwordBox);
                    isSyncing = false;
                }
            };
            passwordBox.PasswordChanged -= onPasswordChanged;
            passwordBox.PasswordChanged += onPasswordChanged;

            // Sync revealTextBox -> PasswordBox
            TextChangedEventHandler onTextChanged = (s, e) =>
            {
                if (!isSyncing)
                {
                    isSyncing = true;
                    passwordBox.Password = revealTxt.Text;
                    UpdateHasText(passwordBox);
                    isSyncing = false;
                }
            };
            revealTxt.TextChanged -= onTextChanged;
            revealTxt.TextChanged += onTextChanged;

            // Toggle show/hide
            RoutedEventHandler onChecked = (s, e) =>
            {
                if (contentHost != null) contentHost.Visibility = Visibility.Collapsed;
                revealTxt.Visibility = Visibility.Visible;
                if (!isSyncing)
                {
                    isSyncing = true;
                    revealTxt.Text = passwordBox.Password;
                    isSyncing = false;
                }
                revealTxt.Focus();
                revealTxt.CaretIndex = revealTxt.Text.Length;
            };

            RoutedEventHandler onUnchecked = (s, e) =>
            {
                revealTxt.Visibility = Visibility.Collapsed;
                if (contentHost != null) contentHost.Visibility = Visibility.Visible;
                if (!isSyncing)
                {
                    isSyncing = true;
                    passwordBox.Password = revealTxt.Text;
                    isSyncing = false;
                }
                passwordBox.Focus();
            };

            toggleBtn.Checked -= onChecked;
            toggleBtn.Checked += onChecked;
            toggleBtn.Unchecked -= onUnchecked;
            toggleBtn.Unchecked += onUnchecked;

            // Initial state check
            if (toggleBtn.IsChecked == true)
            {
                if (contentHost != null) contentHost.Visibility = Visibility.Collapsed;
                revealTxt.Visibility = Visibility.Visible;
                revealTxt.Text = passwordBox.Password;
            }
            else
            {
                revealTxt.Visibility = Visibility.Collapsed;
                if (contentHost != null) contentHost.Visibility = Visibility.Visible;
            }
        }
    }
}
