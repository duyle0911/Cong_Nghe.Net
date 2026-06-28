using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public class SessionContext : ISessionContext
    {
        private User? _currentUser;

        public User? CurrentUser => _currentUser;
        public bool IsLoggedIn => _currentUser != null;
        public int? CurrentUserId => _currentUser?.Id;

        public void SetCurrentUser(User user)
        {
            _currentUser = user;
        }

        public void ClearCurrentUser()
        {
            _currentUser = null;
        }
    }
}





