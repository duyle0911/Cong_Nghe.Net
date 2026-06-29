using System.Windows.Media;
using QuanLyTaiChinhCaNhan_Nhom06.Models;

namespace QuanLyTaiChinhCaNhan_Nhom06.ViewModels
{
    public sealed record TransactionTypeOption(TransactionType Value, string DisplayName);

    public sealed record AccentColorDisplayOption(string Name, string DisplayName, string PrimaryColor, string SecondaryColor);

    public sealed class ColorPaletteOption
    {
        public ColorPaletteOption(string value, string displayName, Brush previewBrush, bool isGradient = false)
        {
            Value = value;
            DisplayName = displayName;
            PreviewBrush = previewBrush;
            IsGradient = isGradient;
        }

        public string Value { get; }
        public string DisplayName { get; }
        public Brush PreviewBrush { get; }
        public bool IsGradient { get; }
    }

    public static class ColorPalette
    {
        public static IReadOnlyList<ColorPaletteOption> Options { get; } = new[]
        {
            Solid("#000000", "Black"),
            Solid("#4B5563", "Gray 700"),
            Solid("#6B7280", "Gray 500"),
            Solid("#A3A3A3", "Gray 400"),
            Solid("#D1D5DB", "Gray 300"),
            Solid("#FFFFFF", "White"),
            Solid("#EF4444", "Red"),
            Solid("#F97316", "Orange"),
            Solid("#F59E0B", "Amber"),
            Solid("#EAB308", "Yellow"),
            Solid("#84CC16", "Lime"),
            Solid("#22C55E", "Green"),
            Solid("#10B981", "Emerald"),
            Solid("#14B8A6", "Teal"),
            Solid("#06B6D4", "Cyan"),
            Solid("#0EA5E9", "Sky"),
            Solid("#2563EB", "Blue"),
            Solid("#004AAD", "Cobalt"),
            Solid("#4F46E5", "Indigo"),
            Solid("#7C3AED", "Violet"),
            Solid("#A855F7", "Purple"),
            Solid("#D946EF", "Fuchsia"),
            Solid("#EC4899", "Pink"),
            Solid("#FF3377", "Rose"),
            Gradient("#111827", "Black gradient", "#000000", "#4B5563"),
            Gradient("#6B7280", "Silver gradient", "#F9FAFB", "#6B7280"),
            Gradient("#84CC16", "Lime gradient", "#A3E635", "#84CC16"),
            Gradient("#F59E0B", "Gold gradient", "#78350F", "#FBBF24"),
            Gradient("#7C3AED", "Purple gold gradient", "#7C3AED", "#FBBF24"),
            Gradient("#1D4ED8", "Deep blue gradient", "#0F172A", "#1D4ED8"),
            Gradient("#0EA5E9", "Soft blue gradient", "#CFFAFE", "#0EA5E9"),
            Gradient("#EF4444", "Red orange gradient", "#EF4444", "#FB923C"),
            Gradient("#EC4899", "Pink purple gradient", "#FB7185", "#7C3AED"),
            Gradient("#06B6D4", "Blue cyan gradient", "#2563EB", "#06B6D4"),
            Gradient("#10B981", "Green teal gradient", "#22C55E", "#14B8A6"),
            Gradient("#FB7185", "Sunset gradient", "#FDE68A", "#FB7185"),
            Gradient("#C084FC", "Pastel gradient", "#FBCFE8", "#C084FC"),
            Gradient("#64748B", "Slate gradient", "#64748B", "#0F172A")
        };

        public static string ToHex(double red, double green, double blue)
        {
            return $"#{ToByte(red):X2}{ToByte(green):X2}{ToByte(blue):X2}";
        }

        public static bool TryParseColor(string? value, out Color color)
        {
            color = Colors.Transparent;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var normalized = value.Trim();
            if (!normalized.StartsWith("#", StringComparison.Ordinal))
                normalized = $"#{normalized}";

            if (normalized.Length != 7)
                return false;

            try
            {
                color = (Color)ColorConverter.ConvertFromString(normalized);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string Normalize(string? value, string fallback)
        {
            return TryParseColor(value, out var color)
                ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
                : fallback;
        }

        public static Brush CreateBrush(string? value, string fallback)
        {
            var normalized = Normalize(value, fallback);
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(normalized));
        }

        public static bool IsPaletteColor(string? value)
        {
            var normalized = Normalize(value, string.Empty);
            return Options.Any(item => string.Equals(item.Value, normalized, StringComparison.OrdinalIgnoreCase));
        }

        private static byte ToByte(double value)
        {
            return (byte)Math.Clamp((int)Math.Round(value), 0, 255);
        }
        private static ColorPaletteOption Solid(string color, string displayName)
        {
            return new ColorPaletteOption(color, displayName, new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)));
        }

        private static ColorPaletteOption Gradient(string value, string displayName, params string[] colors)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(1, 1)
            };

            for (var index = 0; index < colors.Length; index++)
            {
                brush.GradientStops.Add(new GradientStop(
                    (Color)ColorConverter.ConvertFromString(colors[index]),
                    colors.Length == 1 ? 0 : (double)index / (colors.Length - 1)));
            }

            return new ColorPaletteOption(value, displayName, brush, true);
        }
    }
}