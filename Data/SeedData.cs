using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Data
{
    public static class SeedData
    {
        public static List<Category> DefaultCategories(int userId)
        {
            var now = DateTime.Now;
            return new List<Category>
            {
                new() { Name = "Ăn uống", Color = "#FF4B16", Icon = "Restaurant", Type = TransactionType.Expense, UserId = userId, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Mua sắm", Color = "#D842CD", Icon = "Shopping", Type = TransactionType.Expense, UserId = userId, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Giao thông", Color = "#009EF7", Icon = "DirectionsCar", Type = TransactionType.Expense, UserId = userId, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Giải trí", Color = "#7B48F6", Icon = "Movie", Type = TransactionType.Expense, UserId = userId, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Y tế", Color = "#FF3377", Icon = "MedicalBag", Type = TransactionType.Expense, UserId = userId, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Hóa đơn", Color = "#00C875", Icon = "Receipt", Type = TransactionType.Expense, UserId = userId, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Khác", Color = "#475569", Icon = "DotsHorizontal", Type = TransactionType.Expense, UserId = userId, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Lương", Color = "#22C55E", Icon = "Briefcase", Type = TransactionType.Income, UserId = userId, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Thưởng", Color = "#84CC16", Icon = "Gift", Type = TransactionType.Income, UserId = userId, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Freelance", Color = "#14B8A6", Icon = "Laptop", Type = TransactionType.Income, UserId = userId, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Kinh doanh", Color = "#0EA5E9", Icon = "Store", Type = TransactionType.Income, UserId = userId, CreatedAt = now, UpdatedAt = now },
                new() { Name = "Khác", Color = "#3B82F6", Icon = "DotsHorizontal", Type = TransactionType.Income, UserId = userId, CreatedAt = now, UpdatedAt = now }
            };
        }
    }
}
