using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetCategoriesAsync(int userId, TransactionType? type = null);
        Task<bool> CreateCategoryAsync(Category category, int userId);
        Task<bool> UpdateCategoryAsync(int categoryId, string name, TransactionType type, string color, string icon, int userId);
        Task<(bool Success, string Message)> DeleteCategoryAsync(int categoryId, int userId);
    }
}

