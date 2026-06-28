using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public interface IReportService
    {
        Task<FinancialSummary> GetSummaryAsync(int userId);
        Task<List<CategorySpending>> GetCategorySpendingAsync(int userId, DateTime startDate, DateTime endDate);
        Task<List<Transaction>> GetTransactionsAsync(int userId, DateTime startDate, DateTime endDate);
        Task<List<CashFlowPoint>> GetCashFlowAsync(int userId, DateTime startDate, DateTime endDate);
    }

    public class CashFlowPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }
}

