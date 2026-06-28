using System.IO;

namespace QuanLyTaiChinhCaNhan_Nhom06.Helpers
{
    public static class AppLogger
    {
        private static readonly object SyncRoot = new();

        public static void Log(Exception exception)
        {
            Log(exception.ToString());
        }

        public static void Log(string message)
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app-error.log");
                lock (SyncRoot)
                {
                    File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n\n");
                }
            }
            catch
            {
            }
        }
    }
}
