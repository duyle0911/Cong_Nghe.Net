using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;
using QuanLyTaiChinhCaNhan_Nhom06.Commands;
using QuanLyTaiChinhCaNhan_Nhom06.Helpers;
using QuanLyTaiChinhCaNhan_Nhom06.Models;
using QuanLyTaiChinhCaNhan_Nhom06.Services;

namespace QuanLyTaiChinhCaNhan_Nhom06.ViewModels
{
    public class TransactionViewModel : ViewModelBase
    {
        private readonly ITransactionService _transactionService;
        private readonly ICategoryService _categoryService;
        private readonly IAppearanceService _appearanceService;
        private readonly ISessionContext _sessionContext;
        private readonly List<Transaction> _loadedTransactions = new();
        private Transaction? _selectedTransaction;
        private Category? _selectedCategory;
        private string _description = string.Empty;
        private decimal _amount;
        private DateTime _date = DateTime.Now;
        private TransactionType _type = TransactionType.Expense;
        private string _searchText = string.Empty;
        private string _selectedFilter = "All";
        private bool _hasTransactions;
        private bool _isEditorOpen;
        private bool _isQuickCategoryOpen;
        private string _newCategoryName = string.Empty;
        private string _newCategoryColor = "#0EA5E9";
        private bool _isSyncingNewCategoryColorChannels;
        private double _newCategoryColorRed = 14;
        private double _newCategoryColorGreen = 165;
        private double _newCategoryColorBlue = 233;
        private string _newCategoryIcon = "Shape";
        private string _editorTitle = string.Empty;
        private string _filteredCountText = string.Empty;

        public TransactionViewModel(
            ITransactionService transactionService,
            ICategoryService categoryService,
            IAppearanceService appearanceService,
            ISessionContext sessionContext)
        {
            _transactionService = transactionService;
            _categoryService = categoryService;
            _appearanceService = appearanceService;
            _sessionContext = sessionContext;
            EditorTitle = _appearanceService.T("AddTransactionTitle");
            FilteredCountText = _appearanceService.Format("FilteredCountFormat", 0, 0);

            Transactions = new ObservableCollection<Transaction>();
            TransactionItems = new ObservableCollection<TransactionListItem>();
            Categories = new ObservableCollection<Category>();
            TypeOptions = new ObservableCollection<TransactionTypeOption>();
            RefreshTypeOptions();

            SaveCommand = new AsyncRelayCommand(_ => SaveAsync());
            DeleteCommand = new AsyncRelayCommand(_ => DeleteSelectedAsync(), _ => SelectedTransaction != null);
            DeleteItemCommand = new AsyncRelayCommand(item => DeleteItemAsync(item as TransactionListItem));
            EditCommand = new AsyncRelayCommand(item => OpenEditorAsync((item as TransactionListItem)?.Transaction));
            NewCommand = new RelayCommand(_ => OpenNewEditor());
            CancelCommand = new RelayCommand(_ => CloseEditor());
            SelectFilterCommand = new RelayCommand(filter => SelectFilter(filter?.ToString()));
            OpenQuickCategoryCommand = new RelayCommand(_ => OpenQuickCategoryEditor());
            CancelQuickCategoryCommand = new RelayCommand(_ => CloseQuickCategoryEditor());
            CreateQuickCategoryCommand = new AsyncRelayCommand(_ => CreateQuickCategoryAsync());

            _ = RunSafeAsync(LoadAsync);
        }

        public ObservableCollection<Transaction> Transactions { get; }
        public ObservableCollection<TransactionListItem> TransactionItems { get; }
        public ObservableCollection<Category> Categories { get; }
        public ObservableCollection<TransactionTypeOption> TypeOptions { get; }
        public System.Collections.Generic.IReadOnlyList<ColorPaletteOption> ColorPaletteOptions => ColorPalette.Options;

        public Transaction? SelectedTransaction
        {
            get => _selectedTransaction;
            set => SetProperty(ref _selectedTransaction, value);
        }

