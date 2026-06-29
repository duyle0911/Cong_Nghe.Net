using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using QuanLyTaiChinhCaNhan_Nhom06.Commands;
using QuanLyTaiChinhCaNhan_Nhom06.Helpers;
using QuanLyTaiChinhCaNhan_Nhom06.Models;
using QuanLyTaiChinhCaNhan_Nhom06.Services;

namespace QuanLyTaiChinhCaNhan_Nhom06.ViewModels
{
    public class CategoryViewModel : ViewModelBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IDataService _dataService;
        private readonly IBudgetService _budgetService;
        private readonly IAppearanceService _appearanceService;
        private readonly ISessionContext _sessionContext;
        private readonly AsyncRelayCommand _deleteCommand;
        private Category? _selectedCategory;
        private CategoryOverview? _selectedOverview;
        private string _name = string.Empty;
        private string _color = "#0EA5E9";
        private bool _isSyncingColorChannels;
        private double _colorRed = 14;
        private double _colorGreen = 165;
        private double _colorBlue = 233;
        private string _icon = "Category";
        private TransactionType _type = TransactionType.Expense;
        private bool _isEditorOpen;
        private bool _hasCategories;
        private string _editorTitle = string.Empty;

        public CategoryViewModel(
            ICategoryService categoryService,
            IDataService dataService,
            IBudgetService budgetService,
            IAppearanceService appearanceService,
            ISessionContext sessionContext)
        {
            _categoryService = categoryService;
            _dataService = dataService;
            _budgetService = budgetService;
            _appearanceService = appearanceService;
            _sessionContext = sessionContext;
            EditorTitle = _appearanceService.T("AddCategoryTitle");

            Categories = new ObservableCollection<Category>();
            CategoryOverviews = new ObservableCollection<CategoryOverview>();
            TypeOptions = new ObservableCollection<TransactionTypeOption>();
            RefreshTypeOptions();

            SaveCommand = new AsyncRelayCommand(_ => SaveAsync());
            _deleteCommand = new AsyncRelayCommand(_ => DeleteAsync(), _ => SelectedCategory != null);
            DeleteCommand = _deleteCommand;
            NewCommand = new RelayCommand(_ => OpenNewEditor());
            CancelCommand = new RelayCommand(_ => CloseEditor());
            EditCommand = new RelayCommand(category => OpenEditor(category as Category));
            _ = RunSafeAsync(LoadAsync);
        }

        public ObservableCollection<Category> Categories { get; }
        public ObservableCollection<CategoryOverview> CategoryOverviews { get; }
        public ObservableCollection<TransactionTypeOption> TypeOptions { get; }
        public System.Collections.Generic.IReadOnlyList<ColorPaletteOption> ColorPaletteOptions => ColorPalette.Options;

        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (!SetProperty(ref _selectedCategory, value))
                    return;

                _deleteCommand.RaiseCanExecuteChanged();

                if (value == null)
                    return;

