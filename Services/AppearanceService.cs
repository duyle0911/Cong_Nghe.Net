using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public interface IAppearanceService
    {
        AppearanceSettings Settings { get; }
        IReadOnlyList<AccentColorOption> AccentColors { get; }
        IReadOnlyList<BackgroundThemeOption> BackgroundThemes { get; }
        IReadOnlyList<LanguageOption> Languages { get; }
        event EventHandler? SettingsChanged;

        string T(string key);
        string Format(string key, params object[] args);
        string GetAccentColorDisplayName(AccentColorOption option);
        string GetBackgroundThemeDisplayName(BackgroundThemeOption option);
        string LocalizeCategoryName(string? categoryName, TransactionType? type = null);
        string LocalizeTransactionType(TransactionType type);
        void SetDarkMode(bool isDarkMode);
        void SetAccentColor(string accentColor);
        void SetBackgroundTheme(string backgroundTheme);
        void SetLanguage(string languageCode);
        void Save();
        void ApplyCurrentSettings();
    }

    public sealed class AppearanceSettings
    {
        public bool IsDarkMode { get; set; }
        public string AccentColor { get; set; } = "Cyan";
        public string BackgroundTheme { get; set; } = "Travel";
        public string LanguageCode { get; set; } = "vi-VN";
    }

    public sealed record AccentColorOption(string Name, string DisplayKey, string PrimaryColor, string SecondaryColor);
    public sealed record BackgroundThemeOption(
        string Name,
        string DisplayKey,
        string LightBackground,
        string LightCard,
        string LightMuted,
        string LightBorder,
        string LightInput,
        string LightInputBorder,
        string DarkBackground,
        string DarkCard,
        string DarkMuted,
        string DarkBorder,
        string DarkInput,
        string DarkInputBorder,
        string PreviewColor,
        string SecondaryColor);

    public sealed record LanguageOption(string Code, string DisplayName, string Flag)
    {
        public string Subtitle => Code == "vi-VN" ? "Ti\u1EBFng Vi\u1EC7t" : "Ti\u1EBFng Anh";
        public string DisplayLabel => $"{Flag} {DisplayName}";
    }

    public sealed class AppearanceService : IAppearanceService
    {
        private readonly string _settingsPath;

        public AppearanceService()
        {
            AccentColors = new[]
            {
                new AccentColorOption("Cyan", "AccentTravelText", "#0EA5E9", "#06B6D4"),
                new AccentColorOption("Blue", "AccentBusinessText", "#2563EB", "#06B6D4"),
                new AccentColorOption("Rose", "AccentHealthText", "#E11D48", "#FB7185"),
                new AccentColorOption("Lime", "AccentBonusText", "#84CC16", "#22C55E"),
                new AccentColorOption("Sky", "AccentTransportText", "#0284C7", "#38BDF8"),
                new AccentColorOption("Emerald", "AccentSalaryText", "#059669", "#22C55E"),
                new AccentColorOption("Pink", "AccentShoppingText", "#DB2777", "#F472B6"),
                new AccentColorOption("Orange", "AccentFoodText", "#F97316", "#FB923C"),
                new AccentColorOption("Violet", "AccentEntertainmentText", "#7C3AED", "#A855F7"),
                new AccentColorOption("Amber", "AccentBillsText", "#D97706", "#F59E0B")
            };
            BackgroundThemes = new[]
            {
                new BackgroundThemeOption("Travel", "BackgroundTravelText", "#EAF6FF", "#FFFFFF", "#DFF2FF", "#B8E3FF", "#F3FAFF", "#B9DDF5", "#071625", "#0E2436", "#12304A", "#24506E", "#0B1E2E", "#2B5878", "#0EA5E9", "#06B6D4"),
                new BackgroundThemeOption("Business", "BackgroundBusinessText", "#EEF4FF", "#FFFFFF", "#E0EAFF", "#C7D2FE", "#F8FAFF", "#CBD5E1", "#091426", "#101B33", "#172554", "#243B73", "#0F1B2D", "#334155", "#2563EB", "#06B6D4"),
                new BackgroundThemeOption("Health", "BackgroundHealthText", "#FFF1F5", "#FFFFFF", "#FFE4EC", "#FECDD3", "#FFF7FA", "#FDA4AF", "#1E0A14", "#2A1020", "#3B1327", "#7F1D3A", "#21101A", "#9F2A4B", "#E11D48", "#FB7185"),
                new BackgroundThemeOption("Bonus", "BackgroundBonusText", "#F7FEE7", "#FFFFFF", "#ECFCCB", "#D9F99D", "#FCFFF3", "#BEF264", "#111A05", "#1B2A0B", "#263A10", "#4D7C0F", "#162208", "#65A30D", "#84CC16", "#22C55E"),
                new BackgroundThemeOption("Transport", "BackgroundTransportText", "#F0F9FF", "#FFFFFF", "#E0F2FE", "#BAE6FD", "#F8FCFF", "#7DD3FC", "#061826", "#0C2538", "#11344F", "#075985", "#0A1F30", "#0EA5E9", "#0284C7", "#38BDF8"),
                new BackgroundThemeOption("Salary", "BackgroundSalaryText", "#ECFDF5", "#FFFFFF", "#D1FAE5", "#A7F3D0", "#F7FEFB", "#6EE7B7", "#061A12", "#0D2A1D", "#123D2C", "#047857", "#0A2118", "#10B981", "#059669", "#22C55E"),
                new BackgroundThemeOption("Shopping", "BackgroundShoppingText", "#FDF2F8", "#FFFFFF", "#FCE7F3", "#FBCFE8", "#FFF7FC", "#F9A8D4", "#1F0A19", "#2B1023", "#3B1430", "#9D174D", "#24101D", "#DB2777", "#DB2777", "#F472B6"),
                new BackgroundThemeOption("Food", "BackgroundFoodText", "#FFF7ED", "#FFFFFF", "#FFEDD5", "#FED7AA", "#FFFBF5", "#FDBA74", "#201006", "#2B170A", "#3A1F0D", "#9A3412", "#241409", "#F97316", "#F97316", "#FB923C"),
                new BackgroundThemeOption("Entertainment", "BackgroundEntertainmentText", "#F5F3FF", "#FFFFFF", "#EDE9FE", "#DDD6FE", "#FAF8FF", "#C4B5FD", "#140E2A", "#1E1738", "#2E2056", "#6D28D9", "#19112F", "#8B5CF6", "#7C3AED", "#A855F7"),
                new BackgroundThemeOption("Bills", "BackgroundBillsText", "#FFFBEB", "#FFFFFF", "#FEF3C7", "#FDE68A", "#FFFDF3", "#FCD34D", "#1C1304", "#2A1D08", "#3B2A0B", "#92400E", "#211707", "#D97706", "#D97706", "#F59E0B")
            };
            Languages = new[]
            {
                new LanguageOption("vi-VN", "Ti\u1EBFng Vi\u1EC7t", "\U0001F1FB\U0001F1F3"),
                new LanguageOption("en-US", "English", "\U0001F1EC\U0001F1E7")
            };

            var settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MoneyFlow");
            _settingsPath = Path.Combine(settingsDirectory, "appearance.json");
            Settings = Load();
        }

        public AppearanceSettings Settings { get; }
        public IReadOnlyList<AccentColorOption> AccentColors { get; }
        public IReadOnlyList<BackgroundThemeOption> BackgroundThemes { get; }
        public IReadOnlyList<LanguageOption> Languages { get; }
        public event EventHandler? SettingsChanged;

        public string T(string key)
        {
            var languageCode = Languages.Any(item => item.Code == Settings.LanguageCode)
                ? Settings.LanguageCode
                : "vi-VN";

            if (LocalizedText.TryGetValue(languageCode, out var texts) && texts.TryGetValue(key, out var value))
                return value;

            return LocalizedText["vi-VN"].TryGetValue(key, out var fallback) ? fallback : key;
        }

        public string Format(string key, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, T(key), args);
        }

        public string GetAccentColorDisplayName(AccentColorOption option)
        {
            return T(option.DisplayKey);
        }


        public string GetBackgroundThemeDisplayName(BackgroundThemeOption option)
        {
            return T(option.DisplayKey);
        }
        public string LocalizeCategoryName(string? categoryName, TransactionType? type = null)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return T("UncategorizedText");

            if (type.HasValue)
            {
                var typedKey = $"Category_{type.Value}_{categoryName}";
                var typedValue = T(typedKey);
                if (typedValue != typedKey)
                    return typedValue;
            }

            var key = $"Category_{categoryName}";
            var value = T(key);
            return value == key ? categoryName : value;
        }

        public string LocalizeTransactionType(TransactionType type)
        {
            return type == TransactionType.Income ? T("IncomeTypeText") : T("ExpenseTypeText");
        }
