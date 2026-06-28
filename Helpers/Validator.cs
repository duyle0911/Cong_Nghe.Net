using System.Net.Mail;

namespace QuanLyTaiChinhCaNhan_Nhom06.Helpers
{
    public static class Validator
    {
        public static bool Required(string? value) => !string.IsNullOrWhiteSpace(value);

        public static bool Positive(decimal value) => value > 0;

        public static bool Email(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                return new MailAddress(value).Address == value;
            }
            catch
            {
                return false;
            }
        }
    }
}

