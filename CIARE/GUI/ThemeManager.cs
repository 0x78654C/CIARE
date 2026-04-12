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

        private static readonly List<string> _externalThemeNames = new List<string>();
        private static bool _loaded = false;

        public static IReadOnlyList<string> ExternalThemeNames => _externalThemeNames.AsReadOnly();

        public static bool IsExternalDarkTheme(string name) => _externalDarkThemes.Contains(name);

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
                    if (IsXshdDarkTheme(xshdPath))
                        _externalDarkThemes.Add(mode.Name);
                }

                _externalThemeNames.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch { }
        }

        /// <summary>
        /// Reads the .xshd file and checks the Default element's bgcolor attribute to determine if it's a dark theme.
        /// </summary>
        /// <param name="xshdPath"></param>
        /// <returns></returns>
        private static bool IsXshdDarkTheme(string xshdPath)
        {
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
