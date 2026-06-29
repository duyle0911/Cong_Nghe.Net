using System.Windows;
using System.Windows.Controls;
using QuanLyTaiChinhCaNhan_Nhom06.ViewModels;

namespace QuanLyTaiChinhCaNhan_Nhom06.Views
{
    public partial class RegisterView : UserControl
    {
        private bool _isPasswordVisible;
        private bool _isConfirmPasswordVisible;
        private bool _isSyncingPassword;
        private bool _isSyncingConfirmPassword;

        public RegisterView()
        {
            InitializeComponent();
            RefreshPasswordPlaceholders();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingPassword)
                return;

            if (DataContext is RegisterViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.Password = passwordBox.Password;

                if (_isPasswordVisible && VisiblePasswordInput.Text != passwordBox.Password)
                {
                    _isSyncingPassword = true;
                    VisiblePasswordInput.Text = passwordBox.Password;
                    _isSyncingPassword = false;
                }
            }

            RefreshPasswordPlaceholders();
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingConfirmPassword)
                return;

            if (DataContext is RegisterViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.ConfirmPassword = passwordBox.Password;

                if (_isConfirmPasswordVisible && VisibleConfirmPasswordInput.Text != passwordBox.Password)
                {
                    _isSyncingConfirmPassword = true;
                    VisibleConfirmPasswordInput.Text = passwordBox.Password;
                    _isSyncingConfirmPassword = false;
                }
            }

            RefreshPasswordPlaceholders();
        }

        private void VisiblePasswordInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPassword)
                return;

            if (DataContext is RegisterViewModel viewModel && sender is TextBox textBox)
            {
                viewModel.Password = textBox.Text;

                if (PasswordInput.Password != textBox.Text)
                {
                    _isSyncingPassword = true;
                    PasswordInput.Password = textBox.Text;
                    _isSyncingPassword = false;
                }
            }

            RefreshPasswordPlaceholders();
        }

        private void VisibleConfirmPasswordInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingConfirmPassword)
                return;

            if (DataContext is RegisterViewModel viewModel && sender is TextBox textBox)
            {
                viewModel.ConfirmPassword = textBox.Text;

                if (ConfirmPasswordInput.Password != textBox.Text)
                {
                    _isSyncingConfirmPassword = true;
                    ConfirmPasswordInput.Password = textBox.Text;
                    _isSyncingConfirmPassword = false;
                }
            }

            RefreshPasswordPlaceholders();
        }

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                _isSyncingPassword = true;
                VisiblePasswordInput.Text = PasswordInput.Password;
                _isSyncingPassword = false;

                PasswordInput.Visibility = Visibility.Collapsed;
                VisiblePasswordInput.Visibility = Visibility.Visible;
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
                PasswordInput.Focus();
            }

            RefreshPasswordPlaceholders();
        }

        private void ToggleConfirmPasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isConfirmPasswordVisible = !_isConfirmPasswordVisible;

            if (_isConfirmPasswordVisible)
            {
                _isSyncingConfirmPassword = true;
                VisibleConfirmPasswordInput.Text = ConfirmPasswordInput.Password;
                _isSyncingConfirmPassword = false;

                ConfirmPasswordInput.Visibility = Visibility.Collapsed;
                VisibleConfirmPasswordInput.Visibility = Visibility.Visible;
                VisibleConfirmPasswordInput.Focus();
                VisibleConfirmPasswordInput.CaretIndex = VisibleConfirmPasswordInput.Text.Length;
            }
            else
            {
                _isSyncingConfirmPassword = true;
                ConfirmPasswordInput.Password = VisibleConfirmPasswordInput.Text;
                _isSyncingConfirmPassword = false;

                VisibleConfirmPasswordInput.Visibility = Visibility.Collapsed;
                ConfirmPasswordInput.Visibility = Visibility.Visible;
                ConfirmPasswordInput.Focus();
            }

            RefreshPasswordPlaceholders();
        }

        private void RefreshPasswordPlaceholders()
        {
            var passwordText = _isPasswordVisible ? VisiblePasswordInput.Text : PasswordInput.Password;
            var confirmText = _isConfirmPasswordVisible ? VisibleConfirmPasswordInput.Text : ConfirmPasswordInput.Password;
            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(passwordText) ? Visibility.Visible : Visibility.Collapsed;
            ConfirmPasswordPlaceholder.Visibility = string.IsNullOrEmpty(confirmText) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}