        public Category? SelectedCategory { get => _selectedCategory; set => SetProperty(ref _selectedCategory, value); }
        public string Description { get => _description; set => SetProperty(ref _description, value); }
        public decimal Amount { get => _amount; set => SetProperty(ref _amount, value); }
        public DateTime Date { get => _date; set => SetProperty(ref _date, value); }
        public bool HasTransactions { get => _hasTransactions; set => SetProperty(ref _hasTransactions, value); }
        public bool IsEditorOpen { get => _isEditorOpen; set => SetProperty(ref _isEditorOpen, value); }
        public bool IsQuickCategoryOpen { get => _isQuickCategoryOpen; set => SetProperty(ref _isQuickCategoryOpen, value); }
        public string NewCategoryName { get => _newCategoryName; set => SetProperty(ref _newCategoryName, value); }
        public string NewCategoryColor
        {
            get => _newCategoryColor;
            set
            {
                var normalized = ColorPalette.Normalize(value, "#0EA5E9");
                if (!SetProperty(ref _newCategoryColor, normalized))
                    return;

                SyncNewCategoryColorChannels(normalized);
                OnPropertyChanged(nameof(NewCategoryColorPreviewBrush));
                OnPropertyChanged(nameof(SelectedNewCategoryPaletteColor));
            }
        }

        public string? SelectedNewCategoryPaletteColor
        {
            get => ColorPalette.IsPaletteColor(NewCategoryColor) ? NewCategoryColor : null;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    NewCategoryColor = value;
            }
        }

        public Brush NewCategoryColorPreviewBrush => ColorPalette.CreateBrush(NewCategoryColor, "#0EA5E9");

        public double NewCategoryColorRed
        {
            get => _newCategoryColorRed;
            set
            {
                if (SetProperty(ref _newCategoryColorRed, value))
                    UpdateNewCategoryColorFromChannels();
            }
        }

        public double NewCategoryColorGreen
        {
            get => _newCategoryColorGreen;
            set
            {
                if (SetProperty(ref _newCategoryColorGreen, value))
                    UpdateNewCategoryColorFromChannels();
            }
        }

        public double NewCategoryColorBlue
        {
            get => _newCategoryColorBlue;
            set
            {
                if (SetProperty(ref _newCategoryColorBlue, value))
                    UpdateNewCategoryColorFromChannels();
            }
        }
        public string NewCategoryIcon { get => _newCategoryIcon; set => SetProperty(ref _newCategoryIcon, value); }
        public string EditorTitle { get => _editorTitle; set => SetProperty(ref _editorTitle, value); }
        public string FilteredCountText { get => _filteredCountText; set => SetProperty(ref _filteredCountText, value); }
        public bool IsAllFilterSelected => SelectedFilter == "All";
        public bool IsIncomeFilterSelected => SelectedFilter == "Income";
        public bool IsExpenseFilterSelected => SelectedFilter == "Expense";

        public TransactionType Type
        {
            get => _type;
            set
            {
                if (SetProperty(ref _type, value))
                    _ = RunSafeAsync(() => LoadCategoriesAsync());
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplyFilters();
            }
        }

        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (!SetProperty(ref _selectedFilter, value))
                    return;

                OnPropertyChanged(nameof(IsAllFilterSelected));
                OnPropertyChanged(nameof(IsIncomeFilterSelected));
                OnPropertyChanged(nameof(IsExpenseFilterSelected));
                ApplyFilters();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SelectFilterCommand { get; }
        public ICommand OpenQuickCategoryCommand { get; }
        public ICommand CancelQuickCategoryCommand { get; }
        public ICommand CreateQuickCategoryCommand { get; }

        private void UpdateNewCategoryColorFromChannels()
        {
            if (_isSyncingNewCategoryColorChannels)
                return;

            NewCategoryColor = ColorPalette.ToHex(NewCategoryColorRed, NewCategoryColorGreen, NewCategoryColorBlue);
        }

