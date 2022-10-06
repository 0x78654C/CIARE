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
                Form1.Instance.textEditorControl1.ShowLineNumbers = GlobalVariables.OLineNumber;
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
            Form1.Instance.textEditorControl1.ShowLineNumbers = status.Checked;
            GlobalVariables.OLineNumber = status.Checked;
        }
    }
}
