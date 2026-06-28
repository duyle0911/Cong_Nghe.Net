using Microsoft.EntityFrameworkCore;
using QuanLyTaiChinhCaNhan_Nhom06.Data;
using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public class DataService : IDataService
    {
        private readonly IDbContextFactory<ExpenseDbContext> _dbFactory;

        public DataService(IDbContextFactory<ExpenseDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<bool> HasDataAsync(int userId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await context.Transactions.AnyAsync(t => t.UserId == userId)
                || await context.Budgets.AnyAsync(b => b.UserId == userId)
                || await context.Goals.AnyAsync(g => g.UserId == userId);
        }

        public async Task<List<CategorySpending>> GetCategorySpendingByPeriodAsync(int userId, DateTime startDate, DateTime endDate)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var from = startDate.Date;
            var to = endDate.Date;

            return await context.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Where(t => t.UserId == userId
                    && t.Type == TransactionType.Expense
                    && t.Category != null
                    && !t.IsAllocation
                    && !t.IsRefunded
                    && t.Date.Date >= from
                    && t.Date.Date <= to)
                .GroupBy(t => new { t.CategoryId, t.Category!.Name, t.Category!.Color })
                .Select(g => new CategorySpending
                {
                    CategoryName = g.Key.Name,
                    Amount = g.Sum(t => t.Amount),
                    Color = g.Key.Color
                })
                .OrderByDescending(x => x.Amount)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetRecentTransactionsAsync(int userId, int count = 10)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await context.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && !t.IsAllocation && !t.IsRefunded)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetTransactionsByPeriodAsync(int userId, DateTime startDate, DateTime endDate)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var from = startDate.Date;
            var to = endDate.Date;

            return await context.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Where(t => t.UserId == userId
                    && !t.IsAllocation
                    && !t.IsRefunded
                    && t.Date.Date >= from
                    && t.Date.Date <= to)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<FinancialSummary> GetFinancialSummaryAsync(int userId)
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            var currentMonthIncome = await GetMonthlyIncomeAsync(userId, currentMonth, currentYear);
            var currentMonthExpense = await GetMonthlyExpenseAsync(userId, currentMonth, currentYear);
            var lastMonthIncome = await GetMonthlyIncomeAsync(userId, lastMonth, lastMonthYear);
            var lastMonthExpense = await GetMonthlyExpenseAsync(userId, lastMonth, lastMonthYear);
            var totalIncome = await GetTotalIncomeAsync(userId);
            var totalExpense = await GetTotalExpenseAsync(userId);

            return new FinancialSummary
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                TotalBalance = totalIncome - totalExpense,
                MonthlyIncome = currentMonthIncome,
                MonthlyExpense = currentMonthExpense,
                SavingsRate = currentMonthIncome > 0 ? (currentMonthIncome - currentMonthExpense) / currentMonthIncome * 100 : 0,
                IncomeChangePercent = lastMonthIncome > 0 ? (currentMonthIncome - lastMonthIncome) / lastMonthIncome * 100 : 0,
                ExpenseChangePercent = lastMonthExpense > 0 ? (currentMonthExpense - lastMonthExpense) / lastMonthExpense * 100 : 0
            };
        }

        public async Task<decimal> GetMonthlyIncomeAsync(int userId, int? month = null, int? year = null)
        {
            var targetMonth = month ?? DateTime.Now.Month;
            var targetYear = year ?? DateTime.Now.Year;
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await BaseTransactions(context, userId)
                .Where(t => t.Type == TransactionType.Income && t.Date.Month == targetMonth && t.Date.Year == targetYear)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
        }

        public async Task<decimal> GetMonthlyExpenseAsync(int userId, int? month = null, int? year = null)
        {
            var targetMonth = month ?? DateTime.Now.Month;
            var targetYear = year ?? DateTime.Now.Year;
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await BaseTransactions(context, userId)
                .Where(t => t.Type == TransactionType.Expense && t.Date.Month == targetMonth && t.Date.Year == targetYear)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
        }

        public async Task<decimal> GetTotalIncomeByPeriodAsync(int userId, DateTime startDate, DateTime endDate)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await BaseTransactions(context, userId)
                .Where(t => t.Type == TransactionType.Income && t.Date.Date >= startDate.Date && t.Date.Date <= endDate.Date)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
        }

        public async Task<decimal> GetTotalExpenseByPeriodAsync(int userId, DateTime startDate, DateTime endDate)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await BaseTransactions(context, userId)
                .Where(t => t.Type == TransactionType.Expense && t.Date.Date >= startDate.Date && t.Date.Date <= endDate.Date)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
        }

        public async Task<decimal> GetTotalIncomeAsync(int userId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await BaseTransactions(context, userId)
                .Where(t => t.Type == TransactionType.Income)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
        }

        public async Task<decimal> GetTotalExpenseAsync(int userId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await BaseTransactions(context, userId)
                .Where(t => t.Type == TransactionType.Expense)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
        }

        public async Task<decimal> GetAverageMonthlyIncomeAsync(int userId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var values = await BaseTransactions(context, userId)
                .Where(t => t.Type == TransactionType.Income)
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => g.Sum(t => t.Amount))
                .ToListAsync();
            return values.Any() ? values.Average() : 0;
        }

        public async Task<decimal> GetAverageMonthlyExpenseAsync(int userId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var values = await BaseTransactions(context, userId)
                .Where(t => t.Type == TransactionType.Expense)
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => g.Sum(t => t.Amount))
                .ToListAsync();
            return values.Any() ? values.Average() : 0;
        }

        public async Task<bool> ResetUserDataAsync(int userId)
        {
            try
            {
                await using var context = await _dbFactory.CreateDbContextAsync();
                context.Transactions.RemoveRange(await context.Transactions.Where(t => t.UserId == userId).ToListAsync());
                context.Budgets.RemoveRange(await context.Budgets.Where(b => b.UserId == userId).ToListAsync());
                context.Goals.RemoveRange(await context.Goals.Where(g => g.UserId == userId).ToListAsync());
                await context.SaveChangesAsync();

                if (!await context.Categories.AnyAsync(c => c.UserId == userId))
                {
                    context.Categories.AddRange(SeedData.DefaultCategories(userId));
                    await context.SaveChangesAsync();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static IQueryable<Transaction> BaseTransactions(ExpenseDbContext context, int userId)
        {
            return context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId && !t.IsAllocation && !t.IsRefunded);
        }
    }

    public class CategorySpending
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Color { get; set; } = "#2196F3";
    }

    public class FinancialSummary
    {
        public decimal TotalBalance { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpense { get; set; }
        public decimal SavingsRate { get; set; }
        public decimal IncomeChangePercent { get; set; }
        public decimal ExpenseChangePercent { get; set; }
    }
}
