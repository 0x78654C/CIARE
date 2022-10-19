using System;
using CIARE.Utils;
using CIARE.GUI;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using System.Text.RegularExpressions;
using ICSharpCode.TextEditor.Document;
using System.Web;

namespace CIARE
{
    /*
     Form for find and replace string in main text editor.
     */
    public partial class FindAndReplace : Form
    {
        private bool _ignoreCaseFR = false;
        private bool _ignoreCaseF = false;
        private bool _matchCase = false;
        private int _startPos = 0;
        public FindAndReplace()
        {
            InitializeComponent();
        }

        private void FindAndReplace_Load(object sender, EventArgs e)
        {
            WaterMark.TextBoxWaterMark(findTxt, "Find what...");
            WaterMark.TextBoxWaterMark(repalceWithTxt, "Replace with...");
            WaterMark.TextBoxWaterMark(findTxtBox, "Find text...");

            if (GlobalVariables.findTabOpen)
            {
                findNReplaceTab.SelectTab(0);
                ActiveControl = findTxtBox;
            }
            else
                findNReplaceTab.SelectTab(1);

            if (GlobalVariables.darkColor)
                DarkMode.FinAndReplaceDarkMode(this, singleReplaceBtn, multiReplaceBtn, findTxt, repalceWithTxt, groupBox1,
                    ignoreCaseCheckBox, findNReplaceTab.TabPages[0], findNReplaceTab.TabPages[1], findBtn, findTxtBox);

            LoadFindRepalceStoredData(GlobalVariables.findData, GlobalVariables.findWhat, GlobalVariables.repalceWith);
        }

        /// <summary>
        /// Load last used data for find/find and replace option.
        /// </summary>
        /// <param name="findData"></param>
        /// <param name="findWhat"></param>
        /// <param name="replaceWith"></param>
        private void LoadFindRepalceStoredData(string findData, string findWhat, string replaceWith)
        {
            if (!string.IsNullOrEmpty(findData))
                findTxtBox.Text = findData;

            if (!string.IsNullOrEmpty(findWhat))
                findTxt.Text = findWhat;

            if (!string.IsNullOrEmpty(replaceWith))
                repalceWithTxt.Text = replaceWith;
        }

        private void singleReplaceBtn_Click(object sender, EventArgs e)
        {
            if (Form1.Instance != null)
                ReplaceSingle(Form1.Instance.textEditorControl1, findTxt.Text, repalceWithTxt.Text, _ignoreCaseFR, _matchCase);
            StoreReplaceData(findTxt.Text, repalceWithTxt.Text);
        }


        private void multiReplaceBtn_Click(object sender, EventArgs e)
        {
            if (Form1.Instance != null)
                Form1.Instance.textEditorControl1.Text = ReplaceAll(Form1.Instance.textEditorControl1.Text, findTxt.Text, repalceWithTxt.Text, _ignoreCaseFR, _matchCase);
            StoreReplaceData(findTxt.Text, repalceWithTxt.Text);
        }


        /// <summary>
        /// Set last used params for replace data.
        /// </summary>
        /// <param name="findWhat"></param>
        /// <param name="repalceWith"></param>
        private void StoreReplaceData(string findWhat, string repalceWith)
        {
            GlobalVariables.findWhat = findWhat;
            GlobalVariables.repalceWith = repalceWith;
        }

        /// <summary>
        /// Repalce single string one by one.
        /// </summary>
        /// <param name="editor"></param>
        /// <param name="findWhat"></param>
        /// <param name="replaceWith"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        private string ReplaceSingle(TextEditorControl editor, string findWhat, string replaceWith, bool ignoreCase, bool matchCase)
        {
            try
            {
                if (string.IsNullOrEmpty(editor.Text))
                {
                    MessageBox.Show("There is no data in text editor!", "CIARE", MessageBoxButtons.OK,
        MessageBoxIcon.Warning);
                    return editor.Text;
                }

                if (string.IsNullOrEmpty(findWhat))
                {
                    MessageBox.Show("'Find what...' field is emtpy!", "CIARE", MessageBoxButtons.OK,
    MessageBoxIcon.Warning);
                    return editor.Text;
                }

                if (!editor.Text.ToLower().Contains(findWhat.ToLower()))
                {
                    MessageBox.Show($"Cannot find: {findWhat}", "CIARE", MessageBoxButtons.OK,
   MessageBoxIcon.Information);
                    return editor.Text;
                }

                int pos = _startPos;
                int leng = editor.Text.Length;
                string searchText;
                int offset;
                if (ignoreCase)
                {
                    searchText = editor.Text.Substring(pos).ToLower();
                    if (!searchText.Contains(findWhat.ToLower()))
                        _startPos = 0;
                    offset = searchText.IndexOf(findWhat.ToLower()) + _startPos;
                }
                else
                {
                    searchText = editor.Text.Substring(pos);
                    if (!searchText.Contains(findWhat))
                        _startPos = 0;
                    offset = searchText.IndexOf(findWhat) + _startPos;
                }
                var endOffset = offset + findWhat.Length;
                _startPos = endOffset;
                editor.ActiveTextAreaControl.TextArea.Caret.Position = editor.ActiveTextAreaControl.TextArea.Document.OffsetToPosition(endOffset);
                editor.ActiveTextAreaControl.TextArea.SelectionManager.ClearSelection();
                var document = editor.ActiveTextAreaControl.TextArea.Document;
                var selection = new DefaultSelection(document, document.OffsetToPosition(offset), document.OffsetToPosition(endOffset));
                editor.ActiveTextAreaControl.TextArea.SelectionManager.SetSelection(selection);
                string selectedText;
                if (ignoreCase)
                    selectedText = Regex.Replace(selection.SelectedText.ToLower(), findWhat.ToLower(), replaceWith, RegexOptions.IgnoreCase);
                else
                    selectedText = Regex.Replace(selection.SelectedText, findWhat, replaceWith, RegexOptions.None);
                editor.ActiveTextAreaControl.TextArea.InsertString(selectedText);
            }
            catch
            {
                _startPos = 0;
            }
            return findWhat;
        }

