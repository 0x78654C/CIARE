using CIARE.Utils;
using ICSharpCode.TextEditor;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    public class InitializeEditor
    {
        private const string _defaultHighLight = "C#-Dark";
        private const string _regName = "highlight";
        private const string _windowSize = "windowSize";
        private const string _windowSizeMax = "windowSizeMax";


        /// <summary>
        /// Create main CIARE reg key with values.
        /// </summary>
        /// <param name="regKeyName"></param>
        /// <param name="subKeyName"></param>
        /// <param name="subKeyValue"></param>
        public static void SetCiareRegKey(string regKeyName, string subKeyName, string subKeyValue)
        {
            bool ciareRegKey = RegistryManagement.RegKey_Check($"HKEY_CURRENT_USER\\{regKeyName}", subKeyName);
            if (!ciareRegKey)
                RegistryManagement.RegKey_CreateKey(regKeyName, subKeyName, subKeyValue);
        }

        /// <summary>
        /// Read and apply highlight setting from registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        /// <param name="textEditor"></param>
        /// <param name="comboBox"></param>
        public static void ReadEditorHighlight(string regKeyName, TextEditorControl textEditor, ComboBox comboBox)
        {
            string regHighlight = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", _regName);
            if (regHighlight.Length > 0)
            {
                if (regHighlight == _defaultHighLight)
                    GlobalVariables.darkColor = true;
                textEditor.SetHighlighting(regHighlight);
                comboBox.Text = regHighlight;
                return;
            }
            RegistryManagement.RegKey_CreateKey(regKeyName, _regName, _defaultHighLight);
            GlobalVariables.darkColor = true;
        }


        /// <summary>
        /// Read and apply/create Windows size in registry.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="regKeyName"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void ReadEditorWindowSize(Form form, string regKeyName)
        {
            string regWindowSize = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", _windowSize);
            string regWindowSizemax = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", _windowSizeMax);
            if (regWindowSizemax.Length == 0)
                RegistryManagement.RegKey_CreateKey(regKeyName, _windowSize, "False");
            if (regWindowSize.Length > 0)
            {
                string[] splitValue = regWindowSize.Split('|');
                int regWidth = Int32.Parse(splitValue[0]);
                int regHeight = Int32.Parse(splitValue[1]);
                if (regWindowSizemax == "True")
                {
                    form.WindowState = FormWindowState.Maximized;
                }
                else
                {
                    form.Width = regWidth;
                    form.Height = regHeight;
                }
                return;
            }

            //Predefined value on first run.
            RegistryManagement.RegKey_CreateKey(regKeyName, _windowSize, "1225|786");
        }

        /// <summary>
        /// Store Window size in regsitry.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void SetEditorWindowSize(string regKeyName, int width, int height)
        {
            RegistryManagement.RegKey_WriteSubkey(regKeyName, _windowSize, $"{width}|{height}");
        }

        /// <summary>
        /// Set flag for window maximized state in registry used for load on application open.
        /// </summary>
        /// <param name="regKeyName"></param>
        /// <param name="flagMaximized"></param>
        public static void SetMaximizedWindowState(string regKeyName, bool flagMaximized)
        {
            RegistryManagement.RegKey_WriteSubkey(regKeyName, _windowSizeMax, flagMaximized.ToString());
        }
        /// <summary>
        /// Read and apply highlight setting from registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        /// <param name="textEditor"></param>
        /// <param name="comboBox"></param>
        public static void ReadEditorHighlight(string regKeyName, TextEditorControl textEditor)
        {
            string regHighlight = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", _regName);
            if (regHighlight.Length > 0)
            {
                if (regHighlight == _defaultHighLight)
                {
                    GlobalVariables.darkColor = true;
                    MainForm.Instance.SetHighLighter(MainForm.Instance.selectedEditor, regHighlight);
                }
                textEditor.SetHighlighting(regHighlight);
                return;
            }
            RegistryManagement.RegKey_CreateKey(regKeyName, _regName, _defaultHighLight);
            GlobalVariables.darkColor = true;
            MainForm.Instance.SetHighLighter(MainForm.Instance.selectedEditor, _defaultHighLight);
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
                    MainForm.Instance.visibleSplitContainer = false;
                    MainForm.Instance.visibleSplitContainerAutoHide = true;
                }
                else
                {
                    SplitContainerHideShow.HideSplitContainer(splitContainer);
                    MainForm.Instance.visibleSplitContainer = true;
                    MainForm.Instance.visibleSplitContainerAutoHide = false;
                }
                return;
            }
            RegistryManagement.RegKey_CreateKey(regKeyName, "OutWState", "True");
        }

        /// <summary>
        /// Check/Create user data directory on CIARE start up.
        /// </summary>
        /// <param name="directoryPath"></param>
        public static void CreateUserDataDirectory(string directoryPath, string userAppDatafile)
        {
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            if (!File.Exists(userAppDatafile))
                File.WriteAllText(userAppDatafile, string.Empty);
        }

        /// <summary>
        /// Check WinLogin State in registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        /// <param name="regSubKey"></param>
        /// <returns></returns>
        public static void WinLoginState(string regKeyName, string regSubKey, out bool winLoginGlobal)
        {
            string regLoginState = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", regSubKey);
            winLoginGlobal = regLoginState.Length > 0 ? bool.Parse(regLoginState) : false;
        }

        /// <summary>
        /// Generate Session id on CIARE load.
        /// </summary>
        public static void GenerateLiveSessionId()
        {
            GlobalVariables.sessionId = Utils.Encryption.KeyGenerator.GeneratePassword(20, false, false, false, true);
            GlobalVariables.sessionIdMain = GlobalVariables.sessionId;
        }

        /// <summary>
        /// Delete NuGet directory.
        /// </summary>
        /// <param name="nugetFolder"></param>
        public static void CleanNugetFolder(string nugetFolder)
        {
            try
            {
                if (Directory.Exists(nugetFolder))
                    Directory.Delete(nugetFolder, true);
                Directory.CreateDirectory(nugetFolder);
            }
            catch (Exception)
            { // Ignore
            }
        }
    }
}
