using Microsoft.EntityFrameworkCore;
using QuanLyTaiChinhCaNhan_Nhom06.Data;
using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly IDbContextFactory<ExpenseDbContext> _dbFactory;

        public BudgetService(IDbContextFactory<ExpenseDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<Budget>> GetBudgetsAsync(int userId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var budgets = await context.Budgets
                .AsNoTracking()
                .Include(b => b.Category)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.IsActive)
                .ThenBy(b => b.StartDate)
                .ThenBy(b => b.Category != null ? b.Category.Name : string.Empty)
                .ToListAsync();

            if (budgets.Count > 0)
            {
                var minDate = budgets.Min(b => b.StartDate).Date;
                var maxDate = budgets.Max(b => b.EndDate).Date;
                var expenses = await context.Transactions
                    .AsNoTracking()
                    .Where(t => t.UserId == userId
                        && t.Type == TransactionType.Expense
                        && !t.IsAllocation
                        && !t.IsRefunded
                        && t.Date.Date >= minDate
                        && t.Date.Date <= maxDate)
                    .Select(t => new { t.CategoryId, Date = t.Date.Date, t.Amount })
                    .ToListAsync();

                foreach (var budget in budgets)
                {
                    budget.SpentAmount = expenses
                        .Where(t => t.CategoryId == budget.CategoryId
                            && t.Date >= budget.StartDate.Date
                            && t.Date <= budget.EndDate.Date)
                        .Sum(t => t.Amount);
                }
            }

            return budgets;
        }

        public async Task<Budget?> GetBudgetByIdAsync(int budgetId, int userId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var budget = await context.Budgets
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);

            if (budget != null)
                budget.SpentAmount = await CalculateActualSpentAmountAsync(budget.Id);

            return budget;
        }

        public async Task<Budget?> GetBudgetByCategoryAndDateAsync(int categoryId, int userId, DateTime date)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var targetDate = date.Date;
            var budget = await context.Budgets
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.CategoryId == categoryId
                    && b.UserId == userId
                    && b.IsActive
                    && targetDate >= b.StartDate.Date
                    && targetDate <= b.EndDate.Date);

            if (budget != null)
                budget.SpentAmount = await CalculateActualSpentAmountAsync(budget.Id);

            return budget;
        }

        public async Task<bool> CreateBudgetAsync(Budget budget, int userId)
        {
            try
            {
                if (budget.Amount <= 0 || budget.StartDate.Date > budget.EndDate.Date)
                    return false;

                await using var context = await _dbFactory.CreateDbContextAsync();
                var categoryExists = await context.Categories
                    .AnyAsync(c => c.Id == budget.CategoryId && c.UserId == userId && c.Type == TransactionType.Expense);

                if (!categoryExists)
                    return false;

                var overlaps = await context.Budgets.AnyAsync(b => b.CategoryId == budget.CategoryId
                    && b.UserId == userId
                    && b.IsActive
                    && b.StartDate.Date <= budget.EndDate.Date
                    && b.EndDate.Date >= budget.StartDate.Date);

                if (overlaps)
                    return false;

                budget.UserId = userId;
                budget.Name = string.IsNullOrWhiteSpace(budget.Name) ? "Ngân sách" : budget.Name.Trim();
                budget.StartDate = budget.StartDate.Date;
                budget.EndDate = budget.EndDate.Date;
                budget.SpentAmount = 0;
                budget.IsActive = true;
                budget.CreatedAt = DateTime.Now;
                budget.UpdatedAt = DateTime.Now;

                context.Budgets.Add(budget);
                await context.SaveChangesAsync();
                await RecalculateBudgetSpentAmountAsync(context, budget.Id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateBudgetAsync(int budgetId, decimal newAmount, DateTime startDate, DateTime endDate, bool isActive, int userId)
        {
            try
            {
                if (newAmount <= 0 || startDate.Date > endDate.Date)
                    return false;

                await using var context = await _dbFactory.CreateDbContextAsync();
                var budget = await context.Budgets.FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);

                if (budget == null)
                    return false;

                var overlaps = await context.Budgets.AnyAsync(b => b.Id != budgetId
                    && b.CategoryId == budget.CategoryId
                    && b.UserId == userId
                    && b.IsActive
                    && b.StartDate.Date <= endDate.Date
                    && b.EndDate.Date >= startDate.Date);

                if (overlaps)
                    return false;

                budget.Amount = newAmount;
                budget.StartDate = startDate.Date;
                budget.EndDate = endDate.Date;
                budget.IsActive = isActive;
                budget.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();
                await RecalculateBudgetSpentAmountAsync(context, budget.Id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool Success, string Message)> DeleteBudgetAsync(int budgetId, int userId)
        {
            try
            {
                await using var context = await _dbFactory.CreateDbContextAsync();
                var budget = await context.Budgets.FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);

                if (budget == null)
                    return (false, "Không tìm thấy ngân sách.");

                var transactions = await context.Transactions
                    .Where(t => t.BudgetId == budgetId && t.UserId == userId)
                    .ToListAsync();

                foreach (var transaction in transactions)
                    transaction.BudgetId = null;

                context.Budgets.Remove(budget);
                await context.SaveChangesAsync();
                return (true, "Đã xóa ngân sách thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi xóa ngân sách: {ex.Message}");
            }
        }

        public async Task<bool> UpdateBudgetSpentAmountAsync(int categoryId, int userId, decimal amount, DateTime? transactionDate = null)
        {
            try
            {
                await using var context = await _dbFactory.CreateDbContextAsync();
                var date = (transactionDate ?? DateTime.Now).Date;
                var budget = await context.Budgets.FirstOrDefaultAsync(b => b.CategoryId == categoryId
                    && b.UserId == userId
                    && b.IsActive
                    && date >= b.StartDate.Date
                    && date <= b.EndDate.Date);

                if (budget == null)
                    return false;

                await RecalculateBudgetSpentAmountAsync(context, budget.Id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<BudgetAlertResult> CheckBudgetAlertsAsync(int userId)
        {
            var result = new BudgetAlertResult();
            var budgets = await GetBudgetsAsync(userId);

            foreach (var budget in budgets.Where(b => b.IsActive))
            {
                var percentage = budget.Amount > 0 ? budget.SpentAmount / budget.Amount * 100 : 0;
                var name = budget.Category?.Name ?? budget.Name;

                if (percentage >= 100)
                    result.OverBudgetItems.Add($"{name}: đã vượt {percentage:F1}%");
                else if (percentage >= 80)
                    result.WarningItems.Add($"{name}: đã dùng {percentage:F1}%");
            }

            return result;
        }

        public async Task<decimal> CalculateActualSpentAmountAsync(int budgetId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var budget = await context.Budgets.AsNoTracking().FirstOrDefaultAsync(b => b.Id == budgetId);

            if (budget == null)
                return 0;

            return await context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == budget.UserId
                    && t.CategoryId == budget.CategoryId
                    && t.Type == TransactionType.Expense
                    && !t.IsAllocation
                    && !t.IsRefunded
                    && t.Date.Date >= budget.StartDate.Date
                    && t.Date.Date <= budget.EndDate.Date)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
        }

        private static async Task RecalculateBudgetSpentAmountAsync(ExpenseDbContext context, int budgetId)
        {
            var budget = await context.Budgets.FirstOrDefaultAsync(b => b.Id == budgetId);
            if (budget == null)
                return;

            budget.SpentAmount = await context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == budget.UserId
                    && t.CategoryId == budget.CategoryId
                    && t.Type == TransactionType.Expense
                    && !t.IsAllocation
                    && !t.IsRefunded
                    && t.Date.Date >= budget.StartDate.Date
                    && t.Date.Date <= budget.EndDate.Date)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
            budget.UpdatedAt = DateTime.Now;
            await context.SaveChangesAsync();
        }
    }
}
