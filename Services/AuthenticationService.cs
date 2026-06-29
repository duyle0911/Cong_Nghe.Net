using Microsoft.EntityFrameworkCore;
using QuanLyTaiChinhCaNhan_Nhom06.Data;
using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public class AuthenticationService : AuthService
    {
        public AuthenticationService(IDbContextFactory<ExpenseDbContext> dbFactory, ISessionContext sessionContext, IAppearanceService appearanceService)
            : base(dbFactory, sessionContext, appearanceService)
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

