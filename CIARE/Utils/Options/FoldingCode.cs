using ICSharpCode.TextEditor;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Utils.Options
{
    [SupportedOSPlatform("windows")]
    public class FoldingCode
    {
        /// <summary>
        /// Check stored status for folding code display in registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void CheckFoldingCodeStatus(string regKeyName)
        {
            string regHighlight = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.foldingCodeKey);
            if (regHighlight.Length > 0)
            {
                GlobalVariables.OFoldingCode = bool.Parse(regHighlight);
                SetFoldingTabEditors();
            }
        }

        /// <summary>
        /// Store in registry folding code display status.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void SetFoldingCodeStatus(CheckBox status, string regKeyName)
        {
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, status.Checked.ToString());
            SetFoldingTabEditors(status, true);
            GlobalVariables.OFoldingCode = status.Checked;
        }

        /// <summary>
        /// Set folding function for editor controler in all tabs.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="isCheckBox"></param>
        private static void SetFoldingTabEditors(CheckBox status = null, bool isCheckBox = false)
        {
            int count = 0;
            foreach (TabPage tab in MainForm.Instance.EditorTabControl.TabPages)
            {
                if (count > 0)
                {
                    Control ctrl = MainForm.Instance.EditorTabControl.Controls[count].Controls[0];
                    var textEditor = ctrl as TextEditorControl;
                    if (isCheckBox)
                        textEditor.EnableFolding = status.Checked;
                    else
                        textEditor.EnableFolding = GlobalVariables.OFoldingCode;
                }
                count++;
            }
        }
    }
}
