using System.Windows;
using System.Windows.Controls;
using QuanLyTaiChinhCaNhan_Nhom06.ViewModels;

namespace QuanLyTaiChinhCaNhan_Nhom06.Views
{
    public partial class ProfileView : UserControl
    {
        public ProfileView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ProfileViewModel oldViewModel)
                oldViewModel.PasswordChangeSucceeded -= OnPasswordChangeSucceeded;

            if (e.NewValue is ProfileViewModel newViewModel)
                newViewModel.PasswordChangeSucceeded += OnPasswordChangeSucceeded;
        }

        private void OnPasswordChangeSucceeded(object? sender, EventArgs e)
        {
            CurrentPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
        }

        private void CurrentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProfileViewModel viewModel && sender is PasswordBox passwordBox)
                viewModel.CurrentPassword = passwordBox.Password;
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProfileViewModel viewModel && sender is PasswordBox passwordBox)
                viewModel.NewPassword = passwordBox.Password;
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProfileViewModel viewModel && sender is PasswordBox passwordBox)
                viewModel.ConfirmPassword = passwordBox.Password;
        }
    }
}
