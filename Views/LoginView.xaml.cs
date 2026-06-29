using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyTaiChinhCaNhan_Nhom06.ViewModels;

namespace QuanLyTaiChinhCaNhan_Nhom06.Views
{
    public partial class LoginView : UserControl
    {
        private bool _isPasswordVisible;
        private bool _isSyncingPassword;

        public LoginView()
        {
            InitializeComponent();
            RefreshPasswordPlaceholder();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingPassword)
                return;

            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.Password = passwordBox.Password;

                if (_isPasswordVisible && VisiblePasswordInput.Text != passwordBox.Password)
                {
                    _isSyncingPassword = true;
                    VisiblePasswordInput.Text = passwordBox.Password;
                    _isSyncingPassword = false;
                }
            }

            RefreshPasswordPlaceholder();
        }

        private void VisiblePasswordInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPassword)
                return;

            if (DataContext is LoginViewModel viewModel && sender is TextBox textBox)
            {
                viewModel.Password = textBox.Text;

                if (PasswordInput.Password != textBox.Text)
                {
                    _isSyncingPassword = true;
                    PasswordInput.Password = textBox.Text;
                    _isSyncingPassword = false;
                }
            }

            RefreshPasswordPlaceholder();
        }

        private void TogglePasswordVisibility_Click(object sender, MouseButtonEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                _isSyncingPassword = true;
                VisiblePasswordInput.Text = PasswordInput.Password;
                _isSyncingPassword = false;

                PasswordInput.Visibility = Visibility.Collapsed;
                VisiblePasswordInput.Visibility = Visibility.Visible;
                PasswordVisibilityIcon.Opacity = 1;
                VisiblePasswordInput.Focus();
                VisiblePasswordInput.CaretIndex = VisiblePasswordInput.Text.Length;
            }
            else
            {
                _isSyncingPassword = true;
                PasswordInput.Password = VisiblePasswordInput.Text;
                _isSyncingPassword = false;

                VisiblePasswordInput.Visibility = Visibility.Collapsed;
                PasswordInput.Visibility = Visibility.Visible;
                PasswordVisibilityIcon.Opacity = 0.72;
                PasswordInput.Focus();
            }

            RefreshPasswordPlaceholder();
            e.Handled = true;
        }

        private void RefreshPasswordPlaceholder()
        {
            var text = _isPasswordVisible ? VisiblePasswordInput.Text : PasswordInput.Password;
            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}