using System.Globalization;

namespace QuanLyTaiChinhCaNhan_Nhom06.Helpers
{
    public static class MoneyFormatter
    {
        public static string Vnd(decimal value) => string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} ₫", value);
    }
}
