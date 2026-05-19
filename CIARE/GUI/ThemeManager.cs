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

        private static readonly List<string> _externalThemeNames = new List<string>();
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
        public static void LoadExternalThemes()
        {
            if (_loaded) return;
            _loaded = true;

            _externalThemeNames.Clear();
            _externalDarkThemes.Clear();

            if (!Directory.Exists(ThemesFolder))
                return;

            try
            {
                var provider = new FileSyntaxModeProvider(ThemesFolder);
                HighlightingManager.Manager.AddSyntaxModeFileProvider(provider);

                foreach (var mode in provider.SyntaxModes)
                {
                    _externalThemeNames.Add(mode.Name);
                    string xshdPath = Path.Combine(ThemesFolder, mode.FileName);
                    if (IsXshdDarkTheme(xshdPath, out var bgColor))
                        _externalDarkThemes.Add(mode.Name);
                    if (bgColor.HasValue)
                        _externalThemeBgColors[mode.Name] = bgColor.Value;
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
            switch (themeName)
            {
                case "C#-Dark":
                    return Make(
                        bg:       Color.FromArgb(2,   0,   10),
                        altBg:    Color.FromArgb(13,  11,  24),
                        fg:       Color.White,
                        selStart: Color.FromArgb(26,  58,  110),
                        selEnd:   Color.FromArgb(21,  45,  85),
                        selFg:    Color.White,
                        accent:   Color.FromArgb(79,  195, 247),
                        border:   Color.FromArgb(45,  43,  66));

                case "C#-DarkVS":
                    return Make(
                        bg:       Color.FromArgb(30,  30,  30),
                        altBg:    Color.FromArgb(37,  37,  38),
                        fg:       Color.FromArgb(212, 208, 200),
                        selStart: Color.FromArgb(38,  79,  120),
                        selEnd:   Color.FromArgb(27,  59,  92),
                        selFg:    Color.White,
                        accent:   Color.FromArgb(86,  156, 214),
                        border:   Color.FromArgb(63,  63,  70));

                case "C#-Gruvbox":
                    return Make(
                        bg:       Color.FromArgb(40,  40,  40),
                        altBg:    Color.FromArgb(50,  48,  47),
                        fg:       Color.FromArgb(235, 219, 178),
                        selStart: Color.FromArgb(80,  73,  69),
                        selEnd:   Color.FromArgb(60,  56,  54),
                        selFg:    Color.FromArgb(251, 241, 199),
                        accent:   Color.FromArgb(250, 189, 47),
                        border:   Color.FromArgb(80,  73,  69));

                case "C#-Lilac":
                    return Make(
                        bg:       Color.FromArgb(29,  27,  46),
                        altBg:    Color.FromArgb(37,  35,  64),
                        fg:       Color.FromArgb(228, 217, 245),
                        selStart: Color.FromArgb(61,  50,  88),
                        selEnd:   Color.FromArgb(46,  37,  74),
                        selFg:    Color.White,
                        accent:   Color.FromArgb(199, 146, 234),
                        border:   Color.FromArgb(61,  50,  88));

                case "C#-Neon":
                    return Make(
                        bg:       Color.FromArgb(13,  13,  13),
                        altBg:    Color.FromArgb(26,  26,  26),
                        fg:       Color.FromArgb(232, 232, 232),
                        selStart: Color.FromArgb(26,  26,  58),
                        selEnd:   Color.FromArgb(17,  17,  40),
                        selFg:    Color.White,
                        accent:   Color.FromArgb(0,   255, 204),
                        border:   Color.FromArgb(51,  51,  51));

                case "C#-NoctisHC":
                    return Make(
                        bg:       Color.FromArgb(0,   0,   0),
                        altBg:    Color.FromArgb(13,  13,  13),
                        fg:       Color.FromArgb(238, 238, 238),
                        selStart: Color.FromArgb(61,  61,  61),
                        selEnd:   Color.FromArgb(42,  42,  42),
                        selFg:    Color.White,
                        accent:   Color.FromArgb(126, 179, 255),
                        border:   Color.FromArgb(51,  51,  51));

                case "C#-Noegi":
                    return Make(
                        bg:       Color.FromArgb(26,  30,  36),
                        altBg:    Color.FromArgb(34,  40,  49),
                        fg:       Color.FromArgb(198, 208, 218),
                        selStart: Color.FromArgb(46,  58,  68),
                        selEnd:   Color.FromArgb(37,  48,  56),
                        selFg:    Color.FromArgb(198, 208, 218),
                        accent:   Color.FromArgb(128, 179, 210),
                        border:   Color.FromArgb(46,  58,  68));

                case "C#-NordWave":
                    return Make(
                        bg:       Color.FromArgb(46,  52,  64),
                        altBg:    Color.FromArgb(59,  66,  82),
                        fg:       Color.FromArgb(216, 222, 233),
                        selStart: Color.FromArgb(76,  86,  106),
                        selEnd:   Color.FromArgb(67,  76,  94),
                        selFg:    Color.FromArgb(236, 239, 244),
                        accent:   Color.FromArgb(136, 192, 208),
                        border:   Color.FromArgb(76,  86,  106));

                case "C#-Sweet":
                    return Make(
                        bg:       Color.FromArgb(26,  26,  46),
                        altBg:    Color.FromArgb(34,  34,  58),
                        fg:       Color.FromArgb(238, 255, 255),
                        selStart: Color.FromArgb(45,  43,  85),
                        selEnd:   Color.FromArgb(35,  33,  69),
                        selFg:    Color.FromArgb(238, 255, 255),
                        accent:   Color.FromArgb(189, 147, 249),
                        border:   Color.FromArgb(74,  72,  112));

                case "C#-8bit":
                    return Make(
                        bg:       Color.FromArgb(10,  10,  10),
                        altBg:    Color.FromArgb(26,  26,  26),
                        fg:       Color.FromArgb(192, 192, 192),
                        selStart: Color.FromArgb(0,   0,   170),
                        selEnd:   Color.FromArgb(0,   0,   136),
                        selFg:    Color.White,
                        accent:   Color.FromArgb(0,   170, 170),
                        border:   Color.FromArgb(85,  85,  85));

                default:
                    // Unknown / light theme
                    return CompletionThemeColors.Light;
            }
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

        private static bool IsXshdDarkTheme(string xshdPath, out Color? bgColor)
        {
            bgColor = null;
            try
            {
                using var reader = new XmlTextReader(File.OpenRead(xshdPath));
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "Default")
                    {
                        string bgcolor = reader.GetAttribute("bgcolor");
                        if (!string.IsNullOrEmpty(bgcolor))
                        {
                            var color = ColorTranslator.FromHtml(bgcolor);
                            bgColor = color;
                            return color.GetBrightness() < 0.5f;
                        }
                    }
                }
            }
            catch { }
            return false;
        }
    }
}
