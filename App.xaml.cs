using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuanLyTaiChinhCaNhan_Nhom06.Data;
using QuanLyTaiChinhCaNhan_Nhom06.Services;
using QuanLyTaiChinhCaNhan_Nhom06.ViewModels;
using QuanLyTaiChinhCaNhan_Nhom06.Helpers;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace QuanLyTaiChinhCaNhan_Nhom06
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            services.AddDbContextFactory<ExpenseDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("Connection")));

            services.AddSingleton<ISessionContext, SessionContext>();
            services.AddSingleton<IAuthenticationService, AuthService>();
            services.AddSingleton<IDataService, DataService>();
            services.AddSingleton<IReportService, ReportService>();
            services.AddSingleton<ICategoryService, CategoryService>();
            services.AddSingleton<IBudgetService, BudgetService>();
            services.AddSingleton<IGoalService, GoalService>();
            services.AddSingleton<ITransactionService, TransactionService>();
            services.AddSingleton<IAppearanceService, AppearanceService>();

            services.AddSingleton<MainViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<RegisterViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<TransactionViewModel>();
            services.AddTransient<CategoryViewModel>();
            services.AddTransient<BudgetViewModel>();
            services.AddTransient<GoalViewModel>();
            services.AddTransient<ProfileViewModel>();
            services.AddTransient<ReportViewModel>();
            services.AddTransient<MainWindow>();

            ServiceProvider = services.BuildServiceProvider();
            ServiceProvider.GetRequiredService<IAppearanceService>().ApplyCurrentSettings();

            try
            {
                var factory = ServiceProvider.GetRequiredService<IDbContextFactory<ExpenseDbContext>>();
                await using var context = await factory.CreateDbContextAsync();
                await context.Database.MigrateAsync();

            }
            catch (Exception ex)
            {
                AppLogger.Log(ex);
                DialogHelper.Error(DialogHelper.Text("DatabaseConnectionError"));
                Shutdown();
                return;
            }

            var mainViewModel = ServiceProvider.GetRequiredService<MainViewModel>();
            mainViewModel.ShowLogin();
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            AppLogger.Log(e.Exception);
            DialogHelper.Error(DialogHelper.Format("UnhandledUiErrorFormat", e.Exception.Message));
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
                AppLogger.Log(exception);
            else
                AppLogger.Log(e.ExceptionObject?.ToString() ?? "Unhandled exception.");
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            AppLogger.Log(e.Exception);
            e.SetObserved();
        }
    }
}

