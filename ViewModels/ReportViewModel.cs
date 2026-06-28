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
        private readonly ISessionContext _sessionContext;
        private DateTime _fromDate = DateTimeHelper.StartOfMonth(DateTime.Now);
        private DateTime _toDate = DateTimeHelper.EndOfMonth(DateTime.Now);
        private string _income = "0 ₫";
        private string _expense = "0 ₫";
        private string _balance = "0 ₫";
        private string _incomeChangeText = "Tổng khoản thu trong kỳ";
        private string _expenseChangeText = "Tổng khoản chi trong kỳ";
        private string _balanceChangeText = "Thu nhập trừ chi tiêu";
        private string _positiveInsight = "Chưa có đủ dữ liệu để đưa ra nhận xét.";
        private string _warningInsight = "Hãy ghi nhận giao dịch đều đặn để theo dõi tài chính chính xác hơn.";
        private bool _hasData;
        private ISeries[] _categorySeries = Array.Empty<ISeries>();
        private ISeries[] _cashFlowSeries = Array.Empty<ISeries>();
        private ISeries[] _dailyExpenseSeries = Array.Empty<ISeries>();
        private Axis[] _xAxes = new[] { new Axis() };
        private Axis[] _cashFlowYAxes = new[] { new Axis() };
        private Axis[] _dailyXAxes = new[] { new Axis() };
        private string _cashFlowChartSummary = "Theo dõi biến động thu nhập và chi tiêu trong khoảng thời gian đã chọn.";

        public ReportViewModel(IReportService reportService, ISessionContext sessionContext)
        {
            _reportService = reportService;
            _sessionContext = sessionContext;
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

        private async Task LoadAsync()
        {
            if (FromDate.Date > ToDate.Date)
            {
                DialogHelper.Error("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
                return;
            }

            var userId = _sessionContext.CurrentUserId ?? 0;
            var transactions = await _reportService.GetTransactionsAsync(userId, FromDate, ToDate);
            var totalIncome = transactions.Where(transaction => transaction.Type == TransactionType.Income).Sum(transaction => transaction.Amount);
            var totalExpense = transactions.Where(transaction => transaction.Type == TransactionType.Expense).Sum(transaction => transaction.Amount);
            Income = DashboardPresentation.FormatMoney(totalIncome);
            Expense = DashboardPresentation.FormatMoney(totalExpense);
            Balance = DashboardPresentation.FormatMoney(totalIncome - totalExpense);
            IncomeChangeText = "Tổng khoản thu trong kỳ";
            ExpenseChangeText = "Tổng khoản chi trong kỳ";
            BalanceChangeText = totalIncome >= totalExpense ? "Dòng tiền đang dương" : "Chi tiêu đang vượt thu nhập";

            Transactions.Clear();
            foreach (var transaction in transactions)
                Transactions.Add(transaction);
            HasData = Transactions.Any();
            CommandManager.InvalidateRequerySuggested();

            var categories = await _reportService.GetCategorySpendingAsync(userId, FromDate, ToDate);
            CategorySeries = categories.Take(6).Select(category => new PieSeries<double>
            {
                Name = category.CategoryName,
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
                    Name = "Thu nhập",
                    Values = cashFlow.Select(point => (double)point.Income).ToArray(),
                    Stroke = new SolidColorPaint(SKColor.Parse("#10B981"), 3),
                    Fill = new SolidColorPaint(SKColor.Parse("#3010B981")),
                    GeometrySize = 7
                },
                new LineSeries<double>
                {
                    Name = "Chi tiêu",
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
                    Name = "Chi tiêu",
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
                    Name = "Thu nhập",
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
                    Name = "Chi tiêu",
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

                CashFlowChartSummary = $"Hiển thị theo ngày, {totalDays} mốc dữ liệu trong kỳ.";
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

                CashFlowChartSummary = $"Hiển thị theo tuần, {buckets.Count} mốc dữ liệu trong kỳ.";
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

            CashFlowChartSummary = $"Hiển thị theo tháng, {buckets.Count} mốc dữ liệu trong kỳ.";
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

        private static string FormatChartMoney(double value)
        {
            var absolute = Math.Abs(value);

            if (absolute >= 1_000_000_000)
                return $"{value / 1_000_000_000:N1} tỷ";

            if (absolute >= 1_000_000)
                return $"{value / 1_000_000:N0} tr";

            if (absolute >= 1_000)
                return $"{value / 1_000:N0}k";

            return value.ToString("N0", CultureInfo.CurrentCulture);
        }

        private readonly record struct CashFlowBucket(string Label, decimal Income, decimal Expense);

        private void UpdateInsights(IReadOnlyList<CategorySpending> categories, decimal totalIncome, decimal totalExpense)
        {
            if (!HasData)
            {
                PositiveInsight = "Chưa có giao dịch trong khoảng thời gian đã chọn.";
                WarningInsight = "Hãy ghi nhận giao dịch đều đặn để theo dõi tài chính chính xác hơn.";
                return;
            }

            var savingsRate = totalIncome > 0 ? (totalIncome - totalExpense) / totalIncome * 100 : 0;
            PositiveInsight = totalIncome >= totalExpense
                ? $"Dòng tiền dương. Bạn đang giữ lại {Math.Max(0, savingsRate):N0}% thu nhập trong kỳ."
                : "Các giao dịch đã được tổng hợp. Hãy cân đối lại dòng tiền trong kỳ tiếp theo.";

            var largestCategory = categories.FirstOrDefault();
            WarningInsight = largestCategory == null
                ? "Chưa có khoản chi nổi bật cần lưu ý."
                : $"Danh mục \"{largestCategory.CategoryName}\" đang chiếm mức chi lớn nhất: {DashboardPresentation.FormatMoney(largestCategory.Amount)}.";
        }

        private void ExportCsv()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Xuất báo cáo",
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"bao-cao-{FromDate:yyyyMMdd}-{ToDate:yyyyMMdd}.csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            var builder = new StringBuilder();
            builder.AppendLine("Ngày,Nội dung,Danh mục,Loại,Số tiền");
            foreach (var transaction in Transactions)
            {
                builder.AppendLine(string.Join(",",
                    transaction.Date.ToString("dd/MM/yyyy"),
                    EscapeCsv(transaction.Description),
                    EscapeCsv(transaction.Category?.Name ?? string.Empty),
                    transaction.Type,
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
