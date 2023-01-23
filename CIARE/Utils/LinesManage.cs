﻿using System;
using System.Windows.Forms;
using ICSharpCode.TextEditor;

namespace CIARE.Utils
{
    public class LinesManage
    {
        /// <summary>
        /// Get care position(line,column) from editor data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void GetCaretPositon(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(MainForm.Instance.textEditorControl1.Text))
                MainForm.Instance.linesPositionLbl.Text =
                    $"[Line {MainForm.Instance.textEditorControl1.ActiveTextAreaControl.TextArea.Caret.Position.Line + 1}, Col {MainForm.Instance.textEditorControl1.ActiveTextAreaControl.TextArea.Caret.Column}]";
            else
                MainForm.Instance.linesPositionLbl.Text = string.Empty;
        }

        /// <summary>
        /// Get total lines count from editor data.
        /// </summary>
        /// <param name="mainEditor"></param>
        /// <param name="totalLinesCountLbl"></param>
        public static void GetTotalLinesCount(TextEditorControl mainEditor, Label totalLinesCountLbl)
        {
            if (!string.IsNullOrEmpty(mainEditor.Text))
                totalLinesCountLbl.Text = $"Lines: {mainEditor.Document.TotalNumberOfLines}";
            else
                totalLinesCountLbl.Text = string.Empty;
        }
    }
}
