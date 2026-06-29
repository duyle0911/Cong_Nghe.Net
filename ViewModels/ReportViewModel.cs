using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using SkiaSharp;
using QuanLyTaiChinhCaNhan_Nhom06.Commands;
using QuanLyTaiChinhCaNhan_Nhom06.Helpers;
using QuanLyTaiChinhCaNhan_Nhom06.Models;
using QuanLyTaiChinhCaNhan_Nhom06.Services;

namespace QuanLyTaiChinhCaNhan_Nhom06.ViewModels
{
    public class ReportViewModel : ViewModelBase
    {
        private readonly IReportService _reportService;
        private readonly IAppearanceService _appearanceService;
        private readonly ISessionContext _sessionContext;
        private DateTime _fromDate = DateTimeHelper.StartOfMonth(DateTime.Now);
        private DateTime _toDate = DateTimeHelper.EndOfMonth(DateTime.Now);
        private string _income = "0 ₫";
        private string _expense = "0 ₫";
        private string _balance = "0 ₫";
        private string _incomeChangeText = string.Empty;
        private string _expenseChangeText = string.Empty;
        private string _balanceChangeText = string.Empty;
        private string _positiveInsight = string.Empty;
        private string _warningInsight = string.Empty;
        private bool _hasData;
        private ISeries[] _categorySeries = Array.Empty<ISeries>();
        private ISeries[] _cashFlowSeries = Array.Empty<ISeries>();
        private ISeries[] _dailyExpenseSeries = Array.Empty<ISeries>();
        private Axis[] _xAxes = new[] { new Axis() };
        private Axis[] _cashFlowYAxes = new[] { new Axis() };
        private Axis[] _dailyXAxes = new[] { new Axis() };
        private string _cashFlowChartSummary = string.Empty;

        public ReportViewModel(IReportService reportService, IAppearanceService appearanceService, ISessionContext sessionContext)
        {
            _reportService = reportService;
            _appearanceService = appearanceService;
            _sessionContext = sessionContext;
            IncomeChangeText = _appearanceService.T("IncomeInPeriodText");
            ExpenseChangeText = _appearanceService.T("ExpenseInPeriodText");
            BalanceChangeText = _appearanceService.T("BalanceDescriptionText");
            PositiveInsight = _appearanceService.T("NoMajorExpenseInsight");
            WarningInsight = _appearanceService.T("TrackRegularlyInsight");
            CashFlowChartSummary = _appearanceService.T("CashFlowSummaryDefault");
            Transactions = new ObservableCollection<Transaction>();
            FilterCommand = new AsyncRelayCommand(_ => LoadAsync());
            SelectPeriodCommand = new AsyncRelayCommand(period => SelectPeriodAsync(period?.ToString()));
            ExportCommand = new RelayCommand(_ => ExportCsv(), _ => Transactions.Any());
            _ = RunSafeAsync(LoadAsync);
        }

