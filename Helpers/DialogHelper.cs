using System.Windows;

namespace QuanLyTaiChinhCaNhan_Nhom06.Helpers
{
    public static class DialogHelper
    {
        public static void Info(string message) => MessageBox.Show(message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

        public static void Error(string message) => MessageBox.Show(message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

        public static bool Confirm(string message) => MessageBox.Show(message, "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}

