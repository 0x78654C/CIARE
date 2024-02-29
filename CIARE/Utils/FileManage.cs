using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Reference;
using CIARE.Utils.FilesOpenOS;
using ICSharpCode.TextEditor;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.Forms.MessageBox;
using Path = System.IO.Path;

namespace CIARE.Utils
{
    [SupportedOSPlatform("windows")]
    public class FileManage
    {
        private static OpenFileDialog s_openFileDialog = new OpenFileDialog();
        private static SaveFileDialog s_saveFileDialog = new SaveFileDialog();
        private static List<string> s_packageLibs = new List<string>();
        /// <summary>
        /// Open file dialog.
        /// </summary>
        /// <returns></returns>
        public static string OpenFile()
        {
            s_openFileDialog.Filter = "All Files (*.*)|*.*|C# Files (*.cs)|*.cs|Text Files (*.txt)|*.txt";
            s_openFileDialog.Title = "Select file top open:";
            s_openFileDialog.CheckFileExists = true;
            s_openFileDialog.CheckPathExists = true;
            DialogResult dr = s_openFileDialog.ShowDialog();
            if (dr == DialogResult.OK)
            {
                using (StreamReader reader = new StreamReader(s_openFileDialog.FileName))
                {
                    GlobalVariables.openedFilePath = s_openFileDialog.FileName;
                    var fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                    GlobalVariables.openedFileName = fileInfo.Name;
                    return reader.ReadToEnd();
                }
            }
            else
            {
                GlobalVariables.noFileSelected = true;
            }
            return "";
        }


        /// <summary>
        /// Open file dialog.
        /// </summary>
        /// <returns></returns>
        public static void AddReferenceDialog()
        {
            s_openFileDialog.Filter = "Dynamic Linked Library (*.dll)|*.dll|All Files (*.*)|*.*";
            s_openFileDialog.Title = "Select file top open:";
            s_openFileDialog.DefaultExt = "dll";
            s_openFileDialog.CheckFileExists = true;
            s_openFileDialog.CheckPathExists = true;
            s_openFileDialog.Multiselect = true;
            DialogResult dr = s_openFileDialog.ShowDialog();
            if (dr == DialogResult.OK)
            {
                foreach (var lib in s_openFileDialog.FileNames)
                {
                    if (CustomRef.IsManaged(lib))
                    {
                        if (!GlobalVariables.customRefAsm.Contains(lib))
                            GlobalVariables.customRefAsm.Add(lib);
                    }
                    else
                        MessageBox.Show($"{lib} is not managed library!", "CIARE", MessageBoxButtons.OK,
MessageBoxIcon.Warning);
                }
            }
        }


