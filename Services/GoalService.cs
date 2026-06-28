using Microsoft.EntityFrameworkCore;
using QuanLyTaiChinhCaNhan_Nhom06.Data;
using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public class GoalService : IGoalService
    {
        private readonly IDbContextFactory<ExpenseDbContext> _dbFactory;
        private readonly IDataService _dataService;
        private readonly IBudgetService _budgetService;

        public GoalService(IDbContextFactory<ExpenseDbContext> dbFactory, IDataService dataService, IBudgetService budgetService)
        {
            _dbFactory = dbFactory;
            _dataService = dataService;
            _budgetService = budgetService;
        }

        public async Task<List<Goal>> GetGoalsAsync(int userId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await context.Goals
                .AsNoTracking()
                .Where(g => g.UserId == userId)
                .OrderByDescending(g => g.TargetDate)
                .ToListAsync();
        }

        public async Task<Goal?> GetGoalByIdAsync(int goalId, int userId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await context.Goals
                .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);
        }

        public async Task<bool> CreateGoalAsync(Goal goal, int userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(goal.Name) || goal.TargetAmount <= 0 || goal.CurrentAmount < 0)
                    return false;

                goal.Name = goal.Name.Trim();
                goal.Description = goal.Description?.Trim();
                goal.TargetDate = goal.TargetDate.Date;
                goal.CompletedDate = goal.CurrentAmount >= goal.TargetAmount ? DateTime.Now : null;
                goal.Color = string.IsNullOrWhiteSpace(goal.Color) ? "#2196F3" : goal.Color;
                goal.UserId = userId;
                goal.CreatedAt = DateTime.Now;
                goal.UpdatedAt = DateTime.Now;

                await using var context = await _dbFactory.CreateDbContextAsync();
                context.Goals.Add(goal);
                await context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateGoalAsync(int goalId, string name, string? description, decimal targetAmount, DateTime targetDate, string? color, int userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || targetAmount <= 0)
                    return false;

                await using var context = await _dbFactory.CreateDbContextAsync();
                var goal = await context.Goals
                    .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);

                if (goal == null)
                    return false;

                goal.Name = name.Trim();
                goal.Description = description?.Trim();
                goal.TargetAmount = targetAmount;
                goal.TargetDate = targetDate.Date;
                goal.Color = string.IsNullOrWhiteSpace(color) ? "#06B6D4" : color;
                goal.UpdatedAt = DateTime.Now;
                goal.CompletedDate = goal.CurrentAmount >= targetAmount ? goal.CompletedDate ?? DateTime.Now : null;

                await context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteGoalAsync(int goalId, int userId)
        {
            try
            {
                await using var context = await _dbFactory.CreateDbContextAsync();
                var goal = await context.Goals
                    .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);

                if (goal == null)
                    return false;

                context.Goals.Remove(goal);
                await context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool Success, string Message)> AddMoneyToGoalAsync(int goalId, decimal amount, int userId)
        {
            try
            {
                if (amount <= 0)
                    return (false, "Số tiền phải lớn hơn 0.");

                await using var context = await _dbFactory.CreateDbContextAsync();
                var goal = await context.Goals
                    .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);

                if (goal == null)
                    return (false, "Không tìm thấy mục tiêu.");

                if (goal.CurrentAmount >= goal.TargetAmount)
                    return (false, "Mục tiêu này đã hoàn thành.");

                var remainingTarget = goal.TargetAmount - goal.CurrentAmount;
                if (amount > remainingTarget)
                    return (false, $"Số tiền thêm vào vượt quá số còn thiếu của mục tiêu: {remainingTarget:N0} ₫.");

                var summary = await _dataService.GetFinancialSummaryAsync(userId);
                var allocatedAmount = await context.Goals
                    .AsNoTracking()
                    .Where(g => g.UserId == userId)
                    .SumAsync(g => (decimal?)g.CurrentAmount) ?? 0;
                var availableBalance = summary.TotalBalance - allocatedAmount;

                if (amount > availableBalance)
                {
                    return (false, $"Số dư khả dụng không đủ!\n\n" +
                                  $"Số dư hiện tại: {summary.TotalBalance:N0} ₫\n" +
                                  $"Đã phân bổ vào mục tiêu: {allocatedAmount:N0} ₫\n" +
                                  $"Số dư khả dụng: {availableBalance:N0} ₫\n" +
                                  $"Số tiền muốn thêm: {amount:N0} ₫");
                }

                var goalCategory = await context.Categories
                    .FirstOrDefaultAsync(c => c.Name == "Mục tiêu"
                        && c.UserId == userId
                        && c.Type == TransactionType.Expense);

                if (goalCategory == null)
                {
                    goalCategory = new Category
                    {
                        Name = "Mục tiêu",
                        Color = "#FF9800",
                        Icon = "Target",
                        Type = TransactionType.Expense,
                        UserId = userId,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    context.Categories.Add(goalCategory);
                }

                var transaction = new Transaction
                {
                    Amount = amount,
                    Description = $"Phân bổ vào mục tiêu: {goal.Name}",
                    Type = TransactionType.Expense,
                    Category = goalCategory,
                    UserId = userId,
                    Date = DateTime.Now.Date,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsAllocation = true
                };

                context.Transactions.Add(transaction);

                goal.CurrentAmount += amount;
                goal.UpdatedAt = DateTime.Now;

                if (goal.CurrentAmount >= goal.TargetAmount && goal.CompletedDate == null)
                    goal.CompletedDate = DateTime.Now;

                await context.SaveChangesAsync();
                await _budgetService.UpdateBudgetSpentAmountAsync(goalCategory.Id, userId, 0, transaction.Date);

                var message = goal.CurrentAmount >= goal.TargetAmount
                    ? $"Chúc mừng! Bạn đã hoàn thành mục tiêu '{goal.Name}'!"
                    : $"Đã thêm {amount:N0} ₫ vào mục tiêu '{goal.Name}'!";

                return (true, message);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi thêm tiền vào mục tiêu: {ex.Message}");
            }
        }

        public Task<string> GetGoalStatusAsync(Goal goal)
        {
            return Task.FromResult(GetGoalStatus(goal));
        }

        public string GetGoalStatus(Goal goal)
        {
            if (goal.IsCompleted)
                return "Hoàn thành";

            var now = DateTime.Now;
            if (goal.TargetDate < now)
                return "Quá hạn";

            var daysLeft = (goal.TargetDate - now).Days;
            if (daysLeft <= 7)
                return "Sắp hết hạn";

            return "Đang thực hiện";
        }
    }
}