                Name = value.Name;
                Color = value.Color;
                Icon = value.Icon;
                Type = value.Type;
            }
        }

        public CategoryOverview? SelectedOverview
        {
            get => _selectedOverview;
            set
            {
                if (!SetProperty(ref _selectedOverview, value) || value == null)
                    return;

                OpenEditor(value.Category);
            }
        }

        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public string Color
        {
            get => _color;
            set
            {
                var normalized = ColorPalette.Normalize(value, "#0EA5E9");
                if (!SetProperty(ref _color, normalized))
                    return;

                SyncColorChannels(normalized);
                OnPropertyChanged(nameof(ColorPreviewBrush));
                OnPropertyChanged(nameof(SelectedPaletteColor));
            }
        }

        public string? SelectedPaletteColor
        {
            get => ColorPalette.IsPaletteColor(Color) ? Color : null;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    Color = value;
            }
        }

        public Brush ColorPreviewBrush => ColorPalette.CreateBrush(Color, "#0EA5E9");

        public double ColorRed
        {
            get => _colorRed;
            set
            {
                if (SetProperty(ref _colorRed, value))
                    UpdateColorFromChannels();
            }
        }

        public double ColorGreen
        {
            get => _colorGreen;
            set
            {
                if (SetProperty(ref _colorGreen, value))
                    UpdateColorFromChannels();
            }
        }

        public double ColorBlue
        {
            get => _colorBlue;
            set
            {
                if (SetProperty(ref _colorBlue, value))
                    UpdateColorFromChannels();
            }
        }
        public string Icon { get => _icon; set => SetProperty(ref _icon, value); }
        public TransactionType Type { get => _type; set => SetProperty(ref _type, value); }
        public bool IsEditorOpen { get => _isEditorOpen; set => SetProperty(ref _isEditorOpen, value); }
        public bool HasCategories { get => _hasCategories; set => SetProperty(ref _hasCategories, value); }
        public string EditorTitle { get => _editorTitle; set => SetProperty(ref _editorTitle, value); }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand EditCommand { get; }

        private void UpdateColorFromChannels()
        {
            if (_isSyncingColorChannels)
                return;

            Color = ColorPalette.ToHex(ColorRed, ColorGreen, ColorBlue);
        }

        private void SyncColorChannels(string? value)
        {
            if (!ColorPalette.TryParseColor(value, out var parsed))
                return;

            _isSyncingColorChannels = true;
            ColorRed = parsed.R;
            ColorGreen = parsed.G;
            ColorBlue = parsed.B;
            _isSyncingColorChannels = false;
        }
        public override void RefreshLocalization()
        {
            RefreshTypeOptions();
            _ = RunSafeAsync(LoadAsync);
        }

        private void RefreshTypeOptions()
        {
            TypeOptions.Clear();
            TypeOptions.Add(new TransactionTypeOption(TransactionType.Income, _appearanceService.LocalizeTransactionType(TransactionType.Income)));
            TypeOptions.Add(new TransactionTypeOption(TransactionType.Expense, _appearanceService.LocalizeTransactionType(TransactionType.Expense)));
        }
        private async Task LoadAsync()
        {
            var userId = _sessionContext.CurrentUserId ?? 0;
            var now = DateTime.Now;
            var categoriesTask = _categoryService.GetCategoriesAsync(userId);
            var transactionsTask = _dataService.GetTransactionsByPeriodAsync(
                userId,
                DateTimeHelper.StartOfMonth(now),
                DateTimeHelper.EndOfMonth(now));
            var budgetsTask = _budgetService.GetBudgetsAsync(userId);

            await Task.WhenAll(categoriesTask, transactionsTask, budgetsTask);

            Categories.Clear();
            CategoryOverviews.Clear();

            foreach (var category in categoriesTask.Result)
            {
                category.DisplayName = _appearanceService.LocalizeCategoryName(category.Name, category.Type);
                Categories.Add(category);
                var transactions = transactionsTask.Result.Where(transaction => transaction.CategoryId == category.Id);
                var budget = budgetsTask.Result.FirstOrDefault(item =>
                    item.CategoryId == category.Id
                    && item.IsActive
                    && now.Date >= item.StartDate.Date
                    && now.Date <= item.EndDate.Date);

                CategoryOverviews.Add(new CategoryOverview(category, transactions, budget, _appearanceService));
            }

            HasCategories = CategoryOverviews.Any();
        }

        private async Task SaveAsync()
        {
            if (!Validator.Required(Name))
            {
                DialogHelper.Error(_appearanceService.T("CategoryNameRequired"));
                return;
            }

            if (!CategoryOverview.IsValidColor(Color))
            {
                DialogHelper.Error(_appearanceService.T("InvalidColorMessage"));
                return;
            }

            var userId = _sessionContext.CurrentUserId ?? 0;
            var ok = SelectedCategory == null
                ? await _categoryService.CreateCategoryAsync(new Category { Name = Name, Color = Color, Icon = Icon, Type = Type }, userId)
                : await _categoryService.UpdateCategoryAsync(SelectedCategory.Id, Name, Type, Color, Icon, userId);

            if (!ok)
            {
                DialogHelper.Error(_appearanceService.T("CategorySaveFailed"));
                return;
            }

            CloseEditor();
            await LoadAsync();
        }

        private async Task DeleteAsync()
        {
            if (SelectedCategory == null || !DialogHelper.Confirm(_appearanceService.Format("DeleteCategoryConfirmFormat", SelectedCategory.Name)))
                return;

            var result = await _categoryService.DeleteCategoryAsync(SelectedCategory.Id, _sessionContext.CurrentUserId ?? 0);
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
            EditorTitle = _appearanceService.T("AddCategoryTitle");
            IsEditorOpen = true;
        }

        private void OpenEditor(Category? category)
        {
            if (category == null)
                return;

            SelectedCategory = category;
            EditorTitle = _appearanceService.T("EditCategoryTitle");
            IsEditorOpen = true;
        }

        private void CloseEditor()
        {
            ClearForm();
            IsEditorOpen = false;
        }

        private void ClearForm()
        {
            SelectedOverview = null;
            SelectedCategory = null;
            Name = string.Empty;
            Color = "#0EA5E9";
            Icon = "Category";
            Type = TransactionType.Expense;
        }
    }

    public class CategoryOverview
    {
        private static readonly Brush DefaultAccentBrush = CreateBrush("#0EA5E9");
        private static readonly Brush DefaultSoftAccentBrush = CreateBrush("#200EA5E9");
        private readonly IAppearanceService _appearanceService;
        private readonly string _displayName;

        public CategoryOverview(Category category, IEnumerable<Transaction> transactions, Budget? budget, IAppearanceService appearanceService)
        {
            Category = category;
            _appearanceService = appearanceService;
            _displayName = appearanceService.LocalizeCategoryName(category.Name, category.Type);
            var monthlyTransactions = transactions.ToList();
            Budget = budget;
            TransactionCount = monthlyTransactions.Count;
            TotalAmount = monthlyTransactions.Sum(transaction => transaction.Amount);
            var accentColor = GetAccentColor(category);
            AccentBrush = CreateBrush(accentColor, DefaultAccentBrush);
            SoftAccentBrush = CreateBrush(WithAlpha(accentColor, "20"), DefaultSoftAccentBrush);
            IconKind = Enum.TryParse<PackIconKind>(category.Icon, true, out var iconKind)
                ? iconKind
                : PackIconKind.Shape;
        }

        public Category Category { get; }
        public Budget? Budget { get; }
        public string Name => _displayName;
        public string TypeLabel => _appearanceService.LocalizeTransactionType(Category.Type);
        public string TotalLabel => Category.Type == TransactionType.Expense ? _appearanceService.T("TotalSpentLabel") : _appearanceService.T("TotalIncomeLabel");
        public decimal TotalAmount { get; }
        public string TotalAmountText => FormatMoney(TotalAmount);
        public int TransactionCount { get; }
        public decimal BudgetAmount => Budget?.Amount ?? 0;
        public string BudgetAmountText => Budget == null ? _appearanceService.T("NotSetText") : FormatMoney(BudgetAmount);
        public decimal RemainingAmount => BudgetAmount - TotalAmount;
        public string RemainingAmountText => Budget == null ? "-" : FormatMoney(RemainingAmount);
        public decimal ProgressPercent => BudgetAmount > 0 ? Math.Min(100, TotalAmount / BudgetAmount * 100) : 0;
        public string ProgressText => Budget == null ? "-" : $"{ProgressPercent:N0}%";
        public PackIconKind IconKind { get; }
        public Brush AccentBrush { get; }
        public Brush SoftAccentBrush { get; }

        public string StatusText
        {
            get
            {
                if (Category.Type == TransactionType.Income)
                    return _appearanceService.T("IncomeItemStatus");
                if (Budget == null)
                    return _appearanceService.T("NoBudgetStatus");
                if (TotalAmount >= BudgetAmount)
                    return _appearanceService.T("OverBudgetStatus");
                if (TotalAmount >= BudgetAmount * 0.8m)
                    return _appearanceService.T("NeedsAttentionStatus");
                return _appearanceService.T("StableStatus");
            }
        }

        public Brush StatusBackground
        {
            get
            {
                if (Category.Type == TransactionType.Income)
                    return CreateBrush("#DCFCE7");
                if (Budget == null)
                    return CreateBrush("#E2E8F0");
                if (TotalAmount >= BudgetAmount)
                    return CreateBrush("#FEE2E2");
                if (TotalAmount >= BudgetAmount * 0.8m)
                    return CreateBrush("#FEF3C7");
                return CreateBrush("#CCFBF1");
            }
        }

        public Brush StatusForeground
        {
            get
            {
                if (Category.Type == TransactionType.Income)
                    return CreateBrush("#15803D");
                if (Budget == null)
                    return CreateBrush("#475569");
                if (TotalAmount >= BudgetAmount)
                    return CreateBrush("#DC2626");
                if (TotalAmount >= BudgetAmount * 0.8m)
                    return CreateBrush("#A16207");
                return CreateBrush("#0F766E");
            }
        }

        public static bool IsValidColor(string? value)
        {
            return value?.Length == 7
                && value.StartsWith('#')
                && int.TryParse(value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
        }

        private static string FormatMoney(decimal amount)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0:N0} ₫", amount);
        }

        private static string WithAlpha(string? color, string alpha)
        {
            return IsValidColor(color) ? $"#{alpha}{color![1..]}" : "#200EA5E9";
        }

        private static string GetAccentColor(Category category)
        {
            return (category.Icon, category.Color.ToUpperInvariant()) switch
            {
                ("Restaurant", "#EF4444") => "#FF4B16",
                ("Shopping", "#EC4899") => "#D842CD",
                ("DirectionsCar", "#F97316") => "#009EF7",
                ("Movie", "#8B5CF6") => "#7B48F6",
                ("MedicalBag", "#06B6D4") => "#FF3377",
                ("Receipt", "#64748B") => "#00C875",
                _ => category.Color
            };
        }

        private static Brush CreateBrush(string color, Brush? fallback = null)
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            }
            catch
            {
                return fallback ?? Brushes.Transparent;
            }
        }
    }
}



