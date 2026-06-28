namespace QuanLyTaiChinhCaNhan_Nhom06.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime StartOfMonth(DateTime date) => new(date.Year, date.Month, 1);

        public static DateTime EndOfMonth(DateTime date) => new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
    }
}

