using System.Windows.Input;
using QuanLyTaiChinhCaNhan_Nhom06.Commands;
using QuanLyTaiChinhCaNhan_Nhom06.Helpers;
using QuanLyTaiChinhCaNhan_Nhom06.Models;
using QuanLyTaiChinhCaNhan_Nhom06.Services;

namespace QuanLyTaiChinhCaNhan_Nhom06.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAuthenticationService _authService;
        private readonly IAppearanceService _appearanceService;
        private ViewModelBase _currentViewModel;
        private bool _isAuthenticated;
        private bool _isDarkMode;
        private string _currentTarget = "Login";
        private string _title = string.Empty;

        public MainViewModel(
            IServiceProvider serviceProvider,
            IAuthenticationService authService,
            IAppearanceService appearanceService)
        {
            _serviceProvider = serviceProvider;
            _authService = authService;
            _appearanceService = appearanceService;
            _currentViewModel = new PlaceholderViewModel();
            _isDarkMode = appearanceService.Settings.IsDarkMode;
            NavigateCommand = new RelayCommand(p => Navigate(p?.ToString() ?? "Dashboard"), _ => IsAuthenticated);
            LogoutCommand = new RelayCommand(_ => Logout(), _ => IsAuthenticated);
            ToggleThemeCommand = new RelayCommand(_ => IsDarkMode = !IsDarkMode);
            _appearanceService.SettingsChanged += (_, _) =>
            {
                UpdateTitle();
                CurrentViewModel.RefreshLocalization();
                OnPropertyChanged(nameof(CurrentUserDisplayName));
            };
            UpdateTitle();
        }

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set
            {
                if (SetProperty(ref _isAuthenticated, value))
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (!SetProperty(ref _isDarkMode, value))
                    return;

                _appearanceService.SetDarkMode(value);
                _appearanceService.Save();
            }
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public User? CurrentUser => _authService.CurrentUser;
        public string CurrentUserDisplayName => string.IsNullOrWhiteSpace(CurrentUser?.FullName) ? _appearanceService.T("UserFallbackText") : CurrentUser!.FullName;

        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public void ShowLogin()
        {
            IsAuthenticated = false;
            _currentTarget = "Login";
            UpdateTitle();
            CurrentViewModel = Create<LoginViewModel>();
        }

        public void ShowRegister()
        {
            IsAuthenticated = false;
            _currentTarget = "Register";
            UpdateTitle();
            CurrentViewModel = Create<RegisterViewModel>();
        }

        public void OnLoginSuccess()
        {
            IsAuthenticated = true;
            OnPropertyChanged(nameof(CurrentUser));
            OnPropertyChanged(nameof(CurrentUserDisplayName));
            Navigate("Dashboard");
        }

        private void Navigate(string target)
        {
            try
            {
                if (!IsAuthenticated && target != "Login" && target != "Register")
                    return;

                CurrentViewModel = target switch
                {
                    "Transactions" => Create<TransactionViewModel>(),
                    "Categories" => Create<CategoryViewModel>(),
                    "Budgets" => Create<BudgetViewModel>(),
                    "Goals" => Create<GoalViewModel>(),
                    "Reports" => Create<ReportViewModel>(),
                    "Profile" => Create<ProfileViewModel>(),
                    _ => Create<DashboardViewModel>()
                };

                _currentTarget = target;
                UpdateTitle();
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex);
                DialogHelper.Error(_appearanceService.Format("OpenScreenErrorFormat", target, ex.Message));
                CurrentViewModel = new PlaceholderViewModel();
                _currentTarget = "Error";
                UpdateTitle();
            }
        }

        private void Logout()
        {
            _authService.Logout();
            ShowLogin();
        }

        private T Create<T>() where T : ViewModelBase
        {
            return (T)_serviceProvider.GetService(typeof(T))!;
        }

        private void UpdateTitle()
        {
            Title = _appearanceService.T(_currentTarget switch
            {
                "Login" => "PageLoginTitle",
                "Register" => "PageRegisterTitle",
                "Transactions" => "PageTransactionsTitle",
                "Categories" => "PageCategoriesTitle",
                "Budgets" => "PageBudgetsTitle",
                "Goals" => "PageGoalsTitle",
                "Reports" => "PageReportsTitle",
                "Profile" => "PageProfileTitle",
                "Error" => "PageErrorTitle",
                _ => "PageDashboardTitle"
            });
        }

        private sealed class PlaceholderViewModel : ViewModelBase
        {
        }
    }
}



