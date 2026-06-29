using System.Windows;

namespace QuanLyTaiChinhCaNhan_Nhom06.Helpers
{
    public static class DialogHelper
    {
        public static void Info(string message) => MessageBox.Show(message, Text("DialogInfoTitle"), MessageBoxButton.OK, MessageBoxImage.Information);

        public static void Error(string message) => MessageBox.Show(message, Text("DialogErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);

        public static bool Confirm(string message) => MessageBox.Show(message, Text("DialogConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

        public static string Text(string key)
        {
            return Application.Current?.Resources[key] as string ?? key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, Text(key), args);
        }
    }
}
