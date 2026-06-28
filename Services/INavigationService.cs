using QuanLyTaiChinhCaNhan_Nhom06.ViewModels;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public interface INavigationService
    {
        ViewModelBase CreateViewModel(string viewName);
    }
}
