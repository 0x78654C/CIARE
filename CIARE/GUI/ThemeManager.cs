using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;
using System.Xml;
using ICSharpCode.TextEditor.Document;

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
        /// Reads the .xshd file and checks the Default element's bgcolor attribute to determine if it's a dark theme.
        /// Also returns the parsed bgcolor via <paramref name="bgColor"/>.
        /// </summary>
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