        public DateTime FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }
        public DateTime ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }
        public string Income { get => _income; set => SetProperty(ref _income, value); }
        public string Expense { get => _expense; set => SetProperty(ref _expense, value); }
        public string Balance { get => _balance; set => SetProperty(ref _balance, value); }
        public string IncomeChangeText { get => _incomeChangeText; set => SetProperty(ref _incomeChangeText, value); }
        public string ExpenseChangeText { get => _expenseChangeText; set => SetProperty(ref _expenseChangeText, value); }
        public string BalanceChangeText { get => _balanceChangeText; set => SetProperty(ref _balanceChangeText, value); }
        public string PositiveInsight { get => _positiveInsight; set => SetProperty(ref _positiveInsight, value); }
        public string WarningInsight { get => _warningInsight; set => SetProperty(ref _warningInsight, value); }
        public bool HasData { get => _hasData; set => SetProperty(ref _hasData, value); }
        public ObservableCollection<Transaction> Transactions { get; }
        public ISeries[] CategorySeries { get => _categorySeries; set => SetProperty(ref _categorySeries, value); }
        public ISeries[] CashFlowSeries { get => _cashFlowSeries; set => SetProperty(ref _cashFlowSeries, value); }
        public ISeries[] DailyExpenseSeries { get => _dailyExpenseSeries; set => SetProperty(ref _dailyExpenseSeries, value); }
        public Axis[] XAxes { get => _xAxes; set => SetProperty(ref _xAxes, value); }
        public Axis[] CashFlowYAxes { get => _cashFlowYAxes; set => SetProperty(ref _cashFlowYAxes, value); }
        public Axis[] DailyXAxes { get => _dailyXAxes; set => SetProperty(ref _dailyXAxes, value); }
        public string CashFlowChartSummary { get => _cashFlowChartSummary; set => SetProperty(ref _cashFlowChartSummary, value); }
        public ICommand FilterCommand { get; }
        public ICommand SelectPeriodCommand { get; }
        public ICommand ExportCommand { get; }

        private async Task SelectPeriodAsync(string? period)
        {
            var now = DateTime.Now;
            switch (period)
            {
                case "Quarter":
                    var quarterStartMonth = ((now.Month - 1) / 3 * 3) + 1;
                    FromDate = new DateTime(now.Year, quarterStartMonth, 1);
                    ToDate = FromDate.AddMonths(3).AddDays(-1);
                    break;
                case "Year":
                    FromDate = new DateTime(now.Year, 1, 1);
                    ToDate = new DateTime(now.Year, 12, 31);
                    break;
                default:
                    FromDate = DateTimeHelper.StartOfMonth(now);
                    ToDate = DateTimeHelper.EndOfMonth(now);
                    break;
            }

            await LoadAsync();
        }

        public override void RefreshLocalization()
        {
            _ = RunSafeAsync(LoadAsync);
        }

        private async Task LoadAsync()
        {
            if (FromDate.Date > ToDate.Date)
            {
                DialogHelper.Error(_appearanceService.T("InvalidDateRange"));
                return;
            }

            var userId = _sessionContext.CurrentUserId ?? 0;
            var transactions = await _reportService.GetTransactionsAsync(userId, FromDate, ToDate);
            var totalIncome = transactions.Where(transaction => transaction.Type == TransactionType.Income).Sum(transaction => transaction.Amount);
            var totalExpense = transactions.Where(transaction => transaction.Type == TransactionType.Expense).Sum(transaction => transaction.Amount);
            Income = DashboardPresentation.FormatMoney(totalIncome);
            Expense = DashboardPresentation.FormatMoney(totalExpense);
            Balance = DashboardPresentation.FormatMoney(totalIncome - totalExpense);
            IncomeChangeText = _appearanceService.T("IncomeInPeriodText");
            ExpenseChangeText = _appearanceService.T("ExpenseInPeriodText");
            BalanceChangeText = totalIncome >= totalExpense ? _appearanceService.T("PositiveCashFlowText") : _appearanceService.T("ExpenseOverIncomeText");

            Transactions.Clear();
            foreach (var transaction in transactions)
                Transactions.Add(transaction);
            HasData = Transactions.Any();
            CommandManager.InvalidateRequerySuggested();

            var categories = await _reportService.GetCategorySpendingAsync(userId, FromDate, ToDate);
            CategorySeries = categories.Take(6).Select(category => new PieSeries<double>
            {
                Name = _appearanceService.LocalizeCategoryName(category.CategoryName, TransactionType.Expense),
                Values = new[] { (double)category.Amount },
                Fill = new SolidColorPaint(DashboardPresentation.ParseColor(category.Color)),
                InnerRadius = 48
            }).ToArray();

            var cashFlow = await _reportService.GetCashFlowAsync(userId, FromDate, ToDate);
            XAxes = new[]
            {
                new Axis
                {
                    Labels = cashFlow.Select(point => point.Label).ToArray(),
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#64748B")),
                    TextSize = 11
                }
            };
            CashFlowSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = _appearanceService.LocalizeTransactionType(TransactionType.Income),
                    Values = cashFlow.Select(point => (double)point.Income).ToArray(),
                    Stroke = new SolidColorPaint(SKColor.Parse("#10B981"), 3),
                    Fill = new SolidColorPaint(SKColor.Parse("#3010B981")),
                    GeometrySize = 7
                },
                new LineSeries<double>
                {
                    Name = _appearanceService.LocalizeTransactionType(TransactionType.Expense),
                    Values = cashFlow.Select(point => (double)point.Expense).ToArray(),
                    Stroke = new SolidColorPaint(SKColor.Parse("#FB7185"), 3),
                    Fill = new SolidColorPaint(SKColor.Parse("#30FB7185")),
                    GeometrySize = 7
                }
            };

            BuildCashFlowChart(transactions);

            var dailyExpenses = transactions
                .Where(transaction => transaction.Type == TransactionType.Expense)
                .GroupBy(transaction => transaction.Date.Date)
                .OrderBy(group => group.Key)
                .Select(group => new { Date = group.Key, Amount = group.Sum(transaction => transaction.Amount) })
                .ToList();
            DailyXAxes = new[]
            {
                new Axis
                {
                    Labels = dailyExpenses.Select(item => item.Date.ToString("dd/MM")).ToArray(),
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#64748B")),
                    TextSize = 11
                }
            };
            DailyExpenseSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = _appearanceService.LocalizeTransactionType(TransactionType.Expense),
                    Values = dailyExpenses.Select(item => (double)item.Amount).ToArray(),
                    Stroke = new SolidColorPaint(SKColor.Parse("#06B6D4"), 3),
                    Fill = null,
                    GeometrySize = 7
                }
            };

            UpdateInsights(categories, totalIncome, totalExpense);
        }

        private void BuildCashFlowChart(IReadOnlyList<Transaction> transactions)
        {
            var buckets = BuildCashFlowBuckets(transactions);
            var labelCount = buckets.Count;

            XAxes = new[]
            {
                new Axis
                {
                    Labels = buckets.Select(bucket => bucket.Label).ToArray(),
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#64748B")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E2E8F0"), 1),
                    TextSize = 11,
                    MinStep = 1,
                    ForceStepToMin = true,
                    UnitWidth = 1
                }
            };

            CashFlowYAxes = new[]
            {
                new Axis
                {
                    Labeler = FormatChartMoney,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#64748B")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E2E8F0"), 1),
                    TextSize = 11,
                    MinLimit = 0
                }
            };

            CashFlowSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = _appearanceService.LocalizeTransactionType(TransactionType.Income),
                    Values = buckets.Select(bucket => (double)bucket.Income).ToArray(),
                    Fill = new SolidColorPaint(SKColor.Parse("#10B981")),
                    Stroke = null,
                    MaxBarWidth = labelCount <= 12 ? 30 : 22,
                    Padding = labelCount <= 12 ? 8 : 5,
                    Rx = 6,
                    Ry = 6
                },
                new ColumnSeries<double>
                {
                    Name = _appearanceService.LocalizeTransactionType(TransactionType.Expense),
                    Values = buckets.Select(bucket => (double)bucket.Expense).ToArray(),
                    Fill = new SolidColorPaint(SKColor.Parse("#FB7185")),
                    Stroke = null,
                    MaxBarWidth = labelCount <= 12 ? 30 : 22,
                    Padding = labelCount <= 12 ? 8 : 5,
                    Rx = 6,
                    Ry = 6
                }
            };
        }

        private IReadOnlyList<CashFlowBucket> BuildCashFlowBuckets(IReadOnlyList<Transaction> transactions)
        {
            var start = FromDate.Date;
            var end = ToDate.Date;
            var totalDays = Math.Max(1, (end - start).Days + 1);
            var buckets = new List<CashFlowBucket>();

            if (totalDays <= 45)
            {
                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    var index = (date - start).Days;
                    var showLabel = index == 0 || date == end || index % 5 == 0;
                    buckets.Add(CreateBucket(showLabel ? date.ToString("dd/MM") : string.Empty, transactions, date, date));
                }

                CashFlowChartSummary = _appearanceService.Format("DailyDataSummaryFormat", totalDays);
                return buckets;
            }

            if (totalDays <= 120)
            {
                var weekStart = start;
                while (weekStart <= end)
                {
                    var weekEnd = weekStart.AddDays(6);
                    if (weekEnd > end)
                        weekEnd = end;

                    buckets.Add(CreateBucket($"{weekStart:dd/MM}-{weekEnd:dd/MM}", transactions, weekStart, weekEnd));
                    weekStart = weekEnd.AddDays(1);
                }

                CashFlowChartSummary = _appearanceService.Format("WeeklyDataSummaryFormat", buckets.Count);
                return buckets;
            }

            var month = DateTimeHelper.StartOfMonth(start);
            var lastMonth = DateTimeHelper.StartOfMonth(end);
            while (month <= lastMonth)
            {
                var bucketStart = month < start ? start : month;
                var monthEnd = DateTimeHelper.EndOfMonth(month);
                var bucketEnd = monthEnd > end ? end : monthEnd;
                buckets.Add(CreateBucket($"{month:MM/yyyy}", transactions, bucketStart, bucketEnd));
                month = month.AddMonths(1);
            }

            CashFlowChartSummary = _appearanceService.Format("MonthlyDataSummaryFormat", buckets.Count);
            return buckets;
        }

        private static CashFlowBucket CreateBucket(string label, IReadOnlyList<Transaction> transactions, DateTime start, DateTime end)
        {
            var income = transactions
                .Where(transaction => transaction.Type == TransactionType.Income && transaction.Date.Date >= start && transaction.Date.Date <= end)
                .Sum(transaction => transaction.Amount);
            var expense = transactions
                .Where(transaction => transaction.Type == TransactionType.Expense && transaction.Date.Date >= start && transaction.Date.Date <= end)
                .Sum(transaction => transaction.Amount);

            return new CashFlowBucket(label, income, expense);
        }

                private string FormatChartMoney(double value)
        {
            var absolute = Math.Abs(value);

            if (absolute >= 1_000_000_000)
                return $"{value / 1_000_000_000:N1} {_appearanceService.T("BillionUnitSuffix")}";

            if (absolute >= 1_000_000)
                return $"{value / 1_000_000:N0} {_appearanceService.T("MillionUnitSuffix")}";

            if (absolute >= 1_000)
                return $"{value / 1_000:N0}k";

            return value.ToString("N0", CultureInfo.CurrentCulture);
        }

        private readonly record struct CashFlowBucket(string Label, decimal Income, decimal Expense);

        private void UpdateInsights(IReadOnlyList<CategorySpending> categories, decimal totalIncome, decimal totalExpense)
        {
            if (!HasData)
            {
                PositiveInsight = _appearanceService.T("NoTransactionsInPeriodText");
                WarningInsight = _appearanceService.T("TrackRegularlyInsight");
                return;
            }

            var savingsRate = totalIncome > 0 ? (totalIncome - totalExpense) / totalIncome * 100 : 0;
            PositiveInsight = totalIncome >= totalExpense
                ? _appearanceService.Format("PositiveSavingsInsightFormat", Math.Max(0, savingsRate))
                : _appearanceService.T("RebalanceInsightText");

            var largestCategory = categories.FirstOrDefault();
            WarningInsight = largestCategory == null
                ? _appearanceService.T("NoMajorExpenseInsight")
                : _appearanceService.Format("LargestCategoryInsightFormat", _appearanceService.LocalizeCategoryName(largestCategory.CategoryName, TransactionType.Expense), DashboardPresentation.FormatMoney(largestCategory.Amount));
        }

        private void ExportCsv()
        {
            var dialog = new SaveFileDialog
            {
                Title = _appearanceService.T("ExportReportDialogTitle"),
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"report-{FromDate:yyyyMMdd}-{ToDate:yyyyMMdd}.csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            var builder = new StringBuilder();
            builder.AppendLine(_appearanceService.T("CsvHeaderText"));
            foreach (var transaction in Transactions)
            {
                builder.AppendLine(string.Join(",",
                    transaction.Date.ToString("d", CultureInfo.CurrentCulture),
                    EscapeCsv(transaction.Description),
                    EscapeCsv(_appearanceService.LocalizeCategoryName(transaction.Category?.Name, transaction.Category?.Type)),
                    _appearanceService.LocalizeTransactionType(transaction.Type),
                    transaction.Amount.ToString(CultureInfo.InvariantCulture)));
            }

            File.WriteAllText(dialog.FileName, builder.ToString(), Encoding.UTF8);
        }

        private static string EscapeCsv(string value)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}


