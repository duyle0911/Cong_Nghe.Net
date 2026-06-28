using Microsoft.Extensions.DependencyInjection;
using QuanLyTaiChinhCaNhan_Nhom06.ViewModels;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ViewModelBase CreateViewModel(string viewName)
        {
            return viewName switch
            {
                "Transactions" => _serviceProvider.GetRequiredService<TransactionViewModel>(),
                "Categories" => _serviceProvider.GetRequiredService<CategoryViewModel>(),
                "Budgets" => _serviceProvider.GetRequiredService<BudgetViewModel>(),
                "Goals" => _serviceProvider.GetRequiredService<GoalViewModel>(),
                "Reports" => _serviceProvider.GetRequiredService<ReportViewModel>(),
                "Profile" => _serviceProvider.GetRequiredService<ProfileViewModel>(),
                "Register" => _serviceProvider.GetRequiredService<RegisterViewModel>(),
                "Login" => _serviceProvider.GetRequiredService<LoginViewModel>(),
                _ => _serviceProvider.GetRequiredService<DashboardViewModel>()
            };
        }
    }
}
