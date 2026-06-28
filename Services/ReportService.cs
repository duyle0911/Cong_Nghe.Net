using QuanLyTaiChinhCaNhan_Nhom06.Helpers;
using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public class ReportService : IReportService
    {
        private readonly IDataService _dataService;

        public ReportService(IDataService dataService)
        {
            _dataService = dataService;
        }

        public Task<FinancialSummary> GetSummaryAsync(int userId) => _dataService.GetFinancialSummaryAsync(userId);

        public Task<List<CategorySpending>> GetCategorySpendingAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return _dataService.GetCategorySpendingByPeriodAsync(userId, startDate, endDate);
        }

        public Task<List<Transaction>> GetTransactionsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return _dataService.GetTransactionsByPeriodAsync(userId, startDate, endDate);
        }

        public async Task<List<CashFlowPoint>> GetCashFlowAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var result = new List<CashFlowPoint>();
            var month = DateTimeHelper.StartOfMonth(startDate);
            var last = DateTimeHelper.StartOfMonth(endDate);

            while (month <= last)
            {
                result.Add(new CashFlowPoint
                {
                    Label = $"{month:MM/yyyy}",
                    Income = await _dataService.GetMonthlyIncomeAsync(userId, month.Month, month.Year),
                    Expense = await _dataService.GetMonthlyExpenseAsync(userId, month.Month, month.Year)
                });

                month = month.AddMonths(1);
            }

            return result;
        }
    }
}

