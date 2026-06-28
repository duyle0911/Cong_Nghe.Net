using Microsoft.EntityFrameworkCore;
using QuanLyTaiChinhCaNhan_Nhom06.Data;
using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public class AuthenticationService : AuthService
    {
        public AuthenticationService(IDbContextFactory<ExpenseDbContext> dbFactory, ISessionContext sessionContext)
            : base(dbFactory, sessionContext)
        {
        }
    }

    public class UserLoggedInEventArgs : EventArgs
    {
        public User User { get; }

        public UserLoggedInEventArgs(User user)
        {
            User = user;
        }
    }
}
