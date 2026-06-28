using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using QuanLyTaiChinhCaNhan_Nhom06.Helpers;
using QuanLyTaiChinhCaNhan_Nhom06.Models;
using QuanLyTaiChinhCaNhan_Nhom06.Services;

namespace QuanLyTaiChinhCaNhan_Nhom06.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IReportService _reportService;
        private readonly IBudgetService _budgetService;
        private readonly ISessionContext _sessionContext;
        private string _totalIncome = "0 ₫";
        private string _totalExpense = "0 ₫";
        private string _balance = "0 ₫";
        private string _savings = "0 ₫";
        private string _incomeChangeText = "Chưa có dữ liệu kỳ trước";
        private string _expenseChangeText = "Chưa có dữ liệu kỳ trước";
        private string _balanceChangeText = "Thu nhập trừ chi tiêu";
        private string _savingsChangeText = "Tỷ lệ tiết kiệm hiện tại";
        private bool _hasTransactions;
        private bool _hasExpenseChart;
        private bool _hasBudgets;
        private bool _hasBudgetAlerts;
        private string _budgetAlertText = string.Empty;
        private ISeries[] _expenseSeries = Array.Empty<ISeries>();
        private ISeries[] _cashFlowSeries = Array.Empty<ISeries>();
        private Axis[] _cashFlowXAxes = new[] { new Axis() };
        private Axis[] _cashFlowYAxes = new[] { new Axis() };
        private string[] _cashFlowLabels = Array.Empty<string>();
        private string _cashFlowMaxLabel = "0";
        private string _cashFlowMidLabel = "0";

        public DashboardViewModel(IReportService reportService, IBudgetService budgetService, ISessionContext sessionContext)
        {
            _reportService = reportService;
            _budgetService = budgetService;
            _sessionContext = sessionContext;
            RecentTransactions = new ObservableCollection<DashboardTransactionItem>();
            Budgets = new ObservableCollection<DashboardBudgetItem>();
            CashFlowItems = new ObservableCollection<DashboardCashFlowItem>();
            _ = RunSafeAsync(LoadAsync);
        }

        public string TotalIncome { get => _totalIncome; set => SetProperty(ref _totalIncome, value); }
        public string TotalExpense { get => _totalExpense; set => SetProperty(ref _totalExpense, value); }
        public string Balance { get => _balance; set => SetProperty(ref _balance, value); }
        public string Savings { get => _savings; set => SetProperty(ref _savings, value); }
        public string IncomeChangeText { get => _incomeChangeText; set => SetProperty(ref _incomeChangeText, value); }
        public string ExpenseChangeText { get => _expenseChangeText; set => SetProperty(ref _expenseChangeText, value); }
        public string BalanceChangeText { get => _balanceChangeText; set => SetProperty(ref _balanceChangeText, value); }
        public string SavingsChangeText { get => _savingsChangeText; set => SetProperty(ref _savingsChangeText, value); }
        public bool HasTransactions { get => _hasTransactions; set => SetProperty(ref _hasTransactions, value); }
        public bool HasExpenseChart { get => _hasExpenseChart; set => SetProperty(ref _hasExpenseChart, value); }
        public bool HasBudgets { get => _hasBudgets; set => SetProperty(ref _hasBudgets, value); }
        public bool HasBudgetAlerts { get => _hasBudgetAlerts; set => SetProperty(ref _hasBudgetAlerts, value); }
        public string BudgetAlertText { get => _budgetAlertText; set => SetProperty(ref _budgetAlertText, value); }
        public ObservableCollection<DashboardTransactionItem> RecentTransactions { get; }
        public ObservableCollection<DashboardBudgetItem> Budgets { get; }
        public ObservableCollection<DashboardCashFlowItem> CashFlowItems { get; }
        public ISeries[] ExpenseSeries { get => _expenseSeries; set => SetProperty(ref _expenseSeries, value); }
        public ISeries[] CashFlowSeries { get => _cashFlowSeries; set => SetProperty(ref _cashFlowSeries, value); }
        public Axis[] CashFlowXAxes { get => _cashFlowXAxes; set => SetProperty(ref _cashFlowXAxes, value); }
        public Axis[] CashFlowYAxes { get => _cashFlowYAxes; set => SetProperty(ref _cashFlowYAxes, value); }
        public string[] CashFlowLabels { get => _cashFlowLabels; set => SetProperty(ref _cashFlowLabels, value); }
        public string CashFlowMaxLabel { get => _cashFlowMaxLabel; set => SetProperty(ref _cashFlowMaxLabel, value); }
        public string CashFlowMidLabel { get => _cashFlowMidLabel; set => SetProperty(ref _cashFlowMidLabel, value); }

        private async Task LoadAsync()
        {
            var userId = _sessionContext.CurrentUserId ?? 0;
            var summary = await _reportService.GetSummaryAsync(userId);
            TotalIncome = DashboardPresentation.FormatMoney(summary.TotalIncome);
            TotalExpense = DashboardPresentation.FormatMoney(summary.TotalExpense);
            Balance = DashboardPresentation.FormatMoney(summary.TotalBalance);
            Savings = DashboardPresentation.FormatMoney(Math.Max(0, summary.MonthlyIncome - summary.MonthlyExpense));
            IncomeChangeText = DashboardPresentation.FormatChange(summary.IncomeChangePercent, "so với tháng trước");
            ExpenseChangeText = DashboardPresentation.FormatChange(summary.ExpenseChangePercent, "so với tháng trước");
            BalanceChangeText = "Tổng thu trừ tổng chi";
            SavingsChangeText = $"{summary.SavingsRate:+0.##;-0.##;0}% tỷ lệ tiết kiệm";

            var now = DateTime.Now;
            var monthStart = DateTimeHelper.StartOfMonth(now);
            var monthEnd = DateTimeHelper.EndOfMonth(now);

            RecentTransactions.Clear();
            var transactions = await _reportService.GetTransactionsAsync(userId, monthStart, monthEnd);
            foreach (var transaction in transactions.Take(5))
                RecentTransactions.Add(new DashboardTransactionItem(transaction));
            HasTransactions = RecentTransactions.Any();

            var categories = await _reportService.GetCategorySpendingAsync(userId, monthStart, monthEnd);
            ExpenseSeries = categories.Take(6).Select(category => new PieSeries<double>
            {
                Name = category.CategoryName,
                Values = new[] { (double)category.Amount },
                Fill = new SolidColorPaint(DashboardPresentation.ParseColor(category.Color)),
                InnerRadius = 62,
                MaxRadialColumnWidth = 24,
                HoverPushout = 10
            }).ToArray();
            HasExpenseChart = ExpenseSeries.Length > 0;

            var cashFlow = await _reportService.GetCashFlowAsync(userId, now.AddMonths(-5), now);
            CashFlowLabels = cashFlow.Select(point => point.Label).ToArray();
            var maxCashFlowAmount = cashFlow
                .SelectMany(point => new[] { point.Income, point.Expense })
                .DefaultIfEmpty(0)
                .Max();
            var chartMax = DashboardPresentation.GetChartMaxLimit(maxCashFlowAmount);
            CashFlowMaxLabel = DashboardPresentation.FormatChartMoney(chartMax);
            CashFlowMidLabel = DashboardPresentation.FormatChartMoney(chartMax / 2);
            CashFlowItems.Clear();
            foreach (var point in cashFlow)
                CashFlowItems.Add(new DashboardCashFlowItem(point.Label, point.Income, point.Expense, chartMax));

            CashFlowXAxes = new[]
            {
                new Axis
                {
                    IsVisible = false,
                    Labeler = _ => string.Empty,
                    LabelsPaint = null,
                    SeparatorsPaint = null,
                    TextSize = 0,
                    MinStep = 1,
                    ForceStepToMin = true,
                    UnitWidth = 1,
                    MinLimit = -0.5,
                    MaxLimit = Math.Max(0d, cashFlow.Count - 0.5)
                }
            };
            CashFlowYAxes = new[]
            {
                new Axis
                {
                    Labeler = DashboardPresentation.FormatChartMoney,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#64748B")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E2E8F0"), 1),
                    TextSize = 11,
                    MinLimit = 0,
                    MaxLimit = DashboardPresentation.GetChartMaxLimit(maxCashFlowAmount)
                }
            };
            CashFlowSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Thu",
                    Values = cashFlow.Select(point => (double)point.Income).ToArray(),
                    Fill = new SolidColorPaint(SKColor.Parse("#10B981")),
                    Stroke = null,
                    MaxBarWidth = 34,
                    Padding = 8,
                    Rx = 5,
                    Ry = 5
                },
                new ColumnSeries<double>
                {
                    Name = "Chi",
                    Values = cashFlow.Select(point => (double)point.Expense).ToArray(),
                    Fill = new SolidColorPaint(SKColor.Parse("#FB7185")),
                    Stroke = null,
                    MaxBarWidth = 34,
                    Padding = 8,
                    Rx = 5,
                    Ry = 5
                }
            };

            Budgets.Clear();
            var budgets = await _budgetService.GetBudgetsAsync(userId);
            foreach (var budget in budgets.Where(item => item.IsActive).Take(5))
                Budgets.Add(new DashboardBudgetItem(budget));
            HasBudgets = Budgets.Any();

            var budgetAlerts = await _budgetService.CheckBudgetAlertsAsync(userId);
            HasBudgetAlerts = budgetAlerts.HasAlerts;
            BudgetAlertText = budgetAlerts.HasAlerts
                ? string.Join(" | ", budgetAlerts.OverBudgetItems.Concat(budgetAlerts.WarningItems))
                : string.Empty;
        }
    }

    public class DashboardTransactionItem
    {
        public DashboardTransactionItem(Transaction transaction)
        {
            Description = transaction.Description;
            CategoryName = transaction.Category?.Name ?? "Chưa phân loại";
            DateText = transaction.Date.ToString("dd/MM/yyyy");
            var isIncome = transaction.Type == TransactionType.Income;
            AmountText = $"{(isIncome ? "+" : "-")}{DashboardPresentation.FormatMoney(transaction.Amount)}";
            AccentBrush = DashboardPresentation.CreateBrush(isIncome ? "#10B981" : "#EF4444");
            SoftAccentBrush = DashboardPresentation.CreateBrush(isIncome ? "#DCFCE7" : "#FEE2E2");
            IconText = isIncome ? "↗" : "↘";
        }

        public string Description { get; }
        public string CategoryName { get; }
        public string DateText { get; }
        public string AmountText { get; }
        public string IconText { get; }
        public Brush AccentBrush { get; }
        public Brush SoftAccentBrush { get; }
    }

    public class DashboardBudgetItem
    {
        public DashboardBudgetItem(Budget budget)
        {
            Name = budget.Category?.Name ?? budget.Name;
            AmountText = $"{DashboardPresentation.FormatMoney(budget.SpentAmount)} / {DashboardPresentation.FormatMoney(budget.Amount)}";
            ProgressText = $"{budget.UsedPercent:N0}% đã sử dụng";
            UsedPercent = budget.UsedPercent;
            ProgressBrush = DashboardPresentation.CreateBrush(
                budget.UsedPercent >= 100 ? "#EF4444" : budget.UsedPercent >= 80 ? "#F59E0B" : "#10B981");
        }

        public string Name { get; }
        public string AmountText { get; }
        public string ProgressText { get; }
        public decimal UsedPercent { get; }
        public Brush ProgressBrush { get; }
    }

    public class DashboardCashFlowItem
    {
        private const double ChartHeight = 180;

        public DashboardCashFlowItem(string label, decimal income, decimal expense, double maxAmount)
        {
            Label = label;
            IncomeBarHeight = CalculateBarHeight(income, maxAmount);
            ExpenseBarHeight = CalculateBarHeight(expense, maxAmount);
            IncomeAmountText = DashboardPresentation.FormatMoney(income);
            ExpenseAmountText = DashboardPresentation.FormatMoney(expense);
            IncomeTooltip = $"{Label}\nThu: {IncomeAmountText}";
            ExpenseTooltip = $"{Label}\nChi: {ExpenseAmountText}";
        }

        public string Label { get; }
        public double IncomeBarHeight { get; }
        public double ExpenseBarHeight { get; }
        public string IncomeAmountText { get; }
        public string ExpenseAmountText { get; }
        public string IncomeTooltip { get; }
        public string ExpenseTooltip { get; }

        private static double CalculateBarHeight(decimal value, double maxAmount)
        {
            if (value <= 0 || maxAmount <= 0)
                return 0;

            var height = (double)value / maxAmount * ChartHeight;
            return Math.Max(14, Math.Min(ChartHeight, height));
        }
    }

    internal static class DashboardPresentation
    {
        public static string FormatMoney(decimal amount)
        {
            return string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} ₫", amount);
        }

        public static string FormatChange(decimal percentage, string suffix)
        {
            return percentage == 0
                ? $"0% {suffix}"
                : $"{percentage:+0.##;-0.##}% {suffix}";
        }

        public static string FormatChartMoney(double value)
        {
            var absolute = Math.Abs(value);

            if (absolute >= 1_000_000_000)
                return $"{value / 1_000_000_000:N1} B";

            if (absolute >= 1_000_000)
                return $"{value / 1_000_000:N0} M";

            if (absolute >= 1_000)
                return $"{value / 1_000:N0}k";

            return value.ToString("N0", CultureInfo.CurrentCulture);
        }

        public static double GetChartMaxLimit(decimal maxValue)
        {
            if (maxValue <= 0)
                return 1;

            var padded = (double)maxValue * 1.18;
            var step = padded switch
            {
                >= 100_000_000 => 50_000_000,
                >= 10_000_000 => 10_000_000,
                >= 1_000_000 => 1_000_000,
                >= 100_000 => 100_000,
                _ => 10_000
            };

            return Math.Ceiling(padded / step) * step;
        }

        public static SKColor ParseColor(string? color)
        {
            return SKColor.TryParse(color, out var parsed) ? parsed : SKColor.Parse("#0EA5E9");
        }

        public static Brush CreateBrush(string color)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }
    }
}
