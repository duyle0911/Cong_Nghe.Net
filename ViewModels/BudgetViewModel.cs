using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using QuanLyTaiChinhCaNhan_Nhom06.Commands;
using QuanLyTaiChinhCaNhan_Nhom06.Helpers;
using QuanLyTaiChinhCaNhan_Nhom06.Models;
using QuanLyTaiChinhCaNhan_Nhom06.Services;

namespace QuanLyTaiChinhCaNhan_Nhom06.ViewModels
{
    public class BudgetViewModel : ViewModelBase
    {
        private readonly IBudgetService _budgetService;
        private readonly ICategoryService _categoryService;
        private readonly IAppearanceService _appearanceService;
        private readonly ISessionContext _sessionContext;
        private Budget? _selectedBudget;
        private Category? _selectedCategory;
        private decimal _amount;
        private DateTime _startDate = DateTimeHelper.StartOfMonth(DateTime.Now);
        private DateTime _endDate = DateTimeHelper.EndOfMonth(DateTime.Now);
        private bool _isEditorOpen;
        private bool _hasBudgets;
        private string _editorTitle = string.Empty;
        private string _totalBudgetText = "0 ₫";
        private string _totalSpentText = "0 ₫";
        private string _remainingText = "0 ₫";
        private string _spentRateText = string.Empty;

        public BudgetViewModel(IBudgetService budgetService, ICategoryService categoryService, IAppearanceService appearanceService, ISessionContext sessionContext)
        {
            _budgetService = budgetService;
            _categoryService = categoryService;
            _appearanceService = appearanceService;
            _sessionContext = sessionContext;
            EditorTitle = _appearanceService.T("CreateBudgetTitle");
            SpentRateText = _appearanceService.Format("BudgetUsedFormat", 0);
            Budgets = new ObservableCollection<Budget>();
            BudgetOverviews = new ObservableCollection<BudgetOverview>();
            Categories = new ObservableCollection<Category>();
            SaveCommand = new AsyncRelayCommand(_ => SaveAsync());
            DeleteCommand = new AsyncRelayCommand(_ => DeleteAsync(), _ => SelectedBudget != null);
            NewCommand = new RelayCommand(_ => OpenNewEditor());
            CancelCommand = new RelayCommand(_ => CloseEditor());
            EditCommand = new RelayCommand(item => OpenEditor((item as BudgetOverview)?.Budget));
            _ = RunSafeAsync(LoadAsync);
        }

        public ObservableCollection<Budget> Budgets { get; }
        public ObservableCollection<BudgetOverview> BudgetOverviews { get; }
        public ObservableCollection<Category> Categories { get; }

        public Budget? SelectedBudget
        {
            get => _selectedBudget;
            set
            {
                if (!SetProperty(ref _selectedBudget, value) || value == null)
                    return;

                SelectedCategory = Categories.FirstOrDefault(category => category.Id == value.CategoryId);
                Amount = value.Amount;
                StartDate = value.StartDate;
                EndDate = value.EndDate;
            }
        }

        public Category? SelectedCategory { get => _selectedCategory; set => SetProperty(ref _selectedCategory, value); }
        public decimal Amount { get => _amount; set => SetProperty(ref _amount, value); }
        public DateTime StartDate { get => _startDate; set => SetProperty(ref _startDate, value); }
        public DateTime EndDate { get => _endDate; set => SetProperty(ref _endDate, value); }
        public bool IsEditorOpen { get => _isEditorOpen; set => SetProperty(ref _isEditorOpen, value); }
        public bool HasBudgets { get => _hasBudgets; set => SetProperty(ref _hasBudgets, value); }
        public string EditorTitle { get => _editorTitle; set => SetProperty(ref _editorTitle, value); }
        public string TotalBudgetText { get => _totalBudgetText; set => SetProperty(ref _totalBudgetText, value); }
        public string TotalSpentText { get => _totalSpentText; set => SetProperty(ref _totalSpentText, value); }
        public string RemainingText { get => _remainingText; set => SetProperty(ref _remainingText, value); }
        public string SpentRateText { get => _spentRateText; set => SetProperty(ref _spentRateText, value); }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand EditCommand { get; }

        public override void RefreshLocalization()
        {
            _ = RunSafeAsync(LoadAsync);
        }

