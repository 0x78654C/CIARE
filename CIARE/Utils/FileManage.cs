using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using CIARE.Reference;
using CIARE.Utils.FilesOpenOS;
using ICSharpCode.TextEditor;
using Newtonsoft.Json.Bson;
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
        private static bool s_isLoaded = false;
        private static int s_lastIndex = 0;
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
                    return ManageCommandFileParam(MainForm.Instance.selectedEditor, data);
                return data;
            }
            else
            {
                return ManageCommandFileParam(MainForm.Instance.selectedEditor, data);
            }
        }

        /// <summary>
        /// Open file with no path event.
        /// </summary>
        /// <param name="textEditorControl"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string ManageCommandFileParam(TextEditorControl textEditorControl, string fileName)
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
                    SaveAsDialog(textEditorControl);
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
        /// Handle unsaved data from editor on from closing event.
        /// </summary>
        /// <param name="textEditorControl"></param>
        public static void ManageUnsavedData(TextEditorControl textEditorControl)
        {
            DialogResult dr = DialogResult.No;
            //            if (MainForm.Instance.Text.StartsWith("*"))
            //            {
            //                dr = MessageBox.Show("There is unsaved data. Do you want to save it?", "CIARE", MessageBoxButtons.YesNoCancel,
            //MessageBoxIcon.Warning);
            //            }
            //            else if (!MainForm.Instance.Text.Contains("-"))
            //            {
            //                if (!string.IsNullOrEmpty(textEditorControl.Text))
            //                    dr = MessageBox.Show("There is unsaved data. Do you want to save it?", "CIARE", MessageBoxButtons.YesNoCancel,
            //    MessageBoxIcon.Warning);
            //            }

            // Check if any tab has unsaved data.
            foreach (TabPage tab in MainForm.Instance.EditorTabControl.TabPages)
            {
                if (tab.Text.StartsWith("*") || tab.Text.Contains("New Page"))
                {
                    if (!string.IsNullOrEmpty(textEditorControl.Text))
                        dr = MessageBox.Show("There is unsaved data. Do you want to save it?", "CIARE", MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Warning);
                    MainForm.Instance.EditorTabControl.SelectTab(tab);
                    DialogResultAction(dr, textEditorControl);
                }
            }

            //DialogResultAction(dr, textEditorControl);
        }

        /// <summary>
        /// Save data by dialog result.
        /// </summary>
        /// <param name="dialogResult"></param>
        /// <param name="textEditorControl"></param>
        private static void DialogResultAction(DialogResult dialogResult, TextEditorControl textEditorControl)
        {
            if (dialogResult == DialogResult.Yes)
                SaveToFileDialog(textEditorControl);
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
            s_lastIndex = MainForm.Instance.EditorTabControl.SelectedIndex;
            ManageUnsavedData(textEditor);
            if (GlobalVariables.noClear)
                return;
            MainForm.Instance.EditorTabControl.SelectedIndex = s_lastIndex;
            string openedData = OpenFile();
            if (GlobalVariables.noFileSelected)
            {
                GlobalVariables.noFileSelected = false;
                return;
            }
            textEditor.Clear();
            textEditor.Text = openedData;
            FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
            GlobalVariables.openedFileName = fileInfo.Name;
            MainForm.Instance.openedFileLength = fileInfo.Length;
            MainForm.Instance.Text = $"{GlobalVariables.openedFileName} : {GetFilePath(GlobalVariables.openedFilePath)} - CIARE {MainForm.Instance.versionName}";
            MainForm.Instance.EditorTabControl.SelectedTab.Text = $"{GlobalVariables.openedFileName} : {GetFilePath(GlobalVariables.openedFilePath)}";
            AutoStartFile autoStartFile = new AutoStartFile(GlobalVariables.regUserRunPath, GlobalVariables.markFile, GlobalVariables.markFile, GlobalVariables.openedFilePath);
            autoStartFile.CheckFilePath();
        }

        /// <summary>
        /// Save data from editor to a existing file/other file name if no path is found as opened.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void SaveToFileDialog(TextEditorControl textEditor)
        {
            try
            {
                string titleTab = MainForm.Instance.EditorTabControl.SelectedTab.Text;
                string fileName = "";
                string filePath = "";
                string fullPath = "";
                if (titleTab.Contains(":")) {
                    fileName = titleTab.SplitByText(" : ", 0).Replace("*","");
                    filePath = titleTab.SplitByText(" : ", 1);
                    fullPath = $"{filePath}\\{fileName}";
                    GlobalVariables.openedFileName = fileName;
                    GlobalVariables.openedFilePath = fullPath;
                }

               
                bool isSameTitleName = titleTab.Contains(GlobalVariables.openedFilePath);

                if (GlobalVariables.openedFilePath.Length > 0 && !titleTab.Contains("New Page"))
                {
                    File.WriteAllText(GlobalVariables.openedFilePath, textEditor.Text);
                    FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                    MainForm.Instance.openedFileLength = fileInfo.Length;
                    MainForm.Instance.EditorTabControl.SelectedTab.Text = titleTab.Replace("*", "");
                    MainForm.Instance.Text = $"{GlobalVariables.openedFileName} : {GetFilePath(GlobalVariables.openedFilePath)} - CIARE {MainForm.Instance.versionName}";
                    return;
                }
                SaveFile(textEditor.Text);
                if (GlobalVariables.savedFile)
                {
                    FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                    MainForm.Instance.openedFileLength = fileInfo.Length;
                    MainForm.Instance.EditorTabControl.SelectedTab.Text = $"{GlobalVariables.openedFileName} : {GetFilePath(GlobalVariables.openedFilePath)}";
                    MainForm.Instance.Text = $"{GlobalVariables.openedFileName} : {GetFilePath(GlobalVariables.openedFilePath)} - CIARE {MainForm.Instance.versionName}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Method for save modified data in a opened file when runing / compiling code.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void CompileRunSaveData(TextEditorControl textEditor)
        {
            if (!MainForm.Instance.Text.StartsWith("*") || !MainForm.Instance.EditorTabControl.SelectedTab.Text.StartsWith("*"))
                return;
            SaveToFileDialog(textEditor);
        }

        /// <summary>
        /// Save data to a file.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void SaveAsDialog(TextEditorControl textEditor)
        {
            string title = MainForm.Instance.Text;
            SaveFile(textEditor.Text);
            if (string.IsNullOrEmpty(GlobalVariables.openedFilePath))
                return;
            FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
            MainForm.Instance.openedFileLength = fileInfo.Length;
            if (GlobalVariables.savedFile)
            {
                MainForm.Instance.Text = $"{GlobalVariables.openedFileName} : {GetFilePath(GlobalVariables.openedFilePath)} - CIARE {MainForm.Instance.versionName}";
                MainForm.Instance.EditorTabControl.SelectedTab.Text = $"{GlobalVariables.openedFileName} : {GetFilePath(GlobalVariables.openedFilePath)}";
            }
        }

        /// <summary>
        /// Set new empty editor.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void NewFile(TextEditorControl textEditor, RichTextBox logOutput)
        {
            s_lastIndex = MainForm.Instance.EditorTabControl.SelectedIndex;
            ManageUnsavedData(textEditor);
            if (GlobalVariables.noClear)
                return;
            MainForm.Instance.EditorTabControl.SelectedIndex = s_lastIndex;
            int selectedTab = MainForm.Instance.EditorTabControl.SelectedIndex;
            Control ctrl = MainForm.Instance.EditorTabControl.Controls[selectedTab].Controls[0];
            textEditor = ctrl as TextEditorControl;
            textEditor.Clear();
            logOutput.Clear();
            GlobalVariables.openedFilePath = string.Empty;
            GlobalVariables.savedFile = false;
            MainForm.Instance.Text = $"CIARE {MainForm.Instance.versionName}";
            MainForm.Instance.markStartFileChk.Checked = false;
        }

        /// <summary>
        /// Check if opened files is edited by an external application and ask if want to reaload the changed file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileSize"></param>
        /// <param name="textEditorControl"></param>
        public static void CheckFileExternalEdited(string filePath, long fileSize, TextEditorControl textEditorControl)
        {
            if (!File.Exists(filePath))
                return;

            FileInfo fileInfo = new FileInfo(filePath);
            if (fileSize != fileInfo.Length)
            {
                DialogResult dr = MessageBox.Show("The opened file content was changed.\nDo you want to reload it?", "CIARE", MessageBoxButtons.YesNo,
    MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        textEditorControl.Clear();
                        textEditorControl.Text = reader.ReadToEnd();
                        MainForm.Instance.Text = $"{fileInfo.Name} : {GetFilePath(GlobalVariables.openedFilePath)} - CIARE {MainForm.Instance.versionName}";
                        MainForm.Instance.EditorTabControl.SelectedTab.Text = $"{fileInfo.Name} : {GetFilePath(GlobalVariables.openedFilePath)}";
                        MainForm.Instance.openedFileLength = fileInfo.Length;
                    }
                    return;
                }
                MainForm.Instance.openedFileLength = fileInfo.Length;
            }
        }

        /// <summary>
        /// Load C# code sample method.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void LoadCSTemplate(TextEditorControl textEditor)
        {
            ManageUnsavedData(textEditor);
            DialogResult dr = MessageBox.Show("Do you really want to load C# code template?", "CIARE", MessageBoxButtons.YesNo,
MessageBoxIcon.Information);
            if (dr == DialogResult.Yes)
            {
                string path = GlobalVariables.openedFilePath;
                if (!string.IsNullOrEmpty(path))
                {
                    MainForm.Instance.Text = $"*{GlobalVariables.openedFileName} : {GetFilePath(GlobalVariables.openedFilePath)} - CIARE {MainForm.Instance.versionName}";
                    MainForm.Instance.EditorTabControl.SelectedTab.Text = $"*{GlobalVariables.openedFileName} : {GetFilePath(GlobalVariables.openedFilePath)}";
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
            Control ctrl = tabControl.Controls[selectedTab].Controls[0];
            textEditorControl = ctrl as TextEditorControl;
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
            SaveToFileDialog(textEditorControl);
        }
    }
}
