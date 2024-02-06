using ICSharpCode.TextEditor;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Utils.Options
{
    [SupportedOSPlatform("windows")]
    public class LineNumber
    {
        /// <summary>
        /// Check stored status for line number display in registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void CheckLineNumberStatus(string regKeyName)
        {
            string regHighlight = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.lineNumberKey);
            if (regHighlight.Length > 0)
            {
                GlobalVariables.OLineNumber = bool.Parse(regHighlight);
                SetLineNumbersTabEditors();
            }
        }

        /// <summary>
        /// Store in registry line number display status.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void SetLineNumberStatus(CheckBox status, string regKeyName)
        {
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, status.Checked.ToString());
            SetLineNumbersTabEditors(status,true);
            GlobalVariables.OLineNumber = status.Checked;
        }

        /// <summary>
        /// Set line numbers function for editor controler in all tabs.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="isCheckBox"></param>
        private static void SetLineNumbersTabEditors(CheckBox status = null, bool isCheckBox = false)
        {
            int count = 0;
            foreach (TabPage tab in MainForm.Instance.EditorTabControl.TabPages)
            {
                if (count > 0)
                {
                    Control ctrl = MainForm.Instance.EditorTabControl.Controls[count].Controls[0];
                    var textEditor = ctrl as TextEditorControl;
                    if (isCheckBox)
                        textEditor.ShowLineNumbers = status.Checked;
                    else
                        textEditor.ShowLineNumbers = GlobalVariables.OLineNumber;
                }
                count++;
            }
        }
    }
}