        /// <summary>
        /// Save file dialog.
        /// </summary>
        /// <param name="data"></param>
        public static void SaveFile(string data)
        {
            s_saveFileDialog.Filter = "C# Files (*.cs)|*.cs|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            s_saveFileDialog.Title = $"Save As... :";
            DialogResult dr = s_saveFileDialog.ShowDialog();
            if (dr == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(s_saveFileDialog.FileName, data);
                    if (File.Exists(s_saveFileDialog.FileName))
                    {
                        GlobalVariables.openedFilePath = s_saveFileDialog.FileName;
                        FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                        GlobalVariables.openedFileName = fileInfo.Name;
                        GlobalVariables.savedFile = true;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Check if param file contains path.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string PathCheck(string data)
        {
            if (data.Contains(":\\"))
            {
                if (!File.Exists(data))
                    return ManageCommandFileParam(data);
                return data;
            }
            else
                return ManageCommandFileParam(data);
        }

        /// <summary>
        /// Open file with no path event.
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string ManageCommandFileParam(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            fileName = GetCiarePath(fileName);
            if (!File.Exists(fileName))
            {
                DialogResult dr = MessageBox.Show($"File '{fileName}' does not exist.\nDo you want to create it?", "CIARE", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (dr == DialogResult.Cancel)
                    Environment.Exit(1);
                if (dr == DialogResult.Yes)
                {
                    GlobalVariables.openedFilePath = fileName;
                    var fileInfo1 = new FileInfo(GlobalVariables.openedFilePath);
                    GlobalVariables.openedFileName = fileInfo1.Name;
                    GlobalVariables.noPath = true;
                    GlobalVariables.savedFile = true;
                    File.WriteAllText(fileName, "");
                }
                if (dr == DialogResult.No)
                    GlobalVariables.noPath = true;
                return fileName;
            }
            GlobalVariables.openedFilePath = fileName;
            var fileInfo = new FileInfo(GlobalVariables.openedFilePath);
            GlobalVariables.openedFileName = fileInfo.Name;
            return fileName;
        }

        public static bool ManageCommandFileParam(string fileName, bool isFromForm)
        {
            fileName = fileName.Split('|')[1];
            if (string.IsNullOrEmpty(fileName))
                return false;

            fileName = GetCiarePath(fileName);
            if (!File.Exists(fileName))
            {
                DialogResult dr;
                if(isFromForm)
                    dr = MessageBox.Show($"File '{fileName}' does not exist.\nDo you want to create it?", "CIARE", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                else
                    dr = MessageBox.Show($"File '{fileName}' does not exist.\nDo you want to create it?", "CIARE", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                if (dr == DialogResult.Cancel)
                    Environment.Exit(1);
                if (dr == DialogResult.Yes)
                    File.WriteAllText(fileName, "");
                if (dr == DialogResult.No)
                    GlobalVariables.noPath = true;
                return true;
            }
            GlobalVariables.openedFilePath = fileName;
            var fileInfo = new FileInfo(GlobalVariables.openedFilePath);
            GlobalVariables.openedFileName = fileInfo.Name;
            return false;
        }



        /// <summary>
        /// Sanitize fileName path with CIARE applicatin
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string GetCiarePath(string fileName)
        {
            return fileName.Contains(@":\") ? fileName : Path.Combine(Application.StartupPath, fileName);
        }

        /// <summary>
        /// Get path from file name.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFilePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "";

            if (!File.Exists(fileName))
                return "";

            var fileInfo = new FileInfo(fileName);
            return fileInfo.DirectoryName;
        }

        /// <summary>
        /// Check if editor on selected tab is empty.
        /// </summary>
        /// <param name="selectedIndex"></param>
        /// <returns></returns>
        private static bool IsEditorEmpty(int selectedIndex)
        {
            if (selectedIndex == 0)
                return false;
            Control ctrl = MainForm.Instance.EditorTabControl.Controls[selectedIndex].Controls[0];
            var textEditor = ctrl as TextEditorControl;
            return string.IsNullOrEmpty(textEditor.Text);
        }

        /// <summary>
        /// Handle unsaved data from editor on from closing event.
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="selectedIndex"></param>
        /// <param name="checkAll"></param>
        public static void ManageUnsavedData(TextEditorControl textEditorControl, int selectedIndex = 0, bool checkAll = false)
        {
            DialogResult dr = DialogResult.No;
            int countTabs = 0;
            foreach (TabPage tab in MainForm.Instance.EditorTabControl.TabPages)
            {
                countTabs++;
                bool isSelectedTab = countTabs - 1 == selectedIndex;

                // Check if editor is empty.
                if (IsEditorEmpty(countTabs - 1)) continue;

                if (checkAll)
                {
                    if (tab.Text.StartsWith("*") || tab.Text.Contains("New Page"))
                    {
                        if (!string.IsNullOrEmpty(textEditorControl.Text))
                            dr = MessageBox.Show($"There is unsaved data in {tab.Text.Trim().Replace("*","")}. Do you want to save it?", "CIARE", MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);
                        MainForm.Instance.EditorTabControl.SelectTab(tab);
                        DialogResultAction(dr, textEditorControl);
                    }
                }
                else
                {
                    if ((tab.Text.StartsWith("*") || tab.Text.Contains("New Page")) && isSelectedTab)
                    {
                        if (!string.IsNullOrEmpty(textEditorControl.Text))
                            dr = MessageBox.Show($"There is unsaved data in {tab.Text.Trim().Replace("*", "")}. Do you want to save it?", "CIARE", MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);
                        DialogResultAction(dr, textEditorControl);
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Save data by dialog result.
        /// </summary>
        /// <param name="dialogResult"></param>
        /// <param name="textEditorControl"></param>
        private static void DialogResultAction(DialogResult dialogResult, TextEditorControl textEditorControl)
        {
            if (dialogResult == DialogResult.Yes)
                SaveToFileDialog();
            if (dialogResult == DialogResult.Cancel)
                GlobalVariables.noClear = true;
            else
                GlobalVariables.noClear = false;
        }

        /// <summary>
        ///  Open file and set title with path.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void OpenFileDialog(TextEditorControl textEditor)
        {
            var index = MainForm.Instance.EditorTabControl.SelectedIndex;
            ManageUnsavedData(textEditor, index, false);
            //if (GlobalVariables.noClear)
            //    return;
            string openedData = OpenFile();
            if (GlobalVariables.noFileSelected)
            {
                GlobalVariables.noFileSelected = false;
                return;
            }
            textEditor.Text = openedData;
            FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
            GlobalVariables.openedFileName = fileInfo.Name;
            MainForm.Instance.Text = $"{fileInfo.Name} : {GetFilePath(GlobalVariables.openedFilePath)} - CIARE {MainForm.Instance.versionName}";
            var filePath = $"{GetFilePath(GlobalVariables.openedFilePath)}\\{GlobalVariables.openedFileName}";
            var previousTabPath = MainForm.Instance.EditorTabControl.SelectedTab.ToolTipText;
            MainForm.Instance.EditorTabControl.SelectedTab.ToolTipText = filePath;
            MainForm.Instance.EditorTabControl.SelectedTab.Text = $"{GlobalVariables.openedFileName}               ";
            TabControllerManage.StoreFileSize(filePath, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePath, index); // Store file path in user profile.
            if (GlobalVariables.OStartUp)
                TabControllerManage.StoreDeleteTabs(previousTabPath, filePath, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll, index);  // Store tabs title and index.
            AutoStartFile autoStartFile = new AutoStartFile(GlobalVariables.regUserRunPath, GlobalVariables.markFile, GlobalVariables.markFile, GlobalVariables.openedFilePath);
            autoStartFile.CheckFilePath();
        }

        /// <summary>
        /// Open file in drag drop
        /// </summary>
        /// <param name="textEditor"></param>
        /// <param name="filePath"></param>
        public static void OpenFileDragDrop(TextEditorControl textEditor, string filePath)
        {
            var index = MainForm.Instance.EditorTabControl.SelectedIndex;
            ManageUnsavedData(textEditor, index, false);
            //if (GlobalVariables.noClear)
            //    return;
            FileInfo fileInfo = new FileInfo(filePath);
            GlobalVariables.openedFilePath = filePath;
            GlobalVariables.openedFileName = fileInfo.Name;
            using (var reader = new StreamReader(filePath))
            {
                MainForm.Instance.EditorTabControl.SelectTab(index);
                textEditor.Text = reader.ReadToEnd();
                var previousTabPath = MainForm.Instance.EditorTabControl.SelectedTab.ToolTipText;
                MainForm.Instance.Text = $"{fileInfo.Name} : {filePath} - CIARE {MainForm.Instance.versionName}";
                MainForm.Instance.EditorTabControl.SelectedTab.Text = $"{fileInfo.Name}               ";
                MainForm.Instance.EditorTabControl.SelectedTab.ToolTipText = filePath;
                TabControllerManage.StoreFileSize(filePath, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePath, index); // Store file path in user profile.
                if (GlobalVariables.OStartUp)
                    TabControllerManage.StoreDeleteTabs(previousTabPath, filePath, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll, index);  // Store tabs title and index.
            }
        }

        /// <summary>
        /// Save data from editor to a existing file/other file name if no path is found as opened.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void SaveToFileDialog()
        {
            try
            {
                string titleTab = MainForm.Instance.EditorTabControl.SelectedTab.Text.Trim();
                bool isSameTitleName = titleTab.Contains(GlobalVariables.openedFilePath);

                if (GlobalVariables.openedFilePath.Length > 0 && !titleTab.Contains("New Page"))
                {
                    File.WriteAllText(GlobalVariables.openedFilePath, SelectedEditor.GetSelectedEditor().Text);
                    FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                    MainForm.Instance.EditorTabControl.SelectedTab.Text = $"{titleTab.Replace("*", "")}               ";
                    MainForm.Instance.EditorTabControl.SelectedTab.ToolTipText = GlobalVariables.openedFilePath;
                    MainForm.Instance.Text = $"{GlobalVariables.openedFileName} : {GetFilePath(GlobalVariables.openedFilePath)} - CIARE {MainForm.Instance.versionName}";
                    StoreTabs(GlobalVariables.openedFilePath);
                    return;
                }
                SaveFile(SelectedEditor.GetSelectedEditor().Text);
                if (GlobalVariables.savedFile)
                {
                    FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                    MainForm.Instance.EditorTabControl.SelectedTab.Text = $"{GlobalVariables.openedFileName}               ";
                    MainForm.Instance.EditorTabControl.SelectedTab.ToolTipText = GlobalVariables.openedFilePath;
                    MainForm.Instance.Text = $"{GlobalVariables.openedFileName} : {GetFilePath(GlobalVariables.openedFilePath)} - CIARE {MainForm.Instance.versionName}";
                    StoreTabs(GlobalVariables.openedFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Store tabs if set in options on save.
        /// </summary>
        /// <param name="path"></param>
        private static void StoreTabs(string path)
        {
            int tabIndex = MainForm.Instance.EditorTabControl.SelectedIndex;

            if (GlobalVariables.OStartUp)
            {
                TabControllerManage.StoreDeleteTabs("", path, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll, tabIndex);
            }
            TabControllerManage.StoreFileSize(path, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePath, tabIndex);
        }

        /// <summary>
        /// Method for save modified data in a opened file when runing / compiling code.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void CompileRunSaveData(TextEditorControl textEditor)
        {
            if (!MainForm.Instance.Text.StartsWith("*") || !MainForm.Instance.EditorTabControl.SelectedTab.Text.StartsWith("*"))
                return;
            SaveToFileDialog();
        }

        /// <summary>
        /// Save data to a file.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void SaveAsDialog(TextEditorControl textEditor)
        {
            SaveFile(textEditor.Text);
            if (string.IsNullOrEmpty(GlobalVariables.openedFilePath))
                return;
            if (GlobalVariables.savedFile)
            {
                string titleTab = MainForm.Instance.EditorTabControl.SelectedTab.Text.Trim();
                MainForm.Instance.Text = $"{GlobalVariables.openedFileName} : {GetFilePath(GlobalVariables.openedFilePath)} - CIARE {MainForm.Instance.versionName}";
                MainForm.Instance.EditorTabControl.SelectedTab.Text = $"{GlobalVariables.openedFileName}               ";
                MainForm.Instance.EditorTabControl.SelectedTab.ToolTipText =GlobalVariables.openedFilePath;
                int tabIndex = MainForm.Instance.EditorTabControl.SelectedIndex;
                if (GlobalVariables.OStartUp)
                {
                    TabControllerManage.StoreDeleteTabs(titleTab, GlobalVariables.openedFilePath, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll, tabIndex);
                }
                TabControllerManage.StoreFileSize(GlobalVariables.openedFilePath, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePath, tabIndex);
            }
        }

        /// <summary>
        /// Set new empty editor.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void NewFile(TextEditorControl textEditor, RichTextBox logOutput)
        {
            var index = MainForm.Instance.EditorTabControl.SelectedIndex;
            ManageUnsavedData(textEditor, index, false);
            if (GlobalVariables.noClear)
                return;
            Control ctrl = MainForm.Instance.EditorTabControl.Controls[index].Controls[0];
            textEditor = ctrl as TextEditorControl;
            textEditor.Clear();
            logOutput.Clear();
            string path = GlobalVariables.openedFilePath;
            GlobalVariables.openedFilePath = string.Empty;
            GlobalVariables.savedFile = false;
            MainForm.Instance.Text = $"CIARE {MainForm.Instance.versionName}";
            MainForm.Instance.EditorTabControl.SelectedTab.Text = $"New Page               ";
            MainForm.Instance.markStartFileChk.Checked = false;
            if (GlobalVariables.OStartUp)
            {
                TabControllerManage.StoreDeleteTabs("", MainForm.Instance.EditorTabControl.SelectedTab.Text, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll, 0, true, path);
                TabControllerManage.DeleteFileSize(MainForm.Instance.EditorTabControl, path, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePath, index.ToString());
            }
        }

        /// <summary>
        /// Check if opened files is edited by an external application and ask if want to reaload the changed file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileSize"></param>
        /// <param name="textEditorControl"></param>
        public static void CheckFileExternalEdited(string fileTabStore)
        {
            if (!File.Exists(fileTabStore))
                return;

            var readTabsLines = File.ReadAllLines(fileTabStore);

            if (readTabsLines.Count() == 0)
                return;

            foreach (var line in readTabsLines)
            {
                string filePath = line.Split('|')[0];
                long fileSize = long.Parse(line.Split('|')[1]);
                int tabIndex = Int32.Parse(line.Split('|')[2]);
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileSize != fileInfo.Length)
                {
                    DialogResult dr = MessageBox.Show($"{fileInfo.Name} was changed.\nDo you want to reload it?", "CIARE", MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning);
                    if (dr == DialogResult.Yes)
                    {
                        using (var reader = new StreamReader(filePath))
                        {
                            MainForm.Instance.EditorTabControl.SelectTab(tabIndex);
                            SelectedEditor.GetSelectedEditor(tabIndex).Text = reader.ReadToEnd();
                            MainForm.Instance.Text = $"{fileInfo.Name} : {GetFilePath(GlobalVariables.openedFilePath)} - CIARE {MainForm.Instance.versionName}";
                            MainForm.Instance.EditorTabControl.SelectedTab.Text = $"{fileInfo.Name}               ";
                            MainForm.Instance.EditorTabControl.SelectedTab.ToolTipText = $"{GetFilePath(GlobalVariables.openedFilePath)}\\{fileInfo.Name}";
                            TabControllerManage.StoreFileSize(filePath, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePath, tabIndex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load C# code sample method.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void LoadCSTemplate(TextEditorControl textEditor)
        {
            var index = MainForm.Instance.EditorTabControl.SelectedIndex;
            ManageUnsavedData(textEditor, index);
            DialogResult dr = MessageBox.Show("Do you really want to load C# code template?", "CIARE", MessageBoxButtons.YesNo,
MessageBoxIcon.Information);
            if (dr == DialogResult.Yes)
            {
                string path = GlobalVariables.openedFilePath;
                if (!string.IsNullOrEmpty(path))
                {
                    MainForm.Instance.Text = $"*{GlobalVariables.openedFileName} : {GetFilePath(GlobalVariables.openedFilePath)} - CIARE {MainForm.Instance.versionName}";
                    MainForm.Instance.EditorTabControl.SelectedTab.Text = $"*{GlobalVariables.openedFileName}             ";
                }
                else
                    MainForm.Instance.Text = $"CIARE {MainForm.Instance.versionName}";
                textEditor.Text = GlobalVariables.roslynTemplate;
            }
        }

        /// <summary>
        /// Search dll file in nuget extracted archive.
        /// </summary>
        /// <param name="directoryName"></param>
        public static void SearchFile(string directoryName, List<string> listFramework)
        {
            GetLibsFromPacakage(directoryName);
            foreach (var framework in listFramework)
            {
                foreach (var file in s_packageLibs)
                {
                    if (file.Contains(@"\analyzers\")) continue;
                    if (file.Contains(@$"\{framework}\") && file.Contains(@"\lib\") && file.EndsWith(".dll"))
                    {
                        var fileInfo = new FileInfo(file);
                        if (!GlobalVariables.customRefAsm.Any(item => item.EndsWith(fileInfo.Name) && !item.Contains("netstandard")))
                        {
                            GlobalVariables.customRefAsm.Add(file);
                            break;
                        }
                    }

                    if (file.Contains(@$"\{framework}\") && file.Contains(@"\build\") && file.EndsWith(".dll"))
                    {
                        var fileInfo = new FileInfo(file);
                        if (!GlobalVariables.customRefAsm.Any(item => item.EndsWith(fileInfo.Name) && !item.Contains("netstandard")))
                        {
                            GlobalVariables.customRefAsm.Add(file);
                            break;
                        }
                    }

                    if (file.EndsWith(".dll") && file.Contains(@"\dotnet\"))
                    {
                        var fileInfo = new FileInfo(file);
                        if (!GlobalVariables.customRefAsm.Any(item => item.EndsWith(fileInfo.Name) && !item.Contains("netstandard")))
                        {
                            GlobalVariables.customRefAsm.Add(file);
                            break;
                        }
                    }
                }
            }
            s_packageLibs.Clear();
        }


        /// <summary>
        /// Get libraries from NuGet pacakage.
        /// </summary>
        /// <param name="directoryName"></param>
        private static void GetLibsFromPacakage(string directoryName)
        {
            var dirsList = new List<string>();
            var fileList = new List<string>();
            Directory.GetDirectories(directoryName).ToList().ForEach(dir => dirsList.Add(dir));
            Directory.GetFiles(directoryName).ToList().ForEach(file => fileList.Add(file));
            foreach (var file in fileList)
                if (file.EndsWith($".dll"))
                    if (!s_packageLibs.Any(item => item.Contains(file)))
                        s_packageLibs.Add(file);

            foreach (var dir in dirsList)
                GetLibsFromPacakage(dir);
        }

        /// <summary>
        /// Open file dialog for dynamic text editor.
        /// </summary>
        /// <param name="textEditorControl"></param>
        public static void OpenFileTab(TabControl tabControl, TextEditorControl textEditorControl)
        {
            int selectedTab = tabControl.SelectedIndex;
            int liveIndex = GlobalVariables.liveTabIndex;
            if (GlobalVariables.apiConnected && selectedTab == liveIndex)
                OpenFileDialog(SelectedEditor.GetSelectedEditor(GlobalVariables.liveTabIndex));
            else
                OpenFileDialog(textEditorControl);
        }

        /// <summary>
        /// Save file dilaog from dynamic text editor.
        /// </summary>
        /// <param name="textEditorControl"></param>
        public static void SaveFileTab(TabControl tabControl, TextEditorControl textEditorControl)
        {
            int selectedTab = tabControl.SelectedIndex;
            Control ctrl = tabControl.Controls[selectedTab].Controls[0];
            textEditorControl = ctrl as TextEditorControl;
            SaveToFileDialog();
        }


        /// <summary>
        /// Load data to text editor and sanitize path of file.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="textEditorControl"></param>
        private static void LoadParamFile(string data, TabControl tabControl)
        {
            if (data.StartsWith("cli|"))
            {
                string file = data.Split('|')[1];
                FileInfo fileInfo = new FileInfo(file);

                using (var reader = new StreamReader(file))
                {
                    TabControllerManage.AddNewTab(tabControl);
                    tabControl.Invoke(delegate
                    {
                        SelectedEditor.GetSelectedEditor().Text = reader.ReadToEnd();
                        MainForm.Instance.Text = $"{fileInfo.Name} : {GetFilePath(fileInfo.FullName)} - CIARE {MainForm.Instance.versionName}";
                        tabControl.SelectedTab.Text = $"{fileInfo.Name}               ";
                        tabControl.SelectedTab.ToolTipText = file;
                        if (GlobalVariables.OStartUp)
                        {
                            TabControllerManage.StoreDeleteTabs(file, file, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll, 0, false, MainForm.Instance.EditorTabControl.SelectedTab.ToolTipText);
                            TabControllerManage.StoreFileSize(file, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePath, tabControl.SelectedIndex);
                        }
                    });
                }
                return;
            }

            data = PathCheck(data);

            if (File.Exists(data))
            {
                MainForm.Instance.EditorTabControl.SelectTab(1);
                SelectedEditor.GetSelectedEditor(1).Clear();
                SelectedEditor.GetSelectedEditor(1).Text = File.ReadAllText(data);
                FileInfo fileInfo = new FileInfo(data);
                var previousTabPath = MainForm.Instance.EditorTabControl.SelectedTab.ToolTipText;
                MainForm.Instance.Text = $"{fileInfo.Name} : {GetFilePath(fileInfo.FullName)} - CIARE {MainForm.Instance.versionName}";
                MainForm.Instance.EditorTabControl.SelectedTab.Text = $"{fileInfo.Name}      ";
                MainForm.Instance.EditorTabControl.SelectedTab.ToolTipText = fileInfo.FullName;
                TabControllerManage.StoreFileSize(data, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePath, 1);
                if (GlobalVariables.OStartUp)
                    TabControllerManage.StoreDeleteTabs(previousTabPath, data, GlobalVariables.userProfileDirectory, GlobalVariables.tabsFilePathAll, 0, false, MainForm.Instance.EditorTabControl.SelectedTab.ToolTipText);
            }
        }

        /// <summary>
        /// Load files from arguments on cli.
        /// </summary>
        /// <param name="arg"></param>
        public static void OpenFileFromArgs(string arg, TabControl tabControl, bool isFromForm=false)
        {
            try
            {
                LoadParamFile(arg, tabControl);
                if (!GlobalVariables.noPath)
                {
                    arg = (arg.StartsWith("cli|"))? arg.Split('|')[1]: arg;
                    GlobalVariables.openedFilePath = arg;
                    FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                    GlobalVariables.openedFileName = fileInfo.Name;
                    if (arg.Length > 1)
                        MainForm.Instance.Text = $"{fileInfo.Name} - CIARE {MainForm.Instance.versionName}";
                }
            }
            catch { }
        }
    }
}