        private async Task LoadAsync()
        {
            var userId = _sessionContext.CurrentUserId ?? 0;

            Categories.Clear();
            foreach (var item in await _categoryService.GetCategoriesAsync(userId, TransactionType.Expense))
            {
                item.DisplayName = _appearanceService.LocalizeCategoryName(item.Name, item.Type);
                Categories.Add(item);
            }

            Budgets.Clear();
            BudgetOverviews.Clear();
            foreach (var item in await _budgetService.GetBudgetsAsync(userId))
            {
                Budgets.Add(item);
                BudgetOverviews.Add(new BudgetOverview(item, _appearanceService));
            }

            HasBudgets = BudgetOverviews.Any();
            var totalBudget = Budgets.Where(item => item.IsActive).Sum(item => item.Amount);
            var totalSpent = Budgets.Where(item => item.IsActive).Sum(item => item.SpentAmount);
            TotalBudgetText = DashboardPresentation.FormatMoney(totalBudget);
            TotalSpentText = DashboardPresentation.FormatMoney(totalSpent);
            RemainingText = DashboardPresentation.FormatMoney(totalBudget - totalSpent);
            SpentRateText = _appearanceService.Format("BudgetUsedFormat", totalBudget > 0 ? totalSpent / totalBudget * 100 : 0);
        }

        private async Task SaveAsync()
        {
            if (SelectedCategory == null || Amount <= 0 || StartDate > EndDate)
            {
                DialogHelper.Error(_appearanceService.T("BudgetInputInvalid"));
                return;
            }

            var userId = _sessionContext.CurrentUserId ?? 0;
            var ok = SelectedBudget == null
                ? await _budgetService.CreateBudgetAsync(new Budget
                {
                    Name = SelectedCategory.Name,
                    CategoryId = SelectedCategory.Id,
                    Amount = Amount,
                    StartDate = StartDate,
                    EndDate = EndDate
                }, userId)
                : await _budgetService.UpdateBudgetAsync(SelectedBudget.Id, Amount, StartDate, EndDate, true, userId);

            if (!ok)
            {
                DialogHelper.Error(_appearanceService.T("BudgetSaveFailed"));
                return;
            }

            CloseEditor();
            await LoadAsync();
        }

        private async Task DeleteAsync()
        {
            if (SelectedBudget == null || !DialogHelper.Confirm(_appearanceService.T("BudgetDeleteConfirm")))
                return;

            var result = await _budgetService.DeleteBudgetAsync(SelectedBudget.Id, _sessionContext.CurrentUserId ?? 0);
            if (!result.Success)
            {
                DialogHelper.Error(result.Message);
                return;
            }

            CloseEditor();
            await LoadAsync();
        }

        private void OpenNewEditor()
        {
            ClearForm();
            EditorTitle = _appearanceService.T("CreateBudgetTitle");
            IsEditorOpen = true;
        }

        private void OpenEditor(Budget? budget)
        {
            if (budget == null)
                return;

            SelectedBudget = budget;
            EditorTitle = _appearanceService.T("EditBudgetTitle");
            IsEditorOpen = true;
        }

        private void CloseEditor()
        {
            ClearForm();
            IsEditorOpen = false;
        }

        private void ClearForm()
        {
            SelectedBudget = null;
            SelectedCategory = null;
            Amount = 0;
            StartDate = DateTimeHelper.StartOfMonth(DateTime.Now);
            EndDate = DateTimeHelper.EndOfMonth(DateTime.Now);
        }
    }

    public class BudgetOverview
    {
        public BudgetOverview(Budget budget, IAppearanceService appearanceService)
        {
            Budget = budget;
            Name = budget.Category == null ? budget.Name : appearanceService.LocalizeCategoryName(budget.Category.Name, budget.Category.Type);
            AmountText = DashboardPresentation.FormatMoney(budget.Amount);
            SpentText = DashboardPresentation.FormatMoney(budget.SpentAmount);
            RemainingText = DashboardPresentation.FormatMoney(budget.RemainingAmount);
            UsedPercent = budget.UsedPercent;
            ProgressText = appearanceService.Format("UsedPercentFormat", UsedPercent);
            PeriodText = $"{budget.StartDate.ToString("d", System.Globalization.CultureInfo.CurrentCulture)} - {budget.EndDate.ToString("d", System.Globalization.CultureInfo.CurrentCulture)}";
            var color = budget.Category?.Color ?? "#06B6D4";
            AccentBrush = DashboardPresentation.CreateBrush(color);
            ProgressBrush = DashboardPresentation.CreateBrush(
                UsedPercent >= 100 ? "#EF4444" : UsedPercent >= 80 ? "#F59E0B" : color);
            StatusText = UsedPercent >= 100 ? appearanceService.T("OverBudgetStatus") : UsedPercent >= 80 ? appearanceService.T("NeedsAttentionStatus") : appearanceService.T("GoodStatus");
            StatusBrush = DashboardPresentation.CreateBrush(
                UsedPercent >= 100 ? "#EF4444" : UsedPercent >= 80 ? "#D97706" : "#059669");
        }

        public Budget Budget { get; }
        public string Name { get; }
        public string AmountText { get; }
        public string SpentText { get; }
        public string RemainingText { get; }
        public decimal UsedPercent { get; }
        public string ProgressText { get; }
        public string PeriodText { get; }
        public string StatusText { get; }
        public Brush AccentBrush { get; }
        public Brush ProgressBrush { get; }
        public Brush StatusBrush { get; }
    }
}