        /// <summary>
        /// Replace matching string in all data.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="findWhat"></param>
        /// <param name="replaceWith"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        private string ReplaceAll(string data, string findWhat, string replaceWith, bool ignoreCase, bool matchCase)
        {
            if (string.IsNullOrEmpty(data))
            {
                MessageBox.Show("There is no data in text editor!", "CIARE", MessageBoxButtons.OK,
    MessageBoxIcon.Warning);
                return data;
            }

            if (string.IsNullOrEmpty(findWhat))
            {
                MessageBox.Show("'Find what...' field is emtpy!", "CIARE", MessageBoxButtons.OK,
MessageBoxIcon.Warning);
                return data;
            }
            if (!data.ToLower().Contains(findWhat.ToLower()))
            {
                MessageBox.Show($"Cannot find: {findWhat}", "CIARE", MessageBoxButtons.OK,
MessageBoxIcon.Information);
                return data;
            }
            if (ignoreCase)
                return Regex.Replace(data, findWhat, replaceWith, RegexOptions.IgnoreCase);
            else
                return Regex.Replace(data, findWhat, replaceWith, RegexOptions.None);
        }

        /// <summary>
        /// Search next engine for text in text editor.
        /// </summary>
        /// <param name="editor"></param>
        /// <param name="text"></param>
        private void Find(TextEditorControl editor, string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    MessageBox.Show("You need to provide a text to search!", "CIARE", MessageBoxButtons.OK,
MessageBoxIcon.Warning);
                    return;
                }

                if (!editor.Text.ToLower().Contains(text.ToLower()))
                {
                    MessageBox.Show($"Cannot find: {text}", "CIARE", MessageBoxButtons.OK,
   MessageBoxIcon.Information);
                    return;
                }

                int pos = _startPos;
                int leng = editor.Text.Length;
                string searchText = string.Empty;
                int offset = 0;
                if (!_ignoreCaseF)
                {
                    searchText = editor.Text.Substring(pos);
                    if (!searchText.Contains(text))
                        _startPos = 0;
                    offset = searchText.IndexOf(text) + _startPos;
                }
                else
                {
                    searchText = editor.Text.Substring(pos).ToLower();
                    if (!searchText.Contains(text.ToLower()))
                        _startPos = 0;
                    offset = searchText.IndexOf(text.ToLower()) + _startPos;
                }
                var endOffset = offset + text.Length;
                _startPos = endOffset;
                editor.ActiveTextAreaControl.TextArea.Caret.Position = editor.ActiveTextAreaControl.TextArea.Document.OffsetToPosition(endOffset);
                editor.ActiveTextAreaControl.TextArea.SelectionManager.ClearSelection();
                var document = editor.ActiveTextAreaControl.TextArea.Document;
                var selection = new DefaultSelection(document, document.OffsetToPosition(offset), document.OffsetToPosition(endOffset));
                editor.ActiveTextAreaControl.TextArea.SelectionManager.SetSelection(selection);
            }
            catch
            {
                _startPos = 0;
            }
        }

        /// <summary>
        /// Set the flag for ignore case sensitive in replace.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ignoreCaseCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (ignoreCaseCheckBox.Checked)
                _ignoreCaseFR = true;
            else
                _ignoreCaseFR = false;
        }

        /// <summary>
        /// Set the flag for ignore case sensitive in find.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ignCaseSensFindCkb_CheckedChanged(object sender, EventArgs e)
        {
            if (ignCaseSensFindCkb.Checked)
                _ignoreCaseF = true;
            else
                _ignoreCaseF = false;
        }

        /// <summary>
        /// Button for search in texteditor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void findBtn_Click(object sender, EventArgs e)
        {
            Find(Form1.Instance.textEditorControl1, findTxtBox.Text);
            GlobalVariables.findData = findTxtBox.Text;
        }

        /// <summary>
        /// Overwirde escape key to close current form.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape:
                    this.Close();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