        private void SyncNewCategoryColorChannels(string? value)
        {
            if (!ColorPalette.TryParseColor(value, out var parsed))
                return;

            _isSyncingNewCategoryColorChannels = true;
            NewCategoryColorRed = parsed.R;
            NewCategoryColorGreen = parsed.G;
            NewCategoryColorBlue = parsed.B;
            _isSyncingNewCategoryColorChannels = false;
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
            await LoadCategoriesAsync();

            _loadedTransactions.Clear();
            _loadedTransactions.AddRange(await _transactionService.GetTransactionsAsync(_sessionContext.CurrentUserId ?? 0));
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            IEnumerable<Transaction> items = _loadedTransactions;
            items = SelectedFilter switch
            {
                "Income" => items.Where(transaction => transaction.Type == TransactionType.Income),
                "Expense" => items.Where(transaction => transaction.Type == TransactionType.Expense),
                _ => items
            };

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                items = items.Where(transaction =>
                    transaction.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                    || (transaction.Category?.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            Transactions.Clear();
            TransactionItems.Clear();
            foreach (var transaction in items)
            {
                Transactions.Add(transaction);
                TransactionItems.Add(new TransactionListItem(transaction, _appearanceService));
            }

            HasTransactions = TransactionItems.Any();
            FilteredCountText = _appearanceService.Format("FilteredCountFormat", TransactionItems.Count, _loadedTransactions.Count);
        }

        private async Task LoadCategoriesAsync(int? selectedCategoryId = null)
        {
            var categoryId = selectedCategoryId ?? SelectedCategory?.Id;
            Categories.Clear();
            foreach (var category in await _categoryService.GetCategoriesAsync(_sessionContext.CurrentUserId ?? 0, Type))
            {
                category.DisplayName = _appearanceService.LocalizeCategoryName(category.Name, category.Type);
                Categories.Add(category);
            }
            SelectedCategory = Categories.FirstOrDefault(category => category.Id == categoryId);
        }

        private async Task SaveAsync()
        {
            if (!Validator.Required(Description))
            {
                DialogHelper.Error(_appearanceService.T("TransactionDescriptionRequired"));
                return;
            }

            if (Amount <= 0)
            {
                DialogHelper.Error(_appearanceService.T("AmountGreaterThanZero"));
                return;
            }

            if (SelectedCategory == null)
            {
                DialogHelper.Error(_appearanceService.T("CategoryRequired"));
                return;
            }

            var userId = _sessionContext.CurrentUserId ?? 0;
            var result = SelectedTransaction == null
                ? await _transactionService.CreateTransactionAsync(new Transaction
                {
                    Description = Description,
                    Amount = Amount,
                    Date = Date,
                    Type = Type,
                    CategoryId = SelectedCategory.Id,
                    UserId = userId
                }, userId)
                : await _transactionService.UpdateTransactionAsync(
                    SelectedTransaction.Id,
                    Amount,
                    Description,
                    Type,
                    SelectedCategory.Id,
                    Date,
                    userId);

            if (!result.Success)
            {
                DialogHelper.Error(result.Message);
                return;
            }

            CloseEditor();
            await LoadAsync();
        }

        private async Task DeleteSelectedAsync()
        {
            if (SelectedTransaction != null)
                await DeleteAsync(SelectedTransaction);
        }

        private async Task DeleteItemAsync(TransactionListItem? item)
        {
            if (item != null)
                await DeleteAsync(item.Transaction);
        }

        private async Task DeleteAsync(Transaction transaction)
        {
            if (!DialogHelper.Confirm(_appearanceService.Format("DeleteTransactionConfirmFormat", transaction.Description)))
                return;

            var result = await _transactionService.DeleteTransactionAsync(
                transaction.Id,
                _sessionContext.CurrentUserId ?? 0);

            if (!result.Success)
            {
                DialogHelper.Error(result.Message);
                return;
            }

            CloseEditor();
            await LoadAsync();
        }

        private async Task OpenEditorAsync(Transaction? transaction)
        {
            if (transaction == null)
                return;

            SelectedTransaction = transaction;
            Description = transaction.Description;
            Amount = transaction.Amount;
            Date = transaction.Date;

            if (_type != transaction.Type)
            {
                _type = transaction.Type;
                OnPropertyChanged(nameof(Type));
            }

            await LoadCategoriesAsync(transaction.CategoryId);
            EditorTitle = _appearanceService.T("EditTransactionTitle");
            IsEditorOpen = true;
        }

        private void OpenNewEditor()
        {
            ClearForm();
            EditorTitle = _appearanceService.T("AddTransactionTitle");
            IsEditorOpen = true;
        }

        private void CloseEditor()
        {
            ClearForm();
            IsEditorOpen = false;
        }

        private void ClearForm()
        {
            SelectedTransaction = null;
            SelectedCategory = null;
            Description = string.Empty;
            Amount = 0;
            Date = DateTime.Now;
            Type = TransactionType.Expense;
            CloseQuickCategoryEditor();
        }

        private void SelectFilter(string? filter)
        {
            SelectedFilter = filter is "Income" or "Expense" ? filter : "All";
        }

        private void OpenQuickCategoryEditor()
        {
            NewCategoryName = string.Empty;
            NewCategoryColor = Type == TransactionType.Expense ? "#EF4444" : "#10B981";
            NewCategoryIcon = Type == TransactionType.Expense ? "Shape" : "Briefcase";
            IsQuickCategoryOpen = true;
        }

        private void CloseQuickCategoryEditor()
        {
            NewCategoryName = string.Empty;
            NewCategoryColor = "#0EA5E9";
            NewCategoryIcon = "Shape";
            IsQuickCategoryOpen = false;
        }

        private async Task CreateQuickCategoryAsync()
        {
            if (!Validator.Required(NewCategoryName))
            {
                DialogHelper.Error(_appearanceService.T("CategoryNameRequired"));
                return;
            }

            if (!IsValidColor(NewCategoryColor))
            {
                DialogHelper.Error(_appearanceService.T("InvalidColorMessage"));
                return;
            }

            var userId = _sessionContext.CurrentUserId ?? 0;
            var category = new Category
            {
                Name = NewCategoryName,
                Type = Type,
                Color = NewCategoryColor,
                Icon = string.IsNullOrWhiteSpace(NewCategoryIcon) ? "Shape" : NewCategoryIcon
            };

            var ok = await _categoryService.CreateCategoryAsync(category, userId);
            if (!ok)
            {
                DialogHelper.Error(_appearanceService.T("CategorySaveFailed"));
                return;
            }

            await LoadCategoriesAsync(category.Id);
            CloseQuickCategoryEditor();
        }

        private static bool IsValidColor(string? value)
        {
            return value?.Length == 7
                && value.StartsWith('#')
                && int.TryParse(value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
        }
    }

    public class TransactionListItem
    {
        public TransactionListItem(Transaction transaction, IAppearanceService appearanceService)
        {
            Transaction = transaction;
            DateText = transaction.Date.ToString("d", CultureInfo.CurrentCulture);
            Description = transaction.Description;
            CategoryName = appearanceService.LocalizeCategoryName(transaction.Category?.Name, transaction.Category?.Type);
            TypeText = appearanceService.LocalizeTransactionType(transaction.Type);
            AmountText = $"{(transaction.Type == TransactionType.Income ? "+" : "-")}{DashboardPresentation.FormatMoney(transaction.Amount)}";
            IconText = transaction.Type == TransactionType.Income ? "↗" : "↘";
            AmountBrush = CreateBrush(transaction.Type == TransactionType.Income ? "#059669" : "#EF4444");
            IconBackgroundBrush = CreateBrush(transaction.Type == TransactionType.Income ? "#DCFCE7" : "#FEE2E2");
            CategoryBrush = CreateBrush(transaction.Category?.Color, "#06B6D4");
            CategoryBackgroundBrush = CreateBrush(ToSoftColor(transaction.Category?.Color), "#E0F2FE");
        }

        public Transaction Transaction { get; }
        public string DateText { get; }
        public string Description { get; }
        public string CategoryName { get; }
        public string TypeText { get; }
        public string AmountText { get; }
        public string IconText { get; }
        public Brush AmountBrush { get; }
        public Brush IconBackgroundBrush { get; }
        public Brush CategoryBrush { get; }
        public Brush CategoryBackgroundBrush { get; }

        private static Brush CreateBrush(string? color, string fallback = "#06B6D4")
        {
            try
            {
                return DashboardPresentation.CreateBrush(string.IsNullOrWhiteSpace(color) ? fallback : color);
            }
            catch
            {
                return DashboardPresentation.CreateBrush(fallback);
            }
        }

        private static string ToSoftColor(string? color)
        {
            return !string.IsNullOrWhiteSpace(color) && color.Length == 7 && color[0] == '#'
                ? $"#20{color[1..]}"
                : "#E0F2FE";
        }
    }
}



