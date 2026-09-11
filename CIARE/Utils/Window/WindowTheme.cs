using System;
using System.Drawing;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using ICSharpCode.TextEditor;

namespace CIARE.Utils.Window
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class WindowTheme
    {
        private readonly MainForm _mainForm;

        internal WindowTheme(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        internal string _appliedTheme;

        public void SetHighLighter(TextEditorControl textEditorControl, string highlight, bool persistSetting = true)
        {
            highlight = highlight ?? string.Empty;
            if (highlight.Length > 0)
            {
                textEditorControl.SetHighlighting(highlight);
                if (persistSetting)
                    RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, "highlight", highlight);
            }
            // New tabs inherit the existing form palette without repainting every control.
            if (!persistSetting && string.Equals(_appliedTheme, highlight, StringComparison.Ordinal))
                return;

            _appliedTheme = highlight;
            var theme = CIARE.GUI.ThemeManager.GetCompletionThemeColors(highlight);
            ICSharpCode.TextEditor.Gui.CompletionWindow.CodeCompletionListView.ActiveTheme = theme;
            ICSharpCode.TextEditor.Gui.CompletionWindow.DeclarationViewWindow.ThemeBackColor = theme.BackColor;
            ICSharpCode.TextEditor.Gui.CompletionWindow.DeclarationViewWindow.ThemeForeColor = theme.ForeColor;

            if (highlight.StartsWith("C#-Dark") || CIARE.GUI.InitializeEditor.IsDarkTheme(highlight))
            {
                GlobalVariables.darkColor = true;
                GlobalVariables.isVStheme = highlight.EndsWith("VS");
                UpdateThemeColors(highlight);
                var darkBg = GlobalVariables.controlBgColor;
                var darkFg = Color.FromArgb(192, 215, 207);
                DarkModeMain.SetDarkModeMain(_mainForm, _mainForm.outputRBT, _mainForm.groupBox1, _mainForm.label2, _mainForm.label3,
                    _mainForm.menuStrip1, ListMenuStripItems.ListToolStripMenu(), ListMenuStripItems.ListToolStripSeparator(), GlobalVariables.isVStheme);
                _mainForm.errorsLV.BackColor = darkBg;
                _mainForm.errorsLV.ForeColor = darkFg;
                ApplyTabControlDarkMode(_mainForm.EditorTabControl, darkBg);
                ApplyTabControlDarkMode(_mainForm.outputTabControl, darkBg);
                _mainForm.ExplorerFeature.ApplyFileExplorerTheme(highlight);
                return;
            }
            GlobalVariables.darkColor = false;
            LightModeMain.SetLightModeMain(_mainForm, _mainForm.outputRBT, _mainForm.groupBox1,
                _mainForm.menuStrip1, ListMenuStripItems.ListToolStripMenu(), ListMenuStripItems.ListToolStripSeparator());
            _mainForm.errorsLV.BackColor = SystemColors.Window;
            _mainForm.errorsLV.ForeColor = Color.Black;
            ApplyTabControlDarkMode(_mainForm.EditorTabControl, SystemColors.Window);
            ApplyTabControlDarkMode(_mainForm.outputTabControl, SystemColors.Window);
            _mainForm.ExplorerFeature.ApplyFileExplorerTheme(highlight);
        }

        /// <summary>
        /// Sets <see cref="GlobalVariables.formBgColor"/> and <see cref="GlobalVariables.controlBgColor"/>
        /// to match the currently selected theme, including external .xshd themes.
        /// </summary>
        private static void UpdateThemeColors(string highlight)
        {
            if (highlight.EndsWith("VS"))
            {
                GlobalVariables.formBgColor = Color.FromArgb(51, 51, 51);
                GlobalVariables.controlBgColor = Color.FromArgb(30, 30, 30);
            }
            else if (highlight.StartsWith("C#-Dark"))
            {
                GlobalVariables.formBgColor = Color.FromArgb(0, 1, 10);
                GlobalVariables.controlBgColor = Color.FromArgb(2, 0, 10);
            }
            else
            {
                // External dark theme — derive background from the .xshd bgcolor.
                var extBg = CIARE.GUI.ThemeManager.GetExternalThemeBgColor(highlight);
                if (extBg.HasValue)
                {
                    GlobalVariables.formBgColor = extBg.Value;
                    GlobalVariables.controlBgColor = extBg.Value;
                }
                else
                {
                    GlobalVariables.formBgColor = Color.FromArgb(0, 1, 10);
                    GlobalVariables.controlBgColor = Color.FromArgb(2, 0, 10);
                }
            }
        }

        private static void ApplyTabControlDarkMode(TabControl tabControl, Color backColor)
        {
            if (tabControl == null)
                return;

            tabControl.BackColor = backColor;
            foreach (TabPage page in tabControl.TabPages)
            {
                page.UseVisualStyleBackColor = false;
                page.BackColor = backColor;
            }
            tabControl.Invalidate();
        }
    }
}
