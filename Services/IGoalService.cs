using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public interface IGoalService
    {
        Task<List<Goal>> GetGoalsAsync(int userId);
        Task<Goal?> GetGoalByIdAsync(int goalId, int userId);
        Task<bool> CreateGoalAsync(Goal goal, int userId);
        Task<bool> UpdateGoalAsync(int goalId, string name, string? description, decimal targetAmount, DateTime targetDate, string? color, int userId);
        Task<bool> DeleteGoalAsync(int goalId, int userId);
        Task<(bool Success, string Message)> AddMoneyToGoalAsync(int goalId, decimal amount, int userId);
        Task<string> GetGoalStatusAsync(Goal goal);
    }
}