public void SetDarkMode(bool isDarkMode)
        {
            if (Settings.IsDarkMode == isDarkMode)
                return;

            Settings.IsDarkMode = isDarkMode;
            ApplyCurrentSettings();
        }

        public void SetAccentColor(string accentColor)
        {
            if (AccentColors.All(item => item.Name != accentColor) || Settings.AccentColor == accentColor)
                return;

            Settings.AccentColor = accentColor;
            ApplyCurrentSettings();
        }


        public void SetBackgroundTheme(string backgroundTheme)
        {
            if (BackgroundThemes.All(item => item.Name != backgroundTheme) || Settings.BackgroundTheme == backgroundTheme)
                return;

            Settings.BackgroundTheme = backgroundTheme;
            ApplyCurrentSettings();
        }
        public void SetLanguage(string languageCode)
        {
            if (Languages.All(item => item.Code != languageCode))
                return;

            Settings.LanguageCode = languageCode;
            ApplyCulture();
            ApplyLocalization(Application.Current?.Resources);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Save()
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(Settings, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }

        public void ApplyCurrentSettings()
        {
            ApplyCulture();

            var resources = Application.Current?.Resources;
            if (resources == null)
                return;

            ApplyLocalization(resources);

            var accent = AccentColors.FirstOrDefault(item => item.Name == Settings.AccentColor) ?? AccentColors[0];
            var background = BackgroundThemes.FirstOrDefault(item => item.Name == Settings.BackgroundTheme) ?? BackgroundThemes[0];
            var dark = Settings.IsDarkMode;

            var paletteHelper = new PaletteHelper();
            var materialTheme = paletteHelper.GetTheme();
            materialTheme.SetBaseTheme(dark ? BaseTheme.Dark : BaseTheme.Light);
            paletteHelper.SetTheme(materialTheme);

            SetBrush(resources, "BackgroundBrush", dark ? background.DarkBackground : background.LightBackground);
            SetBrush(resources, "CardBgBrush", dark ? background.DarkCard : background.LightCard);
            SetBrush(resources, "MutedCardBrush", dark ? background.DarkMuted : background.LightMuted);
            SetBrush(resources, "BorderBrushSoft", dark ? background.DarkBorder : background.LightBorder);
            SetBrush(resources, "InputBackgroundBrush", dark ? background.DarkInput : background.LightInput);
            SetBrush(resources, "InputBorderBrush", dark ? background.DarkInputBorder : background.LightInputBorder);
            SetBrush(resources, "SidebarHoverBrush", dark ? WithAlpha(accent.PrimaryColor, "33") : WithAlpha(accent.PrimaryColor, "18"));
            SetBrush(resources, "SidebarPressedBrush", dark ? WithAlpha(accent.PrimaryColor, "4D") : WithAlpha(accent.PrimaryColor, "2E"));
            SetBrush(resources, "SidebarUserBrush", dark ? WithAlpha(accent.SecondaryColor, "2B") : WithAlpha(accent.SecondaryColor, "18"));
            SetBrush(resources, "SidebarUserBorderBrush", dark ? WithAlpha(accent.SecondaryColor, "55") : WithAlpha(accent.SecondaryColor, "38"));
            SetBrush(resources, "PrimaryTextBrush", dark ? "#F8FAFC" : "#0F172A");
            SetBrush(resources, "SecondaryTextBrush", dark ? "#A8B4C5" : "#64748B");
            SetBrush(resources, "LightTextBrush", dark ? "#7B8AA0" : "#94A3B8");
            SetBrush(resources, "PrimaryBrush", accent.PrimaryColor);
            SetBrush(resources, "PrimaryDarkBrush", accent.SecondaryColor);
            SetBrush(resources, "SecondaryBrush", accent.SecondaryColor);
            SetBrush(resources, "AccentBrush", accent.SecondaryColor);

            SetGradient(resources, "PrimaryGradient", accent.PrimaryColor, accent.SecondaryColor, accent.SecondaryColor);
            SetGradient(resources, "BackgroundGradient",
                dark ? background.DarkBackground : background.LightBackground,
                dark ? background.DarkMuted : background.LightMuted,
                dark ? background.DarkCard : background.LightCard);
            SetGradient(resources, "SidebarGradient",
                dark ? background.DarkCard : background.LightCard,
                dark ? background.DarkMuted : background.LightMuted);
            SetGradient(resources, "CardSoftGradient",
                dark ? background.DarkCard : background.LightCard,
                dark ? background.DarkInput : background.LightInput);

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ApplyLocalization(ResourceDictionary? resources)
        {
            if (resources == null)
                return;

            var texts = LocalizedText.TryGetValue(Settings.LanguageCode, out var selected)
                ? selected
                : LocalizedText["vi-VN"];

            foreach (var item in texts)
                resources[item.Key] = item.Value;
        }

        private AppearanceSettings Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                    return JsonSerializer.Deserialize<AppearanceSettings>(File.ReadAllText(_settingsPath))
                        ?? new AppearanceSettings();
            }
            catch
            {
            }

            return new AppearanceSettings();
        }

        private void ApplyCulture()
        {
            var language = Languages.FirstOrDefault(item => item.Code == Settings.LanguageCode) ?? Languages[0];
            var culture = CultureInfo.GetCultureInfo(language.Code);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        private static string WithAlpha(string color, string alpha)
        {
            return color.StartsWith("#", StringComparison.Ordinal) && color.Length == 7
                ? $"#{alpha}{color[1..]}"
                : color;
        }
        private static void SetBrush(ResourceDictionary resources, string key, string color)
        {
            resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        private static void SetGradient(ResourceDictionary resources, string key, params string[] colors)
        {
            var gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, colors.Length == 2 ? 1 : 0)
            };

            for (var index = 0; index < colors.Length; index++)
            {
                gradient.GradientStops.Add(new GradientStop(
                    (Color)ColorConverter.ConvertFromString(colors[index]),
                    colors.Length == 1 ? 0 : (double)index / (colors.Length - 1)));
            }

            resources[key] = gradient;
        }

        private static readonly Dictionary<string, Dictionary<string, string>> LocalizedText = new()
        {
            ["vi-VN"] = new Dictionary<string, string>
            {
                ["WindowTitleText"] = "MoneyFlow - Quản lý tài chính cá nhân",
                ["UserFallbackText"] = "Người dùng",
                ["AccentCyanText"] = "Xanh ngọc",
                ["AccentBlueText"] = "Xanh dương",
                ["AccentVioletText"] = "Tím",
                ["AccentEmeraldText"] = "Xanh lá",
                ["AccentRoseText"] = "Hồng",
                ["AccentTravelText"] = "Xanh ng\u1ECDc",
                ["AccentBusinessText"] = "Xanh d\u01B0\u01A1ng",
                ["AccentHealthText"] = "\u0110\u1ECF h\u1ED3ng",
                ["AccentBonusText"] = "Xanh chanh",
                ["AccentTransportText"] = "Xanh tr\u1EDDi",
                ["AccentSalaryText"] = "Xanh l\u00E1",
                ["AccentShoppingText"] = "H\u1ED3ng",
                ["AccentFoodText"] = "Cam",
                ["AccentEntertainmentText"] = "T\u00EDm",
                ["AccentBillsText"] = "V\u00E0ng",
                ["IncomeTypeText"] = "Thu nhập",
                ["ExpenseTypeText"] = "Chi tiêu",
                ["UncategorizedText"] = "Chưa phân loại",
                ["AllFilterText"] = "Tất cả",
                ["SaveText"] = "Lưu",
                ["DeleteText"] = "Xóa",
                ["CloseText"] = "Đóng",
                ["CancelText"] = "Hủy",
                ["EditText"] = "Chỉnh sửa",
                ["FilterText"] = "Lọc",
                ["FromToText"] = "đến",
                ["LoginWelcomeText"] = "Chào mừng trở lại",
                ["LoginSubtitleText"] = "Đăng nhập để tiếp tục quản lý tài chính",
                ["UsernameLabel"] = "Tên đăng nhập",
                ["PasswordLabel"] = "Mật khẩu",
                ["LoginButtonText"] = "Đăng nhập",
                ["UsernamePlaceholderText"] = "Nh\u1EADp t\u00EAn \u0111\u0103ng nh\u1EADp",
                ["PasswordPlaceholderText"] = "Nh\u1EADp m\u1EADt kh\u1EA9u",
                ["FullNamePlaceholderText"] = "Nh\u1EADp h\u1ECD v\u00E0 t\u00EAn",
                ["EmailPlaceholderText"] = "Nh\u1EADp email c\u1EE7a b\u1EA1n",
                ["ConfirmPasswordPlaceholderText"] = "Nh\u1EADp l\u1EA1i m\u1EADt kh\u1EA9u",
                ["RememberLoginText"] = "Ghi nh\u1EDB \u0111\u0103ng nh\u1EADp",
                ["ForgotPasswordText"] = "Qu\u00EAn m\u1EADt kh\u1EA9u?",                ["OrText"] = "HOẶC",
                ["NoAccountText"] = "Chưa có tài khoản? ",
                ["RegisterNowText"] = "Đăng ký ngay",
                ["RegisterTitleText"] = "Tạo tài khoản",
                ["RegisterSubtitleText"] = "Bắt đầu quản lý tài chính của bạn",
                ["FullNameLabel"] = "Họ và tên",
                ["ConfirmPasswordLabel"] = "Xác nhận mật khẩu",
                ["RegisterButtonText"] = "Đăng ký",
                ["AlreadyHaveAccountText"] = "Đã có tài khoản? ",                ["AppSubtitleText"] = "Quản lý tài chính",
                ["MenuText"] = "MENU",
                ["NavDashboardText"] = "Dashboard",
                ["NavTransactionsText"] = "Giao dịch",
                ["NavCategoriesText"] = "Danh mục",
                ["NavReportsText"] = "Báo cáo",
                ["NavBudgetsText"] = "Ngân sách",
                ["NavGoalsText"] = "Mục tiêu",
                ["NavProfileText"] = "Hồ sơ",
                ["NavLogoutText"] = "Đăng xuất",
                ["PageLoginTitle"] = "Đăng nhập",
                ["PageRegisterTitle"] = "Đăng ký",
                ["PageDashboardTitle"] = "Dashboard",
                ["PageTransactionsTitle"] = "Giao dịch",
                ["PageCategoriesTitle"] = "Danh mục",
                ["PageBudgetsTitle"] = "Ngân sách",
                ["PageGoalsTitle"] = "Mục tiêu tài chính",
                ["PageReportsTitle"] = "Báo cáo & Thống kê",
                ["PageProfileTitle"] = "Hồ sơ cá nhân",
                ["PageErrorTitle"] = "Có lỗi",
                ["DashboardTotalIncomeText"] = "Tổng thu",
                ["DashboardTotalExpenseText"] = "Tổng chi",
                ["DashboardBalanceText"] = "Số dư",
                ["DashboardSavingsText"] = "Tiết kiệm",
                ["DashboardBudgetWarningTitle"] = "Cảnh báo ngân sách",
                ["DashboardCashFlowTitle"] = "Thu chi 6 tháng gần nhất",
                ["DashboardExpenseDistributionTitle"] = "Phân bổ chi tiêu",
                ["DashboardNoExpenseDataText"] = "Chưa có dữ liệu chi tiêu.",
                ["DashboardCurrentMonthText"] = "Tháng hiện tại",
                ["DashboardRecentTransactionsTitle"] = "Giao dịch gần đây",
                ["DashboardNoTransactionsText"] = "Chưa có giao dịch trong tháng này.",
                ["DashboardBudgetProgressTitle"] = "Tiến độ ngân sách",
                ["DashboardNoBudgetsText"] = "Chưa có ngân sách đang hoạt động.",
                ["NoPreviousPeriodText"] = "Chưa có dữ liệu kỳ trước",
                ["ComparedWithLastMonthText"] = "so với tháng trước",
                ["BalanceDescriptionText"] = "Tổng thu trừ tổng chi",
                ["SavingsRateText"] = "tỷ lệ tiết kiệm",
                ["BillionUnitSuffix"] = "t\u1EF7",
                ["MillionUnitSuffix"] = "tr",
                ["UsedPercentFormat"] = "{0:N0}% đã sử dụng",
                ["TransactionAddText"] = "Thêm giao dịch",
                ["TransactionDescriptionLabel"] = "Mô tả / ghi chú giao dịch",
                ["AmountLabel"] = "Số tiền",
                ["TypeLabel"] = "Loại",
                ["CategoryLabel"] = "Danh mục",
                ["AddCategoryText"] = "Thêm danh mục",
                ["DateLabel"] = "Ngày",
                ["NewCategoryNameLabel"] = "Tên danh mục mới",
                ["ColorLabel"] = "Màu sắc",
                ["IconLabel"] = "Biểu tượng",
                ["SaveCategoryText"] = "Lưu danh mục",
                ["TransactionNameColumn"] = "Tên giao dịch",
                ["ActionsColumn"] = "Thao tác",
                ["NoMatchingTransactionsText"] = "Chưa có giao dịch phù hợp.",
                ["AddTransactionTitle"] = "Thêm giao dịch",
                ["EditTransactionTitle"] = "Chỉnh sửa giao dịch",
                ["FilteredCountFormat"] = "Hiển thị {0} / {1} giao dịch",
                ["CategoryAddText"] = "Thêm danh mục",
                ["CategoryNameLabel"] = "Tên danh mục",
                ["DeleteCategoryText"] = "Xóa danh mục",
                ["CategoryTransactionsText"] = "Giao dịch",
                ["BudgetText"] = "Ngân sách",
                ["ProgressText"] = "Tiến độ",
                ["CategoryDetailsTitle"] = "Chi tiết danh mục",
                ["TotalAmountColumn"] = "Số tiền",
                ["RemainingColumn"] = "Còn lại",
                ["UsedPercentColumn"] = "% sử dụng",
                ["StatusColumn"] = "Trạng thái",
                ["NoCategoriesText"] = "Chưa có danh mục. Hãy thêm danh mục đầu tiên.",
                ["AddCategoryTitle"] = "Thêm danh mục",
                ["EditCategoryTitle"] = "Chỉnh sửa danh mục",
                ["TotalSpentLabel"] = "Đã chi",
                ["TotalIncomeLabel"] = "Đã thu",
                ["NotSetText"] = "Chưa thiết lập",
                ["IncomeItemStatus"] = "Khoản thu",
                ["NoBudgetStatus"] = "Chưa lập ngân sách",
                ["OverBudgetStatus"] = "Vượt ngân sách",
                ["NeedsAttentionStatus"] = "Cần chú ý",
                ["StableStatus"] = "Ổn định",
                ["GoodStatus"] = "Đang ổn",                ["CreateBudgetText"] = "Tạo ngân sách",
                ["LimitLabel"] = "Hạn mức",
                ["StartDateLabel"] = "Ngày bắt đầu",
                ["EndDateLabel"] = "Ngày kết thúc",
                ["TotalBudgetText"] = "Tổng ngân sách",
                ["ActiveLimitText"] = "Hạn mức đang hoạt động",
                ["SpentText"] = "Đã chi tiêu",
                ["RemainingText"] = "Còn lại",
                ["UnusedAmountText"] = "Số tiền chưa sử dụng",
                ["BudgetDetailsTitle"] = "Chi tiết ngân sách",
                ["LimitPrefixText"] = "Hạn mức: ",
                ["RemainingPrefixText"] = "Còn lại: ",
                ["NoBudgetsText"] = "Chưa có ngân sách. Hãy tạo hạn mức đầu tiên.",
                ["CreateBudgetTitle"] = "Tạo ngân sách",
                ["EditBudgetTitle"] = "Chỉnh sửa ngân sách",
                ["BudgetUsedFormat"] = "{0:N0}% ngân sách đã sử dụng",
                ["BudgetDeleteConfirm"] = "Xóa ngân sách đang chọn?",
                ["CreateGoalText"] = "Tạo mục tiêu",
                ["GoalNameLabel"] = "Tên mục tiêu",
                ["TargetAmountLabel"] = "Số tiền mục tiêu",
                ["TargetDateLabel"] = "Ngày hoàn thành",
                ["DescriptionLabel"] = "Mô tả",
                ["SaveGoalText"] = "Lưu mục tiêu",
                ["AddMoneyToGoalText"] = "Nạp tiền vào mục tiêu",
                ["TrackedGoalsText"] = "Mục tiêu đang theo dõi",
                ["TotalGoalsText"] = "Tổng mục tiêu",
                ["TargetValueText"] = "Giá trị cần tích lũy",
                ["SavedText"] = "Đã tiết kiệm",
                ["AllocatedTotalText"] = "Tổng tiền đã phân bổ",
                ["CompletedGoalsText"] = "Mục tiêu đã hoàn thành",
                ["NoActiveGoalsText"] = "Chưa có mục tiêu đang theo dõi.",
                ["GoalTipTitle"] = "Mẹo đạt mục tiêu nhanh hơn",
                ["GoalTipText"] = "Đặt mục tiêu rõ ràng, theo dõi tiến độ thường xuyên và chia nhỏ số tiền cần tích lũy theo tháng.",
                ["CreateGoalTitle"] = "Tạo mục tiêu",
                ["EditGoalTitle"] = "Chỉnh sửa mục tiêu",
                ["CompletedGoalsCountFormat"] = "{0} mục tiêu đã hoàn thành",
                ["GoalCompletedDateFormat"] = "Hoàn thành: {0:d}",
                ["GoalDeadlineFormat"] = "Hạn: {0:d}",
                ["GoalRemainingFormat"] = "Còn {0}",                ["ExportReportText"] = "Xuất báo cáo",
                ["MonthText"] = "Tháng",
                ["QuarterText"] = "Quý",
                ["YearText"] = "Năm",
                ["ReportTotalIncomeText"] = "Tổng thu nhập",
                ["ReportTotalExpenseText"] = "Tổng chi tiêu",
                ["ReportTotalSavingsText"] = "Tổng tiết kiệm",
                ["CashFlowTrendTitle"] = "Xu hướng thu chi",
                ["CategoryExpenseDistributionTitle"] = "Phân bổ chi tiêu theo danh mục",
                ["DailyExpenseTitle"] = "Chi tiêu hàng ngày",
                ["InsightsTitle"] = "Nhận xét & đề xuất",
                ["PositiveInsightTitle"] = "Điểm tốt",
                ["WarningInsightTitle"] = "Cần chú ý",
                ["IncomeInPeriodText"] = "Tổng khoản thu trong kỳ",
                ["ExpenseInPeriodText"] = "Tổng khoản chi trong kỳ",
                ["PositiveCashFlowText"] = "Dòng tiền đang dương",
                ["ExpenseOverIncomeText"] = "Chi tiêu đang vượt thu nhập",
                ["CashFlowSummaryDefault"] = "Theo dõi biến động thu nhập và chi tiêu trong khoảng thời gian đã chọn.",
                ["DailyDataSummaryFormat"] = "Hiển thị theo ngày, {0} mốc dữ liệu trong kỳ.",
                ["WeeklyDataSummaryFormat"] = "Hiển thị theo tuần, {0} mốc dữ liệu trong kỳ.",
                ["MonthlyDataSummaryFormat"] = "Hiển thị theo tháng, {0} mốc dữ liệu trong kỳ.",
                ["NoTransactionsInPeriodText"] = "Chưa có giao dịch trong khoảng thời gian đã chọn.",
                ["TrackRegularlyInsight"] = "Hãy ghi nhận giao dịch đều đặn để theo dõi tài chính chính xác hơn.",
                ["PositiveSavingsInsightFormat"] = "Dòng tiền dương. Bạn đang giữ lại {0:N0}% thu nhập trong kỳ.",
                ["RebalanceInsightText"] = "Các giao dịch đã được tổng hợp. Hãy cân đối lại dòng tiền trong kỳ tiếp theo.",
                ["NoMajorExpenseInsight"] = "Chưa có khoản chi nổi bật cần lưu ý.",
                ["LargestCategoryInsightFormat"] = "Danh mục \"{0}\" đang chiếm mức chi lớn nhất: {1}.",
                ["ExportReportDialogTitle"] = "Xuất báo cáo",
                ["CsvHeaderText"] = "Ngày,Nội dung,Danh mục,Loại,Số tiền",                ["ProfileChooseAvatarText"] = "Chọn ảnh",
                ["ProfileClearAvatarText"] = "Gỡ ảnh",
                ["ProfileAccountBadgeText"] = "Tài khoản MoneyFlow",
                ["ProfileIntroText"] = "Cập nhật ảnh đại diện, thông tin cá nhân và tùy chỉnh giao diện ở phần bên phải.",
                ["ProfileInfoTitle"] = "Thông tin cá nhân",
                ["ProfileFullNameLabel"] = "Họ và tên",
                ["ProfileEmailLabel"] = "Email",
                ["ProfileSaveChangesText"] = "Lưu thay đổi",
                ["ProfileSecurityTitle"] = "Bảo mật",
                ["ProfileCurrentPasswordLabel"] = "Mật khẩu hiện tại",
                ["ProfileNewPasswordLabel"] = "Mật khẩu mới",
                ["ProfileConfirmPasswordLabel"] = "Xác nhận mật khẩu",
                ["ProfileChangePasswordText"] = "Đổi mật khẩu",
                ["ProfileAppearanceTitle"] = "Giao diện và ngôn ngữ",
                ["ProfileDisplayModeLabel"] = "Chế độ hiển thị",
                ["ProfileDarkModeText"] = "Bật giao diện tối",
                ["ProfileBackgroundThemeLabel"] = "M\u00E0u n\u1EC1n giao di\u1EC7n",
                ["BackgroundTravelText"] = "Xanh ng\u1ECDc",
                ["BackgroundBusinessText"] = "Xanh d\u01B0\u01A1ng",
                ["BackgroundHealthText"] = "\u0110\u1ECF h\u1ED3ng",
                ["BackgroundBonusText"] = "Xanh chanh",
                ["BackgroundTransportText"] = "Xanh tr\u1EDDi",
                ["BackgroundSalaryText"] = "Xanh l\u00E1",
                ["BackgroundShoppingText"] = "H\u1ED3ng",
                ["BackgroundFoodText"] = "Cam",
                ["BackgroundEntertainmentText"] = "T\u00EDm",
                ["BackgroundBillsText"] = "V\u00E0ng",
                ["ProfileAccentColorLabel"] = "Màu chủ đạo",
                ["ProfileLanguageLabel"] = "Ngôn ngữ",
                ["ProfileSaveAppearanceText"] = "Lưu tùy chỉnh",
                ["ProfileLanguageHintText"] = "Ngôn ngữ được áp dụng ngay và sẽ được lưu cho lần mở ứng dụng tiếp theo.",
                ["ProfileMemberSinceFormat"] = "Thành viên từ {0:dd/MM/yyyy}",
                ["ProfileSavedAppearanceMessage"] = "Đã lưu tùy chỉnh giao diện và ngôn ngữ.",
                ["Category_Expense_Ăn uống"] = "Ăn uống",
                ["Category_Expense_Mua sắm"] = "Mua sắm",
                ["Category_Expense_Giao thông"] = "Giao thông",
                ["Category_Expense_Giải trí"] = "Giải trí",
                ["Category_Expense_Y tế"] = "Y tế",
                ["Category_Expense_Hóa đơn"] = "Hóa đơn",
                ["Category_Expense_Khác"] = "Khác",
                ["Category_Income_Lương"] = "Lương",
                ["Category_Income_Thưởng"] = "Thưởng",
                ["Category_Income_Freelance"] = "Freelance",
                ["Category_Income_Kinh doanh"] = "Kinh doanh",
                ["Category_Income_Khác"] = "Khác",
                ["Category_Mục tiêu"] = "Mục tiêu",
                ["DefaultBudgetName"] = "Ngân sách",
                ["GoalAllocationCategoryName"] = "Mục tiêu",
                ["GoalAllocationDescriptionFormat"] = "Phân bổ vào mục tiêu: {0}",
                ["TransactionDescriptionRequired"] = "Vui lòng nhập mô tả giao dịch.",
                ["AmountGreaterThanZero"] = "Số tiền phải lớn hơn 0.",
                ["CategoryRequired"] = "Vui lòng chọn danh mục.",
                ["ValidCategoryRequired"] = "Vui lòng chọn danh mục hợp lệ.",
                ["DeleteTransactionConfirmFormat"] = "Xóa giao dịch '{0}'?",
                ["CategoryNameRequired"] = "Vui lòng nhập tên danh mục.",
                ["InvalidColorMessage"] = "Màu sắc phải có định dạng #RRGGBB, ví dụ #0EA5E9.",
                ["CategorySaveFailed"] = "Không thể lưu danh mục. Có thể tên đã tồn tại.",
                ["DeleteCategoryConfirmFormat"] = "Xóa danh mục '{0}'?",
                ["BudgetInputInvalid"] = "Vui lòng chọn danh mục, nhập hạn mức > 0 và ngày hợp lệ.",
                ["BudgetSaveFailed"] = "Không thể lưu ngân sách. Kiểm tra thời gian trùng hoặc hạn mức.",
                ["GoalNameRequired"] = "Vui lòng nhập tên mục tiêu.",
                ["GoalAmountInvalid"] = "Số tiền mục tiêu phải lớn hơn 0.",
                ["GoalSaveFailed"] = "Không thể lưu mục tiêu.",
                ["SelectGoalBeforeAdding"] = "Vui lòng chọn một mục tiêu trước khi nạp tiền.",
                ["GoalAddAmountInvalid"] = "Số tiền thêm vào mục tiêu phải lớn hơn 0.",
                ["SelectGoalBeforeDelete"] = "Vui lòng chọn một mục tiêu trước khi xóa.",
                ["DeleteGoalConfirmFormat"] = "Xóa mục tiêu '{0}'?",
                ["GoalDeleteFailed"] = "Không thể xóa mục tiêu.",
                ["InvalidDateRange"] = "Ngày bắt đầu không được lớn hơn ngày kết thúc.",
                ["NotFoundTransaction"] = "Không tìm thấy giao dịch.",
                ["NotFoundCategory"] = "Không tìm thấy danh mục.",
                ["NotFoundBudget"] = "Không tìm thấy ngân sách.",
                ["NotFoundGoal"] = "Không tìm thấy mục tiêu.",
                ["CategoryHasTransactions"] = "Không thể xóa danh mục đã có giao dịch.",
                ["CategoryHasBudgets"] = "Không thể xóa danh mục đang có ngân sách.",
                ["GoalAlreadyCompleted"] = "Mục tiêu này đã hoàn thành.",
                ["GoalAddExceedsRemainingFormat"] = "Số tiền thêm vào vượt quá số còn thiếu của mục tiêu: {0}.",
                ["InsufficientBalanceFormat"] = "Số dư khả dụng không đủ!\n\nSố dư hiện tại: {0}\nĐã phân bổ vào mục tiêu: {1}\nSố dư khả dụng: {2}\nSố tiền muốn thêm: {3}",
                ["GoalCompletedMessageFormat"] = "Chúc mừng! Bạn đã hoàn thành mục tiêu '{0}'!",
                ["GoalAddedMoneyMessageFormat"] = "Đã thêm {0} vào mục tiêu '{1}'!",
                ["BudgetOverFormat"] = "{0}: đã vượt {1:F1}%",
                ["BudgetWarningFormat"] = "{0}: đã dùng {1:F1}%",
                ["ProfileSavedMessage"] = "Đã lưu hồ sơ.",
                ["ProfileSaveFailedMessage"] = "Không thể lưu hồ sơ. Email có thể đã tồn tại.",
                ["ProfileAvatarSelectedMessage"] = "Đã chọn ảnh đại diện. Nhấn Lưu thay đổi để cập nhật hồ sơ.",
                ["ProfileAvatarClearedMessage"] = "Đã gỡ ảnh đại diện. Nhấn Lưu thay đổi để cập nhật hồ sơ.",
                ["ChooseAvatarDialogTitle"] = "Chọn ảnh đại diện",
                ["AvatarFilterText"] = "Ảnh đại diện (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Tất cả tệp (*.*)|*.*",
                ["DialogInfoTitle"] = "Thông báo",
                ["DialogErrorTitle"] = "Lỗi",
                ["DialogConfirmTitle"] = "Xác nhận",
                ["GenericErrorFormat"] = "Có lỗi xảy ra: {0}",
                ["LoadScreenErrorFormat"] = "Không thể tải dữ liệu màn hình: {0}",
                ["OpenScreenErrorFormat"] = "Không thể mở màn hình {0}: {1}",
                ["DatabaseConnectionError"] = "Không thể kết nối hoặc cập nhật CSDL. Vui lòng kiểm tra SQL Server/LocalDB rồi mở lại ứng dụng.",
                ["UnhandledUiErrorFormat"] = "Ứng dụng gặp lỗi khi xử lý màn hình: {0}",
                ["InvalidEmailMessage"] = "Email không hợp lệ.",
                ["PasswordMismatchMessage"] = "Mật khẩu xác nhận không khớp.",
                ["RequiredLoginMessage"] = "Vui lòng nhập tên đăng nhập và mật khẩu.",
                ["InvalidLoginMessage"] = "Tên đăng nhập hoặc mật khẩu không đúng.",
                ["InvalidUsernameMessage"] = "Tên đăng nhập không được để trống hoặc chứa khoảng trắng.",
                ["PasswordTooShortMessage"] = "Mật khẩu phải có ít nhất 6 ký tự.",
                ["RegisterSuccessMessage"] = "Đăng ký thành công. Hãy đăng nhập.",
                ["RegisterExistsMessage"] = "Tên đăng nhập hoặc email đã tồn tại.",
                ["CurrentPasswordRequiredMessage"] = "Bạn cần đăng nhập.",
                ["NewPasswordTooShortMessage"] = "Mật khẩu mới phải có ít nhất 6 ký tự.",
                ["AccountNotFoundMessage"] = "Không tìm thấy tài khoản.",
                ["CurrentPasswordIncorrectMessage"] = "Mật khẩu hiện tại không đúng.",
                ["PasswordChangedMessage"] = "Đổi mật khẩu thành công.",
            },
            ["en-US"] = new Dictionary<string, string>
            {
                ["WindowTitleText"] = "MoneyFlow - Personal Finance Manager",
                ["UserFallbackText"] = "User",
                ["AccentCyanText"] = "Cyan",
                ["AccentBlueText"] = "Blue",
                ["AccentVioletText"] = "Violet",
                ["AccentEmeraldText"] = "Emerald",
                ["AccentRoseText"] = "Rose",
                ["AccentTravelText"] = "Cyan",
                ["AccentBusinessText"] = "Blue",
                ["AccentHealthText"] = "Rose",
                ["AccentBonusText"] = "Lime",
                ["AccentTransportText"] = "Sky",
                ["AccentSalaryText"] = "Emerald",
                ["AccentShoppingText"] = "Pink",
                ["AccentFoodText"] = "Orange",
                ["AccentEntertainmentText"] = "Violet",
                ["AccentBillsText"] = "Amber",
                ["IncomeTypeText"] = "Income",
                ["ExpenseTypeText"] = "Expense",
                ["UncategorizedText"] = "Uncategorized",
                ["AllFilterText"] = "All",
                ["SaveText"] = "Save",
                ["DeleteText"] = "Delete",
                ["CloseText"] = "Close",
                ["CancelText"] = "Cancel",
                ["EditText"] = "Edit",
                ["FilterText"] = "Filter",
                ["FromToText"] = "to",
                ["LoginWelcomeText"] = "Welcome back",
                ["LoginSubtitleText"] = "Sign in to continue managing your finances",
                ["UsernameLabel"] = "Username",
                ["PasswordLabel"] = "Password",
                ["LoginButtonText"] = "Sign in",
                ["UsernamePlaceholderText"] = "Enter your username",
                ["PasswordPlaceholderText"] = "Enter your password",
                ["FullNamePlaceholderText"] = "Enter your full name",
                ["EmailPlaceholderText"] = "Enter your email",
                ["ConfirmPasswordPlaceholderText"] = "Re-enter your password",
                ["RememberLoginText"] = "Remember me",
                ["ForgotPasswordText"] = "Forgot password?",
                ["OrText"] = "OR",
                ["NoAccountText"] = "Don't have an account? ",
                ["RegisterNowText"] = "Sign up now",
                ["RegisterTitleText"] = "Create account",
                ["RegisterSubtitleText"] = "Start managing your finances",
                ["FullNameLabel"] = "Full name",
                ["ConfirmPasswordLabel"] = "Confirm password",
                ["RegisterButtonText"] = "Sign up",
                ["AlreadyHaveAccountText"] = "Already have an account? ",                ["AppSubtitleText"] = "Personal finance",
                ["MenuText"] = "MENU",
                ["NavDashboardText"] = "Dashboard",
                ["NavTransactionsText"] = "Transactions",
                ["NavCategoriesText"] = "Categories",
                ["NavReportsText"] = "Reports",
                ["NavBudgetsText"] = "Budgets",
                ["NavGoalsText"] = "Goals",
                ["NavProfileText"] = "Profile",
                ["NavLogoutText"] = "Log out",
                ["PageLoginTitle"] = "Sign in",
                ["PageRegisterTitle"] = "Create account",
                ["PageDashboardTitle"] = "Dashboard",
                ["PageTransactionsTitle"] = "Transactions",
                ["PageCategoriesTitle"] = "Categories",
                ["PageBudgetsTitle"] = "Budgets",
                ["PageGoalsTitle"] = "Financial Goals",
                ["PageReportsTitle"] = "Reports & Analytics",
                ["PageProfileTitle"] = "Profile",
                ["PageErrorTitle"] = "Something went wrong",
                ["DashboardTotalIncomeText"] = "Total income",
                ["DashboardTotalExpenseText"] = "Total expenses",
                ["DashboardBalanceText"] = "Balance",
                ["DashboardSavingsText"] = "Savings",
                ["DashboardBudgetWarningTitle"] = "Budget warning",
                ["DashboardCashFlowTitle"] = "Income and expenses over the last 6 months",
                ["DashboardExpenseDistributionTitle"] = "Expense distribution",
                ["DashboardNoExpenseDataText"] = "No expense data yet.",
                ["DashboardCurrentMonthText"] = "Current month",
                ["DashboardRecentTransactionsTitle"] = "Recent transactions",
                ["DashboardNoTransactionsText"] = "No transactions this month.",
                ["DashboardBudgetProgressTitle"] = "Budget progress",
                ["DashboardNoBudgetsText"] = "No active budgets yet.",
                ["NoPreviousPeriodText"] = "No previous period data",
                ["ComparedWithLastMonthText"] = "compared with last month",
                ["BalanceDescriptionText"] = "Total income minus total expenses",
                ["SavingsRateText"] = "savings rate",
                ["BillionUnitSuffix"] = "B",
                ["MillionUnitSuffix"] = "M",
                ["UsedPercentFormat"] = "{0:N0}% used",
                ["TransactionAddText"] = "Add transaction",
                ["TransactionDescriptionLabel"] = "Transaction description / note",
                ["AmountLabel"] = "Amount",
                ["TypeLabel"] = "Type",
                ["CategoryLabel"] = "Category",
                ["AddCategoryText"] = "Add category",
                ["DateLabel"] = "Date",
                ["NewCategoryNameLabel"] = "New category name",
                ["ColorLabel"] = "Color",
                ["IconLabel"] = "Icon",
                ["SaveCategoryText"] = "Save category",
                ["TransactionNameColumn"] = "Transaction name",
                ["ActionsColumn"] = "Actions",
                ["NoMatchingTransactionsText"] = "No matching transactions.",
                ["AddTransactionTitle"] = "Add transaction",
                ["EditTransactionTitle"] = "Edit transaction",
                ["FilteredCountFormat"] = "Showing {0} / {1} transactions",
                ["CategoryAddText"] = "Add category",
                ["CategoryNameLabel"] = "Category name",
                ["DeleteCategoryText"] = "Delete category",
                ["CategoryTransactionsText"] = "Transactions",
                ["BudgetText"] = "Budget",
                ["ProgressText"] = "Progress",
                ["CategoryDetailsTitle"] = "Category details",
                ["TotalAmountColumn"] = "Amount",
                ["RemainingColumn"] = "Remaining",
                ["UsedPercentColumn"] = "% used",
                ["StatusColumn"] = "Status",
                ["NoCategoriesText"] = "No categories yet. Add your first category.",
                ["AddCategoryTitle"] = "Add category",
                ["EditCategoryTitle"] = "Edit category",
                ["TotalSpentLabel"] = "Spent",
                ["TotalIncomeLabel"] = "Received",
                ["NotSetText"] = "Not set",
                ["IncomeItemStatus"] = "Income item",
                ["NoBudgetStatus"] = "No budget",
                ["OverBudgetStatus"] = "Over budget",
                ["NeedsAttentionStatus"] = "Needs attention",
                ["StableStatus"] = "Stable",
                ["GoodStatus"] = "On track",                ["CreateBudgetText"] = "Create budget",
                ["LimitLabel"] = "Limit",
                ["StartDateLabel"] = "Start date",
                ["EndDateLabel"] = "End date",
                ["TotalBudgetText"] = "Total budget",
                ["ActiveLimitText"] = "Active limits",
                ["SpentText"] = "Spent",
                ["RemainingText"] = "Remaining",
                ["UnusedAmountText"] = "Unused amount",
                ["BudgetDetailsTitle"] = "Budget details",
                ["LimitPrefixText"] = "Limit: ",
                ["RemainingPrefixText"] = "Remaining: ",
                ["NoBudgetsText"] = "No budgets yet. Create your first limit.",
                ["CreateBudgetTitle"] = "Create budget",
                ["EditBudgetTitle"] = "Edit budget",
                ["BudgetUsedFormat"] = "{0:N0}% of budget used",
                ["BudgetDeleteConfirm"] = "Delete the selected budget?",
                ["CreateGoalText"] = "Create goal",
                ["GoalNameLabel"] = "Goal name",
                ["TargetAmountLabel"] = "Target amount",
                ["TargetDateLabel"] = "Target date",
                ["DescriptionLabel"] = "Description",
                ["SaveGoalText"] = "Save goal",
                ["AddMoneyToGoalText"] = "Add money to goal",
                ["TrackedGoalsText"] = "Tracked goals",
                ["TotalGoalsText"] = "Total goals",
                ["TargetValueText"] = "Target value",
                ["SavedText"] = "Saved",
                ["AllocatedTotalText"] = "Total allocated amount",
                ["CompletedGoalsText"] = "Completed goals",
                ["NoActiveGoalsText"] = "No active goals yet.",
                ["GoalTipTitle"] = "Tips to reach goals faster",
                ["GoalTipText"] = "Set clear goals, track progress regularly, and split the amount you need to save by month.",
                ["CreateGoalTitle"] = "Create goal",
                ["EditGoalTitle"] = "Edit goal",
                ["CompletedGoalsCountFormat"] = "{0} completed goals",
                ["GoalCompletedDateFormat"] = "Completed: {0:d}",
                ["GoalDeadlineFormat"] = "Due: {0:d}",
                ["GoalRemainingFormat"] = "Remaining {0}",                ["ExportReportText"] = "Export report",
                ["MonthText"] = "Month",
                ["QuarterText"] = "Quarter",
                ["YearText"] = "Year",
                ["ReportTotalIncomeText"] = "Total income",
                ["ReportTotalExpenseText"] = "Total expenses",
                ["ReportTotalSavingsText"] = "Total savings",
                ["CashFlowTrendTitle"] = "Cash flow trend",
                ["CategoryExpenseDistributionTitle"] = "Expense distribution by category",
                ["DailyExpenseTitle"] = "Daily expenses",
                ["InsightsTitle"] = "Insights & suggestions",
                ["PositiveInsightTitle"] = "Positive",
                ["WarningInsightTitle"] = "Needs attention",
                ["IncomeInPeriodText"] = "Total income in this period",
                ["ExpenseInPeriodText"] = "Total expenses in this period",
                ["PositiveCashFlowText"] = "Cash flow is positive",
                ["ExpenseOverIncomeText"] = "Expenses are exceeding income",
                ["CashFlowSummaryDefault"] = "Track income and expense changes across the selected period.",
                ["DailyDataSummaryFormat"] = "Showing daily data across {0} points in this period.",
                ["WeeklyDataSummaryFormat"] = "Showing weekly data across {0} points in this period.",
                ["MonthlyDataSummaryFormat"] = "Showing monthly data across {0} points in this period.",
                ["NoTransactionsInPeriodText"] = "No transactions in the selected period.",
                ["TrackRegularlyInsight"] = "Record transactions regularly to track your finances more accurately.",
                ["PositiveSavingsInsightFormat"] = "Cash flow is positive. You kept {0:N0}% of income in this period.",
                ["RebalanceInsightText"] = "Transactions have been summarized. Consider rebalancing your cash flow in the next period.",
                ["NoMajorExpenseInsight"] = "No major expense category needs attention yet.",
                ["LargestCategoryInsightFormat"] = "Category \"{0}\" has the largest spending amount: {1}.",
                ["ExportReportDialogTitle"] = "Export report",
                ["CsvHeaderText"] = "Date,Description,Category,Type,Amount",                ["ProfileChooseAvatarText"] = "Choose photo",
                ["ProfileClearAvatarText"] = "Remove photo",
                ["ProfileAccountBadgeText"] = "MoneyFlow account",
                ["ProfileIntroText"] = "Update your avatar, personal information, and interface preferences on the right.",
                ["ProfileInfoTitle"] = "Personal Information",
                ["ProfileFullNameLabel"] = "Full name",
                ["ProfileEmailLabel"] = "Email",
                ["ProfileSaveChangesText"] = "Save changes",
                ["ProfileSecurityTitle"] = "Security",
                ["ProfileCurrentPasswordLabel"] = "Current password",
                ["ProfileNewPasswordLabel"] = "New password",
                ["ProfileConfirmPasswordLabel"] = "Confirm password",
                ["ProfileChangePasswordText"] = "Change password",
                ["ProfileAppearanceTitle"] = "Appearance & Language",
                ["ProfileDisplayModeLabel"] = "Display mode",
                ["ProfileDarkModeText"] = "Enable dark mode",
                ["ProfileBackgroundThemeLabel"] = "Background color",
                ["BackgroundTravelText"] = "Cyan",
                ["BackgroundBusinessText"] = "Blue",
                ["BackgroundHealthText"] = "Rose",
                ["BackgroundBonusText"] = "Lime",
                ["BackgroundTransportText"] = "Sky",
                ["BackgroundSalaryText"] = "Emerald",
                ["BackgroundShoppingText"] = "Pink",
                ["BackgroundFoodText"] = "Orange",
                ["BackgroundEntertainmentText"] = "Violet",
                ["BackgroundBillsText"] = "Amber",
                ["ProfileAccentColorLabel"] = "Accent color",
                ["ProfileLanguageLabel"] = "Language",
                ["ProfileSaveAppearanceText"] = "Save preferences",
                ["ProfileLanguageHintText"] = "Language is applied immediately and saved for the next time you open the app.",
                ["ProfileMemberSinceFormat"] = "Member since {0:MM/dd/yyyy}",
                ["ProfileSavedAppearanceMessage"] = "Appearance and language preferences saved.",
                ["Category_Expense_Ăn uống"] = "Food",
                ["Category_Expense_Mua sắm"] = "Shopping",
                ["Category_Expense_Giao thông"] = "Transport",
                ["Category_Expense_Giải trí"] = "Entertainment",
                ["Category_Expense_Y tế"] = "Healthcare",
                ["Category_Expense_Hóa đơn"] = "Bills",
                ["Category_Expense_Khác"] = "Other",
                ["Category_Income_Lương"] = "Salary",
                ["Category_Income_Thưởng"] = "Bonus",
                ["Category_Income_Freelance"] = "Freelance",
                ["Category_Income_Kinh doanh"] = "Business",
                ["Category_Income_Khác"] = "Other",
                ["Category_Mục tiêu"] = "Goals",
                ["DefaultBudgetName"] = "Budget",
                ["GoalAllocationCategoryName"] = "Goals",
                ["GoalAllocationDescriptionFormat"] = "Allocated to goal: {0}",
                ["TransactionDescriptionRequired"] = "Please enter a transaction description.",
                ["AmountGreaterThanZero"] = "Amount must be greater than 0.",
                ["CategoryRequired"] = "Please choose a category.",
                ["ValidCategoryRequired"] = "Please choose a valid category.",
                ["DeleteTransactionConfirmFormat"] = "Delete transaction '{0}'?",
                ["CategoryNameRequired"] = "Please enter a category name.",
                ["InvalidColorMessage"] = "Color must use #RRGGBB format, for example #0EA5E9.",
                ["CategorySaveFailed"] = "Could not save category. The name may already exist.",
                ["DeleteCategoryConfirmFormat"] = "Delete category '{0}'?",
                ["BudgetInputInvalid"] = "Please choose a category, enter a limit > 0, and use a valid date range.",
                ["BudgetSaveFailed"] = "Could not save budget. Check overlapping periods or the limit amount.",
                ["GoalNameRequired"] = "Please enter a goal name.",
                ["GoalAmountInvalid"] = "Target amount must be greater than 0.",
                ["GoalSaveFailed"] = "Could not save goal.",
                ["SelectGoalBeforeAdding"] = "Please choose a goal before adding money.",
                ["GoalAddAmountInvalid"] = "Amount added to the goal must be greater than 0.",
                ["SelectGoalBeforeDelete"] = "Please choose a goal before deleting it.",
                ["DeleteGoalConfirmFormat"] = "Delete goal '{0}'?",
                ["GoalDeleteFailed"] = "Could not delete goal.",
                ["InvalidDateRange"] = "Start date cannot be later than end date.",
                ["NotFoundTransaction"] = "Transaction not found.",
                ["NotFoundCategory"] = "Category not found.",
                ["NotFoundBudget"] = "Budget not found.",
                ["NotFoundGoal"] = "Goal not found.",
                ["CategoryHasTransactions"] = "Cannot delete a category that already has transactions.",
                ["CategoryHasBudgets"] = "Cannot delete a category that already has budgets.",
                ["GoalAlreadyCompleted"] = "This goal is already completed.",
                ["GoalAddExceedsRemainingFormat"] = "The added amount exceeds the goal's remaining amount: {0}.",
                ["InsufficientBalanceFormat"] = "Available balance is not enough!\n\nCurrent balance: {0}\nAllocated to goals: {1}\nAvailable balance: {2}\nAmount to add: {3}",
                ["GoalCompletedMessageFormat"] = "Congratulations! You completed goal '{0}'!",
                ["GoalAddedMoneyMessageFormat"] = "Added {0} to goal '{1}'!",
                ["BudgetOverFormat"] = "{0}: over {1:F1}%",
                ["BudgetWarningFormat"] = "{0}: used {1:F1}%",
                ["ProfileSavedMessage"] = "Profile saved.",
                ["ProfileSaveFailedMessage"] = "Could not save profile. The email may already exist.",
                ["ProfileAvatarSelectedMessage"] = "Photo selected. Click Save changes to update your profile.",
                ["ProfileAvatarClearedMessage"] = "Photo removed. Click Save changes to update your profile.",
                ["ChooseAvatarDialogTitle"] = "Choose profile photo",
                ["AvatarFilterText"] = "Profile photos (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*",
                ["DialogInfoTitle"] = "Information",
                ["DialogErrorTitle"] = "Error",
                ["DialogConfirmTitle"] = "Confirm",
                ["GenericErrorFormat"] = "An error occurred: {0}",
                ["LoadScreenErrorFormat"] = "Could not load screen data: {0}",
                ["OpenScreenErrorFormat"] = "Could not open {0}: {1}",
                ["DatabaseConnectionError"] = "Could not connect to or update the database. Please check SQL Server/LocalDB and reopen the app.",
                ["UnhandledUiErrorFormat"] = "The app hit an error while processing the screen: {0}",
                ["InvalidEmailMessage"] = "Invalid email address.",
                ["PasswordMismatchMessage"] = "Password confirmation does not match.",
                ["RequiredLoginMessage"] = "Please enter your username and password.",
                ["InvalidLoginMessage"] = "Username or password is incorrect.",
                ["InvalidUsernameMessage"] = "Username cannot be empty or contain spaces.",
                ["PasswordTooShortMessage"] = "Password must be at least 6 characters.",
                ["RegisterSuccessMessage"] = "Registration successful. Please sign in.",
                ["RegisterExistsMessage"] = "Username or email already exists.",
                ["CurrentPasswordRequiredMessage"] = "You need to sign in.",
                ["NewPasswordTooShortMessage"] = "New password must be at least 6 characters.",
                ["AccountNotFoundMessage"] = "Account not found.",
                ["CurrentPasswordIncorrectMessage"] = "Current password is incorrect.",
                ["PasswordChangedMessage"] = "Password changed successfully.",
            }
        };
    }
}









