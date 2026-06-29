using Microsoft.EntityFrameworkCore;
using QuanLyTaiChinhCaNhan_Nhom06.Data;
using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IDbContextFactory<ExpenseDbContext> _dbFactory;
        private readonly IAppearanceService _appearanceService;

        public CategoryService(IDbContextFactory<ExpenseDbContext> dbFactory, IAppearanceService appearanceService)
        {
            _dbFactory = dbFactory;
            _appearanceService = appearanceService;
        }

        public async Task<List<Category>> GetCategoriesAsync(int userId, TransactionType? type = null)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var query = context.Categories
                .AsNoTracking()
                .Where(c => c.UserId == userId);

            if (type.HasValue)
            {
                query = query.Where(c => c.Type == type.Value);
            }

            return await query.OrderBy(c => c.Type).ThenBy(c => c.Name).ToListAsync();
        }

        public async Task<bool> CreateCategoryAsync(Category category, int userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category.Name))
                    return false;

                category.Name = category.Name.Trim();
                category.Color = string.IsNullOrWhiteSpace(category.Color) ? "#2196F3" : category.Color.Trim();
                category.Icon = string.IsNullOrWhiteSpace(category.Icon) ? "Category" : category.Icon.Trim();

                await using var context = await _dbFactory.CreateDbContextAsync();
                var exists = await context.Categories
                    .AnyAsync(c => c.Name == category.Name && c.Type == category.Type && c.UserId == userId);

                if (exists)
                    return false;

                category.UserId = userId;
                category.CreatedAt = DateTime.Now;
                category.UpdatedAt = DateTime.Now;
                context.Categories.Add(category);
                await context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateCategoryAsync(int categoryId, string name, TransactionType type, string color, string icon, int userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                name = name.Trim();
                color = string.IsNullOrWhiteSpace(color) ? "#2196F3" : color.Trim();
                icon = string.IsNullOrWhiteSpace(icon) ? "Category" : icon.Trim();

                await using var context = await _dbFactory.CreateDbContextAsync();
                var category = await context.Categories
                    .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

                if (category == null)
                    return false;

                if (category.Type != type)
                {
                    var hasTransactions = await context.Transactions
                        .AnyAsync(t => t.CategoryId == categoryId && t.UserId == userId);
                    var hasBudgets = await context.Budgets
                        .AnyAsync(b => b.CategoryId == categoryId && b.UserId == userId);

                    if (hasTransactions || hasBudgets)
                        return false;
                }

                var exists = await context.Categories
                    .AnyAsync(c => c.Name == name && c.Type == type && c.UserId == userId && c.Id != categoryId);

                if (exists)
                    return false;

                category.Name = name;
                category.Type = type;
                category.Color = color;
                category.Icon = icon;
                category.UpdatedAt = DateTime.Now;
                context.Categories.Update(category);
                await context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool Success, string Message)> DeleteCategoryAsync(int categoryId, int userId)
        {
            try
            {
                await using var context = await _dbFactory.CreateDbContextAsync();
                var category = await context.Categories
                    .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

                if (category == null)
                    return (false, _appearanceService.T("NotFoundCategory"));

                var hasTransactions = await context.Transactions
                    .AnyAsync(t => t.CategoryId == categoryId && t.UserId == userId);

                if (hasTransactions)
                    return (false, _appearanceService.T("CategoryHasTransactions"));

                var hasBudgets = await context.Budgets
                    .AnyAsync(b => b.CategoryId == categoryId && b.UserId == userId);

                if (hasBudgets)
                    return (false, _appearanceService.T("CategoryHasBudgets"));

                context.Categories.Remove(category);
                await context.SaveChangesAsync();
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, _appearanceService.Format("GenericErrorFormat", ex.Message));
            }
        }
    }
}

