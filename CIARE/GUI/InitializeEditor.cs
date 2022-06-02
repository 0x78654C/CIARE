using CIARE.Utils;
using ICSharpCode.TextEditor;
using System.Drawing;
using System.Windows.Forms;

namespace CIARE.GUI
{
    public class InitializeEditor
    {
        /// <summary>
        /// Read and apply highlight setting from registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        /// <param name="textEditor"></param>
        /// <param name="comboBox"></param>
        public static void ReadEditorHighlight(string regKeyName, TextEditorControl textEditor, ComboBox comboBox)
        {
            string regHighlight = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", "highlight");
            if (regHighlight.Length > 0)
            {
                if (regHighlight == "C#-Dark")
                    GlobalVariables.darkColor = true;
                textEditor.SetHighlighting(regHighlight);
                comboBox.Text = regHighlight;
                return;
            }
            RegistryManagement.RegKey_CreateKey(regKeyName, "highlight", "Default");
        }

        /// <summary>
        /// Read and apply last editor zoom size.
        /// </summary>
        /// <param name="regKeyName"></param>
        /// <param name="regSubKey"></param>
        /// <param name="textEditor"></param>
        public static void ReadEditorFontSize(string regKeyName, string regSubKey, TextEditorControl textEditor)
        {
            string sizeFont = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", regSubKey);
            if (sizeFont.Length > 0)
            {
                textEditor.Font = new Font("Consolas", float.Parse(sizeFont), FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                return;
            }
            RegistryManagement.RegKey_CreateKey(GlobalVariables.registryPath, regSubKey, "9.75");
        }

        /// <summary>
        /// Read output window state from registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void ReadOutputWindowState(string regKeyName, SplitContainer splitContainer)
        {
            string regHighlight = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", "OutWState");
            if (regHighlight.Length > 0)
            {
                if (regHighlight == "False")
                {
                    SplitContainerHideShow.ShowSplitContainer(splitContainer);
                    Form1.Instance.visibleSplitContainer = false;
                    Form1.Instance.visibleSplitContainerAutoHide = true;
                }
                else
                {
                    SplitContainerHideShow.HideSplitContainer(splitContainer);
                    Form1.Instance.visibleSplitContainer = true;
                    Form1.Instance.visibleSplitContainerAutoHide = false;
                }
                return;
            }
            RegistryManagement.RegKey_CreateKey(regKeyName, "OutWState", "True");
        }
    }
}
