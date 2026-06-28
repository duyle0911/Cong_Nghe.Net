using Microsoft.EntityFrameworkCore;
using System.IO;
using QuanLyTaiChinhCaNhan_Nhom06.Data;
using QuanLyTaiChinhCaNhan_Nhom06.Helpers;
using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public class AuthService : IAuthenticationService
    {
        private readonly IDbContextFactory<ExpenseDbContext> _dbFactory;
        private readonly ISessionContext _sessionContext;
        private User? _currentUser;

        public AuthService(IDbContextFactory<ExpenseDbContext> dbFactory, ISessionContext sessionContext)
        {
            _dbFactory = dbFactory;
            _sessionContext = sessionContext;
        }

        public User? CurrentUser => _currentUser;
        public bool IsLoggedIn => _currentUser != null;

        public event EventHandler<UserLoggedInEventArgs>? UserLoggedIn;
        public event EventHandler? UserLoggedOut;

        public async Task<bool> RegisterAsync(string username, string email, string password, string? fullName = null)
        {
            username = username.Trim();
            email = email.Trim();

            if (!Validator.Required(username) || username.Any(char.IsWhiteSpace) || !Validator.Email(email) || password.Length < 6)
                return false;

            await using var context = await _dbFactory.CreateDbContextAsync();
            var exists = await context.Users.AnyAsync(u => u.Username == username || u.Email == email);

            if (exists)
                return false;

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                FullName = string.IsNullOrWhiteSpace(fullName) ? username : fullName.Trim(),
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            context.Categories.AddRange(SeedData.DefaultCategories(user.Id));
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LoginAsync(string usernameOrEmail, string password)
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "auth.log");
                File.AppendAllText(logPath, $"[Login] Start for '{usernameOrEmail}' at {DateTime.Now}\n");

                usernameOrEmail = (usernameOrEmail ?? string.Empty).Trim();
                File.AppendAllText(logPath, $"[Login] Trimmed input: '{usernameOrEmail}'\n");

                await using var context = await _dbFactory.CreateDbContextAsync();
                var user = await context.Users.FirstOrDefaultAsync(u => (u.Username == usernameOrEmail || u.Email == usernameOrEmail) && u.IsActive);
                File.AppendAllText(logPath, user == null ? "[Login] User not found or inactive\n" : $"[Login] Found user id={user.Id}\n");

                if (user == null || !PasswordHasher.Verify(password, user.PasswordHash))
                {
                    File.AppendAllText(logPath, "[Login] Password verify failed or user missing\n");
                    return false;
                }

                user.LastLoginAt = DateTime.Now;
                if (PasswordHasher.NeedsUpgrade(user.PasswordHash))
                    user.PasswordHash = PasswordHasher.Hash(password);

                await context.SaveChangesAsync();
                await EnsureDefaultCategoriesAsync(context, user.Id);
                _currentUser = user;
                _sessionContext.SetCurrentUser(user);
                UserLoggedIn?.Invoke(this, new UserLoggedInEventArgs(user));
                File.AppendAllText(logPath, $"[Login] Success for user id={user.Id}\n");
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "auth.log");
                    File.AppendAllText(logPath, $"[Login] Exception: {ex}\n");
                }
                catch { }
                return false;
            }
        }

        private static async Task EnsureDefaultCategoriesAsync(ExpenseDbContext context, int userId)
        {
            var hasCategories = await context.Categories.AnyAsync(c => c.UserId == userId);
            if (hasCategories)
                return;

            context.Categories.AddRange(SeedData.DefaultCategories(userId));
            await context.SaveChangesAsync();
        }

        public void Logout()
        {
            _currentUser = null;
            _sessionContext.ClearCurrentUser();
            UserLoggedOut?.Invoke(this, EventArgs.Empty);
        }

        public async Task<bool> UpdateProfileAsync(string? fullName, string? email, string? avatar = null)
        {
            email = email?.Trim();

            if (_currentUser == null || !Validator.Email(email))
                return false;

            await using var context = await _dbFactory.CreateDbContextAsync();
            var emailExists = await context.Users.AnyAsync(u => u.Email == email && u.Id != _currentUser.Id);

            if (emailExists)
                return false;

            var user = await context.Users.FindAsync(_currentUser.Id);

            if (user == null)
                return false;

            user.FullName = string.IsNullOrWhiteSpace(fullName) ? user.Username : fullName.Trim();
            user.Email = email!;

            if (avatar != null)
                user.Avatar = avatar;

            await context.SaveChangesAsync();

            _currentUser.FullName = user.FullName;
            _currentUser.Email = user.Email;
            _currentUser.Avatar = user.Avatar;
            _sessionContext.SetCurrentUser(_currentUser);
            return true;
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            if (_currentUser == null)
                return (false, "Bạn cần đăng nhập.");

            if (newPassword.Length < 6)
                return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

            await using var context = await _dbFactory.CreateDbContextAsync();
            var user = await context.Users.FindAsync(_currentUser.Id);

            if (user == null)
                return (false, "Không tìm thấy tài khoản.");

            if (!PasswordHasher.Verify(currentPassword, user.PasswordHash))
                return (false, "Mật khẩu hiện tại không đúng.");

            user.PasswordHash = PasswordHasher.Hash(newPassword);
            await context.SaveChangesAsync();
            _currentUser.PasswordHash = user.PasswordHash;
            _sessionContext.SetCurrentUser(_currentUser);
            return (true, "Đổi mật khẩu thành công.");
        }
    }
}
