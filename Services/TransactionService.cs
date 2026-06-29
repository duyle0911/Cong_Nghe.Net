using Microsoft.EntityFrameworkCore;
using QuanLyTaiChinhCaNhan_Nhom06.Data;
using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IDbContextFactory<ExpenseDbContext> _dbFactory;
        private readonly IBudgetService _budgetService;
        private readonly IAppearanceService _appearanceService;

        public TransactionService(IDbContextFactory<ExpenseDbContext> dbFactory, IBudgetService budgetService, IAppearanceService appearanceService)
        {
            _dbFactory = dbFactory;
            _budgetService = budgetService;
            _appearanceService = appearanceService;
        }

        public async Task<List<Transaction>> GetTransactionsAsync(int userId, TransactionType? type = null)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var query = context.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && !t.IsAllocation && !t.IsRefunded);

            if (type.HasValue)
                query = query.Where(t => t.Type == type.Value);

            return await query.OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt).ToListAsync();
        }

        public async Task<Transaction?> GetTransactionByIdAsync(int transactionId, int userId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await context.Transactions
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);
        }

        public async Task<(bool Success, string Message)> CreateTransactionAsync(Transaction transaction, int userId)
        {
            try
            {
                var validation = await ValidateTransactionInputAsync(transaction.Amount, transaction.Description, transaction.CategoryId, transaction.Type, userId);
                if (!validation.Success)
                    return validation;

                await using var context = await _dbFactory.CreateDbContextAsync();
                transaction.UserId = userId;
                transaction.Description = transaction.Description.Trim();
                transaction.Date = transaction.Date.Date;
                transaction.CreatedAt = DateTime.Now;
                transaction.UpdatedAt = DateTime.Now;
                transaction.BudgetId = await FindBudgetIdAsync(transaction.CategoryId, userId, transaction.Date, transaction.Type);

                context.Transactions.Add(transaction);
                await context.SaveChangesAsync();
                await _budgetService.UpdateBudgetSpentAmountAsync(transaction.CategoryId, userId, 0, transaction.Date);

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, _appearanceService.Format("GenericErrorFormat", ex.Message));
            }
        }

        public async Task<(bool Success, string Message)> UpdateTransactionAsync(int transactionId, decimal amount, string description, TransactionType type, int categoryId, DateTime date, int userId)
        {
            try
            {
                var validation = await ValidateTransactionInputAsync(amount, description, categoryId, type, userId);
                if (!validation.Success)
                    return validation;

                await using var context = await _dbFactory.CreateDbContextAsync();
                var transaction = await context.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

                if (transaction == null)
                    return (false, _appearanceService.T("NotFoundTransaction"));

                var oldCategoryId = transaction.CategoryId;
                var oldDate = transaction.Date;

                transaction.Amount = amount;
                transaction.Description = description.Trim();
                transaction.Type = type;
                transaction.CategoryId = categoryId;
                transaction.Date = date.Date;
                transaction.BudgetId = await FindBudgetIdAsync(categoryId, userId, date, type);
                transaction.UpdatedAt = DateTime.Now;

                await context.SaveChangesAsync();
                await _budgetService.UpdateBudgetSpentAmountAsync(oldCategoryId, userId, 0, oldDate);
                await _budgetService.UpdateBudgetSpentAmountAsync(categoryId, userId, 0, date);

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, _appearanceService.Format("GenericErrorFormat", ex.Message));
            }
        }

        public async Task<(bool Success, string Message)> DeleteTransactionAsync(int transactionId, int userId)
        {
            try
            {
                await using var context = await _dbFactory.CreateDbContextAsync();
                var transaction = await context.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

                if (transaction == null)
                    return (false, _appearanceService.T("NotFoundTransaction"));

                var categoryId = transaction.CategoryId;
                var date = transaction.Date;
                context.Transactions.Remove(transaction);
                await context.SaveChangesAsync();
                await _budgetService.UpdateBudgetSpentAmountAsync(categoryId, userId, 0, date);

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, _appearanceService.Format("GenericErrorFormat", ex.Message));
            }
        }

        public Task<(bool CanAfford, string Message)> ValidateExpenseTransactionAsync(decimal amount, int categoryId, int userId, DateTime date)
        {
            return Task.FromResult((amount > 0, amount > 0 ? string.Empty : _appearanceService.T("AmountGreaterThanZero")));
        }

        public Task<(bool CanAfford, string Message)> ValidateExpenseTransactionUpdateAsync(int transactionId, decimal newAmount, int newCategoryId, int userId, DateTime newDate)
        {
            return Task.FromResult((newAmount > 0, newAmount > 0 ? string.Empty : _appearanceService.T("AmountGreaterThanZero")));
        }

        private async Task<(bool Success, string Message)> ValidateTransactionInputAsync(decimal amount, string description, int categoryId, TransactionType type, int userId)
        {
            if (amount <= 0)
                return (false, _appearanceService.T("AmountGreaterThanZero"));

            if (string.IsNullOrWhiteSpace(description))
                return (false, _appearanceService.T("TransactionDescriptionRequired"));

            await using var context = await _dbFactory.CreateDbContextAsync();
            var categoryExists = await context.Categories.AnyAsync(c => c.Id == categoryId && c.UserId == userId && c.Type == type);

            if (!categoryExists)
                return (false, _appearanceService.T("ValidCategoryRequired"));

            return (true, string.Empty);
        }

        private async Task<int?> FindBudgetIdAsync(int categoryId, int userId, DateTime date, TransactionType type)
        {
            if (type != TransactionType.Expense)
                return null;

            var budget = await _budgetService.GetBudgetByCategoryAndDateAsync(categoryId, userId, date);
            return budget?.Id;
        }
    }
}

