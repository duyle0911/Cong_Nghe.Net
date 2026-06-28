using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyTaiChinhCaNhan_Nhom06.Models
{
    public class Goal
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public decimal TargetAmount { get; set; }

        [Required]
        public decimal CurrentAmount { get; set; } = 0;

        [Required]
        public DateTime TargetDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        [StringLength(7)]
        public string? Color { get; set; } = "#2196F3";

        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public bool IsCompleted => CurrentAmount >= TargetAmount;

        [NotMapped]
        public decimal ProgressPercent => TargetAmount > 0 ? Math.Min(100, CurrentAmount / TargetAmount * 100) : 0;

        [NotMapped]
        public string Status
        {
            get
            {
                if (IsCompleted) return "Đã hoàn thành";
                if (TargetDate.Date <= DateTime.Today.AddDays(7)) return "Sắp đến hạn";
                return "Đang thực hiện";
            }
        }
    }
}
