using CIARE.Utils;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace CIARE
{
    public partial class Form1 : Form
    {
        private string _versionName;
        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _versionName = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            _versionName = _versionName.Substring(0, _versionName.Length - 2);
            this.Text = $"CIARE {_versionName}";
            WaterMark.TextBoxWaterMark(searchBox, "Find text...");
            ReadEditorHighlight(GlobalVariables.registryPath, textEditorControl1, highlightCMB);
            Console.SetOut(new ControlWriter(outputRBT));
            try
            {
                var args = Environment.GetCommandLineArgs();
                LoadParamFile(args[1], textEditorControl1);
                GlobalVariables.openedFilePath = args[1];
                this.Text = $"CIARE {_versionName} | {GlobalVariables.openedFilePath}";
            }
            catch { }
        }

        /// <summary>
        /// Read and apply highlight setting from registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        /// <param name="textEditor"></param>
        /// <param name="comboBox"></param>
        private void ReadEditorHighlight(string regKeyName, ICSharpCode.TextEditor.TextEditorControl textEditor, ComboBox comboBox)
        {
            string regHighlight = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", "highlight");
            if (regHighlight.Length > 0)
            {
                textEditor.SetHighlighting(regHighlight);
                comboBox.Text = regHighlight;
                return;
            }
            RegistryManagement.RegKey_CreateKey(regKeyName, "highlight", "Default");
        }

        /// <summary>
        /// Button event for start compile and run code from editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void runCodePb_Click(object sender, EventArgs e)
        {
            outputRBT.Text = "Compile and Runing..\n";
            runCodePb.Image = Properties.Resources.runButton_gray;
            runCodePb.Enabled = false;
            Roslyn.RoslynRun.CompileAndRun(textEditorControl1.Text, "", outputRBT);
            runCodePb.Image = Properties.Resources.runButton2;
            runCodePb.Enabled = true;
            GC.Collect();
        }

        /// <summary>
        /// Exit Main application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        /// <summary>
        /// Open file on text editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        /// <summary>
        /// Save data from text editor. (Save)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveToFile();
        }

        /// <summary>
        /// Load data to text editor and sanitize path of file.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="textEditorControl"></param>
        private void LoadParamFile(string data, ICSharpCode.TextEditor.TextEditorControl textEditorControl)
        {
            data = FileManage.PathCheck(data);
            if (File.Exists(data))
                textEditorControl1.Clear(); textEditorControl.Text = File.ReadAllText(data);
        }

        /// <summary>
        /// Save data from text editor. (Save As)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveAsStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        /// <summary>
        /// Text change event on editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textEditorControl1_TextChanged(object sender, EventArgs e)
        {
            if (GlobalVariables.openedFilePath.Length > 0)
                this.Text = $"CIARE {_versionName} | *{GlobalVariables.openedFilePath}";
        }

        /// <summary>
        /// Clear the editor and path for new file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            textEditorControl1.Clear();
            GlobalVariables.openedFilePath = string.Empty;
            this.Text = $"CIARE {_versionName}";
        }

        /// <summary>
        /// Override the key combination listener for file management events.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.N | Keys.Control:
                    NewFile();
                    return true;
                case Keys.S | Keys.Control:
                    SaveToFile();
                    return true;
                case Keys.S | Keys.Control | Keys.Shift:
                    SaveAs();
                    return true;
                case Keys.O | Keys.Control:
                    OpenFile();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        internal void Find(TextEditorControl editor, string text)
        {
            try
            {
                var offset = editor.Text.ToLower().IndexOf(text);
                if (offset > 0)
                {
                    var endOffset = offset + text.Length;
                    editor.ActiveTextAreaControl.TextArea.Caret.Position = editor.ActiveTextAreaControl.TextArea.Document.OffsetToPosition(endOffset);
                    editor.ActiveTextAreaControl.TextArea.SelectionManager.ClearSelection();
                    var document = editor.ActiveTextAreaControl.TextArea.Document;
                    var selection = new DefaultSelection(document, document.OffsetToPosition(offset), document.OffsetToPosition(endOffset));
                    editor.ActiveTextAreaControl.TextArea.SelectionManager.SetSelection(selection);
                }
            }catch(Exception e)
            {
                outputRBT.Text = e.ToString();
            }
        }

        /// <summary>
        /// Open file and set title with path.
        /// </summary>
        private void OpenFile()
        {
            string openedData = FileManage.OpenFile();
            if (openedData.Length > 0)
            {
                textEditorControl1.Clear();
                textEditorControl1.Text = openedData;
                this.Text = $"CIARE {_versionName} | {GlobalVariables.openedFilePath}";
            }
        }

        /// <summary>
        /// Set new empty editor.
        /// </summary>
        private void NewFile()
        {
            textEditorControl1.Clear();
            GlobalVariables.openedFilePath = string.Empty;
            this.Text = $"CIARE {_versionName}";
        }

        /// <summary>
        /// Save data from editor to a existing file/other file name if no path is found as opened.
        /// </summary>
        private void SaveToFile()
        {
            try
            {
                if (GlobalVariables.openedFilePath.Length > 0)
                {
                    File.WriteAllText(GlobalVariables.openedFilePath, textEditorControl1.Text);
                    this.Text = $"CIARE {_versionName} | {GlobalVariables.openedFilePath}";
                    return;
                }
                FileManage.SaveFile(textEditorControl1.Text);
                if (GlobalVariables.savedFile)
                {
                    this.Text = $"CIARE {_versionName} | {GlobalVariables.openedFilePath}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        /// <summary>
        /// Save data to a file.
        /// </summary>
        private void SaveAs()
        {
            FileManage.SaveFile(textEditorControl1.Text);
            if (GlobalVariables.savedFile)
            {
                this.Text = $"CIARE {_versionName} | {GlobalVariables.openedFilePath}";
            }
        }

        /// <summary>
        /// Change Highlight of text via combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void highlightCMB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (highlightCMB.Text.Length > 0)
            {
                textEditorControl1.SetHighlighting(highlightCMB.Text);
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, "highlight", highlightCMB.Text);
            }
        }

        /// <summary>
        /// Load predefined C# code sample for run with Roslyn!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadCStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Do you really want to load C# code template?", "CIARE", MessageBoxButtons.YesNo,
    MessageBoxIcon.Information);

            if (dr == DialogResult.Yes)
            {
                textEditorControl1.Text = GlobalVariables.roslynTemplate;
            }
        }

        /// <summary>
        /// Open about window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }

        private void findButton_Click(object sender, EventArgs e)
        {
            if (searchBox.Text.Length > 0)
            {
                Find(textEditorControl1, searchBox.Text);
            }
        }
    }
}
