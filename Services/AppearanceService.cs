using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace QuanLyTaiChinhCaNhan_Nhom06.Services
{
    public interface IAppearanceService
    {
        AppearanceSettings Settings { get; }
        IReadOnlyList<AccentColorOption> AccentColors { get; }
        IReadOnlyList<LanguageOption> Languages { get; }
        event EventHandler? SettingsChanged;

        string T(string key);
        void SetDarkMode(bool isDarkMode);
        void SetAccentColor(string accentColor);
        void SetLanguage(string languageCode);
        void Save();
        void ApplyCurrentSettings();
    }

    public sealed class AppearanceSettings
    {
        public bool IsDarkMode { get; set; }
        public string AccentColor { get; set; } = "Cyan";
        public string LanguageCode { get; set; } = "vi-VN";
    }

    public sealed record AccentColorOption(string Name, string DisplayName, string PrimaryColor, string SecondaryColor);

    public sealed record LanguageOption(string Code, string DisplayName);

    public sealed class AppearanceService : IAppearanceService
    {
        private readonly string _settingsPath;

        public AppearanceService()
        {
            AccentColors = new[]
            {
                new AccentColorOption("Cyan", "Xanh ngọc", "#06B6D4", "#10B981"),
                new AccentColorOption("Blue", "Xanh dương", "#2563EB", "#06B6D4"),
                new AccentColorOption("Violet", "Tím", "#7C3AED", "#EC4899"),
                new AccentColorOption("Emerald", "Xanh lá", "#059669", "#14B8A6"),
                new AccentColorOption("Rose", "Hồng", "#E11D48", "#FB7185")
            };
            Languages = new[]
            {
                new LanguageOption("vi-VN", "Tiếng Việt"),
                new LanguageOption("en-US", "English")
            };

            var settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MoneyFlow");
            _settingsPath = Path.Combine(settingsDirectory, "appearance.json");
            Settings = Load();
        }

        public AppearanceSettings Settings { get; }
        public IReadOnlyList<AccentColorOption> AccentColors { get; }
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

        public void SetLanguage(string languageCode)
        {
            if (Languages.All(item => item.Code != languageCode) || Settings.LanguageCode == languageCode)
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
            var dark = Settings.IsDarkMode;

            var paletteHelper = new PaletteHelper();
            var materialTheme = paletteHelper.GetTheme();
            materialTheme.SetBaseTheme(dark ? BaseTheme.Dark : BaseTheme.Light);
            paletteHelper.SetTheme(materialTheme);

            SetBrush(resources, "BackgroundBrush", dark ? "#07111F" : "#EEF4FF");
            SetBrush(resources, "CardBgBrush", dark ? "#0F1B2D" : "#FDFEFF");
            SetBrush(resources, "MutedCardBrush", dark ? "#152238" : "#F8FAFC");
            SetBrush(resources, "BorderBrushSoft", dark ? "#26364C" : "#E2E8F0");
            SetBrush(resources, "InputBackgroundBrush", dark ? "#111E31" : "#F8FAFC");
            SetBrush(resources, "InputBorderBrush", dark ? "#334155" : "#CBD5E1");
            SetBrush(resources, "SidebarHoverBrush", dark ? "#14304A" : "#E0F2FE");
            SetBrush(resources, "SidebarPressedBrush", dark ? "#164E63" : "#BAE6FD");
            SetBrush(resources, "SidebarUserBrush", dark ? "#102A3A" : "#E0F2FE");
            SetBrush(resources, "SidebarUserBorderBrush", dark ? "#155E75" : "#BAE6FD");
            SetBrush(resources, "PrimaryTextBrush", dark ? "#F8FAFC" : "#0F172A");
            SetBrush(resources, "SecondaryTextBrush", dark ? "#A8B4C5" : "#64748B");
            SetBrush(resources, "LightTextBrush", dark ? "#7B8AA0" : "#94A3B8");
            SetBrush(resources, "PrimaryBrush", accent.PrimaryColor);
            SetBrush(resources, "PrimaryDarkBrush", accent.SecondaryColor);
            SetBrush(resources, "SecondaryBrush", accent.SecondaryColor);
            SetBrush(resources, "AccentBrush", accent.SecondaryColor);

            SetGradient(resources, "PrimaryGradient", accent.PrimaryColor, accent.SecondaryColor, accent.SecondaryColor);
            SetGradient(resources, "BackgroundGradient",
                dark ? "#07111F" : "#EEF4FF",
                dark ? "#0B1628" : "#E0EAFF",
                dark ? "#10172A" : "#F5F3FF");
            SetGradient(resources, "SidebarGradient",
                dark ? "#0B1729" : "#FDFEFF",
                dark ? "#0E1C30" : "#F8FAFC");
            SetGradient(resources, "CardSoftGradient",
                dark ? "#0F1B2D" : "#FFFFFF",
                dark ? "#122139" : "#F8FAFC");

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
                ["AppSubtitleText"] = "Quản lý tài chính",
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
                ["ProfileChooseAvatarText"] = "Chọn ảnh",
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
                ["ProfileAccentColorLabel"] = "Màu chủ đạo",
                ["ProfileLanguageLabel"] = "Ngôn ngữ",
                ["ProfileSaveAppearanceText"] = "Lưu tùy chỉnh",
                ["ProfileLanguageHintText"] = "Ngôn ngữ được áp dụng ngay và sẽ được lưu cho lần mở ứng dụng tiếp theo.",
                ["ProfileMemberSinceFormat"] = "Thành viên từ {0:dd/MM/yyyy}",
                ["ProfileSavedAppearanceMessage"] = "Đã lưu tùy chỉnh giao diện và ngôn ngữ."
            },
            ["en-US"] = new Dictionary<string, string>
            {
                ["AppSubtitleText"] = "Personal finance",
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
                ["ProfileChooseAvatarText"] = "Choose photo",
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
                ["ProfileAccentColorLabel"] = "Accent color",
                ["ProfileLanguageLabel"] = "Language",
                ["ProfileSaveAppearanceText"] = "Save preferences",
                ["ProfileLanguageHintText"] = "Language is applied immediately and saved for the next time you open the app.",
                ["ProfileMemberSinceFormat"] = "Member since {0:MM/dd/yyyy}",
                ["ProfileSavedAppearanceMessage"] = "Appearance and language preferences saved."
            }
        };
    }
}
