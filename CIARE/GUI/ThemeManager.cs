using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;
using System.Xml;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    public static class ThemeManager
    {
        public static readonly string ThemesFolder = Path.Combine(Application.StartupPath, "themes");

        private static readonly HashSet<string> _externalDarkThemes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Color> _externalThemeBgColors =
            new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, CompletionThemeColors> _externalCompletionThemeColors =
            new Dictionary<string, CompletionThemeColors>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, CompletionThemeColors> _builtInCompletionThemeColors =
            new Dictionary<string, CompletionThemeColors>(StringComparer.Ordinal);

        private static readonly List<string> _externalThemeNames = new List<string>();
        private static FileSyntaxModeProvider _externalThemeProvider;
        private static bool _loaded = false;

        public static IReadOnlyList<string> ExternalThemeNames => _externalThemeNames.AsReadOnly();

        public static bool IsExternalDarkTheme(string name) => _externalDarkThemes.Contains(name);

        /// <summary>
        /// Returns the background color read from an external theme's .xshd file, or null if unavailable.
        /// </summary>
        public static Color? GetExternalThemeBgColor(string name) =>
            _externalThemeBgColors.TryGetValue(name, out var c) ? c : (Color?)null;

        /// <summary>
        /// Scans the themes folder, registers all .xshd files with the HighlightingManager
        /// and detects which ones are dark themes by reading the Default bgcolor element.
        /// </summary>
        public static void LoadExternalThemes(bool forceRefresh = false)
        {
            if (_loaded && !forceRefresh) return;
            _loaded = true;

            _externalThemeNames.Clear();
            _externalDarkThemes.Clear();
            _externalThemeBgColors.Clear();
            _externalCompletionThemeColors.Clear();

            if (!Directory.Exists(ThemesFolder))
                return;

            try
            {
                if (_externalThemeProvider == null)
                    _externalThemeProvider = new FileSyntaxModeProvider(ThemesFolder);
                else
                    _externalThemeProvider.UpdateSyntaxModeList();

                HighlightingManager.Manager.AddSyntaxModeFileProvider(_externalThemeProvider);

                foreach (var mode in _externalThemeProvider.SyntaxModes)
                {
                    _externalThemeNames.Add(mode.Name);
                    string xshdPath = Path.Combine(ThemesFolder, mode.FileName);
                    if (TryReadXshdTheme(xshdPath, out var themeInfo) && themeInfo.DefaultBackColor.HasValue)
                    {
                        var bgColor = themeInfo.DefaultBackColor.Value;
                        _externalThemeBgColors[mode.Name] = bgColor;
                        _externalCompletionThemeColors[mode.Name] = BuildCompletionThemeColors(themeInfo);

                        if (bgColor.GetBrightness() < 0.5f)
                            _externalDarkThemes.Add(mode.Name);
                    }
                }

                _externalThemeNames.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch { }
        }

        /// <summary>
        /// Returns a <see cref="CompletionThemeColors"/> palette that matches the named editor theme.
        /// Falls back to the generic light palette for any unknown theme name.
        /// </summary>
        public static CompletionThemeColors GetCompletionThemeColors(string themeName)
        {
            LoadExternalThemes();

            if (!string.IsNullOrEmpty(themeName) &&
                _externalCompletionThemeColors.TryGetValue(themeName, out var externalTheme))
            {
                return externalTheme;
            }

            if (string.IsNullOrEmpty(themeName))
                return CompletionThemeColors.Light;

            if (_builtInCompletionThemeColors.TryGetValue(themeName, out var builtInTheme))
                return builtInTheme;

            string resourceName = themeName switch
            {
                "C#-Light" => "CSharp-Mode.xshd",
                "C#-Dark" => "CSharp-Mode-Dark.xshd",
                "C#-DarkVS" => "CSharp-Mode-DarkVS.xshd",
                _ => null
            };
            if (resourceName != null)
            {
                using var stream = typeof(HighlightingManager).Assembly.GetManifestResourceStream(
                    "ICSharpCode.TextEditor.Resources." + resourceName);
                if (stream != null)
                {
                    using var reader = XmlReader.Create(stream);
                    if (TryReadXshdTheme(reader, out var themeInfo))
                    {
                        builtInTheme = BuildCompletionThemeColors(themeInfo);
                        _builtInCompletionThemeColors[themeName] = builtInTheme;
                        return builtInTheme;
                    }
                }
            }
            return CompletionThemeColors.Light;
        }

        private static CompletionThemeColors Make(
            Color bg, Color altBg, Color fg,
            Color selStart, Color selEnd, Color selFg,
            Color accent, Color border)
        {
            return new CompletionThemeColors
            {
                BackColor           = bg,
                RowAlternateColor   = altBg,
                ForeColor           = fg,
                SelectionStartColor = selStart,
                SelectionEndColor   = selEnd,
                SelectionForeColor  = selFg,
                AccentColor         = accent,
                BorderColor         = border,
            };
        }

        private static bool TryReadXshdTheme(string xshdPath, out XshdThemeInfo themeInfo)
        {
            themeInfo = new XshdThemeInfo();
            try
            {
                using var reader = XmlReader.Create(xshdPath);
                return TryReadXshdTheme(reader, out themeInfo);
            }
            catch { return false; }
        }

        private static bool TryReadXshdTheme(XmlReader reader, out XshdThemeInfo themeInfo)
        {
            themeInfo = new XshdThemeInfo();
            try
            {
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element)
                        continue;

                    var color = ReadColorAttribute(reader, "color");
                    var bgColor = ReadColorAttribute(reader, "bgcolor");
                    if (color.HasValue)
                        themeInfo.AccentCandidates.Add(color.Value);

                    switch (reader.Name)
                    {
                        case "Custom":
                            if (reader.GetAttribute("name") == "Accent")
                                themeInfo.AccentColor = color;
                            break;
                        case "Default":
                            themeInfo.DefaultForeColor = color;
                            themeInfo.DefaultBackColor = bgColor;
                            break;
                        case "Selection":
                            themeInfo.SelectionForeColor = color;
                            themeInfo.SelectionBackColor = bgColor;
                            break;
                        case "LineNumbers":
                            themeInfo.LineNumberForeColor = color;
                            break;
                        case "InvalidLines":
                        case "CaretMarker":
                        case "FoldLine":
                            if (!themeInfo.BorderColor.HasValue)
                                themeInfo.BorderColor = color ?? bgColor;
                            break;
                    }
                }

                return themeInfo.DefaultBackColor.HasValue;
            }
            catch { }
            return false;
        }

        private static CompletionThemeColors BuildCompletionThemeColors(XshdThemeInfo themeInfo)
        {
            var bg = themeInfo.DefaultBackColor.Value;
            bool dark = bg.GetBrightness() < 0.5f;

            var fg = GetReadableTextColor(themeInfo.DefaultForeColor, bg);
            var altBg = dark ? Blend(bg, Color.White, 0.08) : Blend(bg, Color.Black, 0.04);
            var selectionStart = themeInfo.SelectionBackColor ?? (dark ? Blend(bg, Color.White, 0.22) : Blend(bg, Color.Black, 0.12));
            var selectionEnd = Blend(selectionStart, Color.Black, dark ? 0.12 : 0.08);
            var selectionFg = GetReadableTextColor(themeInfo.SelectionForeColor ?? themeInfo.DefaultForeColor, selectionStart);
            var accent = PickAccentColor(themeInfo, bg, selectionStart, selectionFg);
            var border = themeInfo.BorderColor ?? (dark ? Blend(bg, Color.White, 0.22) : Blend(bg, Color.Black, 0.18));

            if (ContrastRatio(border, bg) < 1.25)
                border = dark ? Blend(bg, Color.White, 0.22) : Blend(bg, Color.Black, 0.18);

            return Make(
                bg: bg,
                altBg: altBg,
                fg: fg,
                selStart: selectionStart,
                selEnd: selectionEnd,
                selFg: selectionFg,
                accent: accent,
                border: border);
        }

        private static Color? ReadColorAttribute(XmlReader reader, string attributeName)
        {
            string value = reader.GetAttribute(attributeName);
            if (string.IsNullOrWhiteSpace(value))
                return null;

            try
            {
                var color = ColorTranslator.FromHtml(value);
                return color.IsEmpty ? (Color?)null : color;
            }
            catch
            {
                var color = Color.FromName(value);
                return color.IsKnownColor ? color : (Color?)null;
            }
        }

        private static Color PickAccentColor(XshdThemeInfo themeInfo, Color bg, Color selectionBg, Color fallback)
        {
            if (themeInfo.AccentColor.HasValue &&
                ContrastRatio(themeInfo.AccentColor.Value, bg) >= 3.0 &&
                ContrastRatio(themeInfo.AccentColor.Value, selectionBg) >= 1.6)
            {
                return themeInfo.AccentColor.Value;
            }

            Color best = Color.Empty;
            double bestScore = double.MinValue;
            var seen = new HashSet<int>();

            foreach (var candidate in EnumerateAccentCandidates(themeInfo))
            {
                if (!seen.Add(candidate.ToArgb()))
                    continue;

                double backgroundContrast = ContrastRatio(candidate, bg);
                double selectionContrast = ContrastRatio(candidate, selectionBg);
                if (backgroundContrast < 1.35 || selectionContrast < 1.6)
                    continue;

                double score = candidate.GetSaturation() * 3.0
                    + Math.Min(selectionContrast, 7.0)
                    + Math.Abs(candidate.GetBrightness() - bg.GetBrightness());

                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best.IsEmpty ? fallback : best;
        }

        private static IEnumerable<Color> EnumerateAccentCandidates(XshdThemeInfo themeInfo)
        {
            if (themeInfo.LineNumberForeColor.HasValue)
                yield return themeInfo.LineNumberForeColor.Value;
            if (themeInfo.SelectionForeColor.HasValue)
                yield return themeInfo.SelectionForeColor.Value;
            foreach (var color in themeInfo.AccentCandidates)
                yield return color;
        }

        private static Color GetReadableTextColor(Color? preferred, Color bg)
        {
            if (preferred.HasValue && ContrastRatio(preferred.Value, bg) >= 4.5)
                return preferred.Value;

            return ContrastRatio(Color.White, bg) >= ContrastRatio(Color.Black, bg)
                ? Color.White
                : Color.FromArgb(32, 32, 32);
        }

        private static Color Blend(Color source, Color target, double amount)
        {
            amount = Math.Max(0, Math.Min(1, amount));
            return Color.FromArgb(
                (int)Math.Round(source.R + (target.R - source.R) * amount),
                (int)Math.Round(source.G + (target.G - source.G) * amount),
                (int)Math.Round(source.B + (target.B - source.B) * amount));
        }

        private static double ContrastRatio(Color a, Color b)
        {
            double l1 = RelativeLuminance(a);
            double l2 = RelativeLuminance(b);
            double lighter = Math.Max(l1, l2);
            double darker = Math.Min(l1, l2);
            return (lighter + 0.05) / (darker + 0.05);
        }

        private static double RelativeLuminance(Color color)
        {
            double r = Linearize(color.R / 255.0);
            double g = Linearize(color.G / 255.0);
            double b = Linearize(color.B / 255.0);
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private static double Linearize(double channel)
        {
            return channel <= 0.03928
                ? channel / 12.92
                : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }

        private sealed class XshdThemeInfo
        {
            public Color? AccentColor { get; set; }
            public Color? DefaultBackColor { get; set; }
            public Color? DefaultForeColor { get; set; }
            public Color? SelectionBackColor { get; set; }
            public Color? SelectionForeColor { get; set; }
            public Color? LineNumberForeColor { get; set; }
            public Color? BorderColor { get; set; }
            public List<Color> AccentCandidates { get; } = new List<Color>();
        }
    }
}
