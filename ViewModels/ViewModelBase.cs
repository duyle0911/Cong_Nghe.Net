using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuanLyTaiChinhCaNhan_Nhom06.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected async Task RunSafeAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Helpers.AppLogger.Log(ex);
                Helpers.DialogHelper.Error($"Không thể tải dữ liệu màn hình: {ex.Message}");
            }
        }
    }
}

