using System;
using CIARE.Utils;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using System.Text.RegularExpressions;
using ICSharpCode.TextEditor.Document;

namespace CIARE
{
    /*
     Form for find and replace string in main text editor.
     */
    public partial class FindAndReplace : Form
    {
        private bool _ignoreCase = false;
        private int _startPos = 0;
        public FindAndReplace()
        {
            InitializeComponent();
        }

        private void singleReplaceBtn_Click(object sender, EventArgs e)
        {
            if (Form1.Instance != null)
                ReplaceSingle(Form1.Instance.textEditorControl1, findTxt.Text, repalceWithTxt.Text, _ignoreCase);
        }

        private void FindAndReplace_Load(object sender, EventArgs e)
        {
            WaterMark.TextBoxWaterMark(findTxt, "Find what...");
            WaterMark.TextBoxWaterMark(repalceWithTxt, "Repalce with...");
        }

        private void multiReplaceBtn_Click(object sender, EventArgs e)
        {
            if (Form1.Instance != null)
                Form1.Instance.textEditorControl1.Text = ReplaceAll(Form1.Instance.textEditorControl1.Text, findTxt.Text, repalceWithTxt.Text, _ignoreCase);
        }

        /// <summary>
        /// Repalce single string one by one.
        /// </summary>
        /// <param name="editor"></param>
        /// <param name="findWhat"></param>
        /// <param name="replaceWith"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        private string ReplaceSingle(TextEditorControl editor, string findWhat, string replaceWith, bool ignoreCase)
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

                if (string.IsNullOrEmpty(replaceWith))
                {
                    MessageBox.Show("'Replace with...' field is emtpy!", "CIARE", MessageBoxButtons.OK,
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
        private string ReplaceAll(string data, string findWhat, string replaceWith, bool ignoreCase)
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

            if (string.IsNullOrEmpty(replaceWith))
            {
                MessageBox.Show("'Replace with...' field is emtpy!", "CIARE", MessageBoxButtons.OK,
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
        /// Set the flag for ignore case sensitive in replace.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ignoreCaseCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (ignoreCaseCheckBox.Checked)
                _ignoreCase = true;
            else
                _ignoreCase = false;
        }
    }
}
