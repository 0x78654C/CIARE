using System;
using System.IO;
using System.Windows.Forms;
using ICSharpCode.TextEditor;

namespace CIARE.Utils
{
    public class FileManage
    {
        private static OpenFileDialog s_openFileDialog = new OpenFileDialog();
        private static SaveFileDialog s_saveFileDialog = new SaveFileDialog();

        /// <summary>
        /// Open file dialog.
        /// </summary>
        /// <returns></returns>
        public static string OpenFile()
        {
            s_openFileDialog.Filter = "All Files (*.*)|*.*|C# Files (*.cs)|*.cs";
            s_openFileDialog.Title = "Select file top open:";
            s_openFileDialog.CheckFileExists = true;
            s_openFileDialog.CheckPathExists = true;
            DialogResult dr = s_openFileDialog.ShowDialog();
            if (dr == DialogResult.OK)
            {
                using (StreamReader reader = new StreamReader(s_openFileDialog.FileName))
                {
                    GlobalVariables.openedFilePath = s_openFileDialog.FileName;
                    return reader.ReadToEnd();
                }
            }
            return "";
        }

        /// <summary>
        /// Save file dialog.
        /// </summary>
        /// <param name="data"></param>
        public static void SaveFile(string data)
        {
            s_saveFileDialog.Filter = "All Files (*.*)|*.*|C# Files (*.cs)|*.cs";
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
                return data;
            else
                return Application.StartupPath + $"\\{data}";
        }


        /// <summary>
        /// Handle unsaved data from editor on from closing event.
        /// </summary>
        /// <param name="textEditorControl"></param>
        public static void ManageUnsavedData(TextEditorControl textEditorControl)
        {
            DialogResult dr = DialogResult.No;
            if (Form1.Instance.Text.Contains("| *"))
            {
                dr = MessageBox.Show("There is unsaved data. Do you want to save it?", "CIARE", MessageBoxButtons.YesNoCancel,
MessageBoxIcon.Warning);
            }
            else if (!Form1.Instance.Text.Contains("|"))
            {
                if (!string.IsNullOrEmpty(textEditorControl.Text))
                    dr = MessageBox.Show("There is unsaved data. Do you want to save it?", "CIARE", MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Warning);
            }

            if (dr == DialogResult.Yes)
                SaveToFileDialog(textEditorControl);
            if (dr == DialogResult.Cancel)
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
            ManageUnsavedData(textEditor);
            if (GlobalVariables.noClear)
                return;
            string openedData = OpenFile();
            if (openedData.Length > 0)
            {
                textEditor.Clear();
                textEditor.Text = openedData;
                FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                Form1.Instance.openedFileLength = fileInfo.Length;
                Form1.Instance.Text = $"CIARE { Form1.Instance.versionName} | {GlobalVariables.openedFilePath}";
            }
        }

        /// <summary>
        /// Save data from editor to a existing file/other file name if no path is found as opened.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void SaveToFileDialog(TextEditorControl textEditor)
        {
            try
            {
                if (GlobalVariables.openedFilePath.Length > 0)
                {
                    File.WriteAllText(GlobalVariables.openedFilePath, textEditor.Text);
                    FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                    Form1.Instance.openedFileLength = fileInfo.Length;
                    Form1.Instance.Text = $"CIARE { Form1.Instance.versionName} | {GlobalVariables.openedFilePath}";
                    return;
                }
                SaveFile(textEditor.Text);
                if (GlobalVariables.savedFile)
                {
                    FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
                    Form1.Instance.openedFileLength = fileInfo.Length;
                    Form1.Instance.Text = $"CIARE {Form1.Instance.versionName} | {GlobalVariables.openedFilePath}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Save data to a file.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void SaveAsDialog(TextEditorControl textEditor)
        {
            FileManage.SaveFile(textEditor.Text);
            if (string.IsNullOrEmpty(GlobalVariables.openedFilePath))
                return;
            FileInfo fileInfo = new FileInfo(GlobalVariables.openedFilePath);
            Form1.Instance.openedFileLength = fileInfo.Length;
            if (GlobalVariables.savedFile)
            {
                Form1.Instance.Text = $"CIARE {Form1.Instance.versionName} | {GlobalVariables.openedFilePath}";
            }
        }

        /// <summary>
        /// Set new empty editor.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void NewFile(TextEditorControl textEditor)
        {
            ManageUnsavedData(textEditor);
            if (GlobalVariables.noClear)
                return;
            textEditor.Clear();
            GlobalVariables.openedFilePath = string.Empty;
            Form1.Instance.Text = $"CIARE { Form1.Instance.versionName}";
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
                        Form1.Instance.Text = $"CIARE {Form1.Instance.versionName} | {filePath}";
                        Form1.Instance.openedFileLength = fileInfo.Length;
                    }
                    return;
                }
                Form1.Instance.openedFileLength = fileInfo.Length;
            }
        }

        /// <summary>
        /// Load C# code sample method.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void LoadCSTemplate(TextEditorControl textEditor)
        {
            FileManage.ManageUnsavedData(textEditor);
            DialogResult dr = MessageBox.Show("Do you really want to load C# code template?", "CIARE", MessageBoxButtons.YesNo,
MessageBoxIcon.Information);
            if (dr == DialogResult.Yes)
            {
                GlobalVariables.openedFilePath = string.Empty;
                Form1.Instance.Text = $"CIARE {Form1.Instance.versionName}";
                textEditor.Text = GlobalVariables.roslynTemplate;
            }
        }
    }
}
