﻿using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using CIARE.GUI;

namespace CIARE.Utils
{
    [SupportedOSPlatform("Windows")]
    public class LinesManage
    {
        /// <summary>
        /// Get care position(line,column) from editor data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void GetCaretPositon(object sender, EventArgs e)
        {
            SelectedEditor.GetSelectedEditor();
            if (!string.IsNullOrEmpty(SelectedEditor.GetSelectedEditor().Text))
                MainForm.Instance.linesPositionLbl.Text =
                    $"[Line {SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.Caret.Position.Line + 1}, Col {SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.Caret.Column}]";
            else
                MainForm.Instance.linesPositionLbl.Text = string.Empty;
        }

        /// <summary>
        /// Get total lines count from editor data.
        /// </summary>
        /// <param name="mainEditor"></param>
        /// <param name="totalLinesCountLbl"></param>
        public static void GetTotalLinesCount(Label totalLinesCountLbl)
        {
            if (!string.IsNullOrEmpty(SelectedEditor.GetSelectedEditor().Text))
                totalLinesCountLbl.Text = $"Lines: {SelectedEditor.GetSelectedEditor().Document.TotalNumberOfLines}";
            else
                totalLinesCountLbl.Text = string.Empty;
        }
    }
}
