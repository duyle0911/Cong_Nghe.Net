using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public interface ISessionContext
    {
        User? CurrentUser { get; }
        bool IsLoggedIn { get; }
        int? CurrentUserId { get; }

        void SetCurrentUser(User user);
        void ClearCurrentUser();
    }
}







