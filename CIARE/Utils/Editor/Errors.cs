using System;
using System.Drawing;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using CIARE.Utils.OpenAISettings;

namespace CIARE.Utils.Editor
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class Errors
    {
        private readonly MainForm _mainForm;

        internal Errors(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        private string _clickedErrorLine = "";

        internal void ConfigureErrorsListView()
        {
            if (_mainForm.errorsLV == null)
                return;

            if (_mainForm.errorsLV.Columns.Count == 0)
            {
                _mainForm.errorsLV.Columns.Add(string.Empty, 34, HorizontalAlignment.Center);
                _mainForm.errorsLV.Columns.Add("Line", 64, HorizontalAlignment.Right);
                _mainForm.errorsLV.Columns.Add("Code", 84, HorizontalAlignment.Left);
                _mainForm.errorsLV.Columns.Add("Message", 320, HorizontalAlignment.Left);
            }

            _mainForm.errorsLV.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _mainForm.errorsLV.ShowItemToolTips = true;
            if (_mainForm.errorsLV.ListViewItemSorter == null)
                _mainForm.errorsLV.ListViewItemSorter = new CIARE.GUI.ListViewColumnSorter();
            _mainForm.errorsLV.Resize += errorsLV_Resize;
            ResizeErrorsListViewColumns();
        }

        private void errorsLV_Resize(object sender, EventArgs e)
        {
            ResizeErrorsListViewColumns();
        }

        private void ResizeErrorsListViewColumns()
        {
            if (_mainForm.errorsLV == null || _mainForm.errorsLV.IsDisposed || _mainForm.errorsLV.Columns.Count < 4)
                return;

            int availableWidth = _mainForm.errorsLV.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 6;
            if (availableWidth <= 0)
                return;

            int iconWidth = 34;
            int lineWidth = 64;
            int codeWidth = 84;
            int messageWidth = Math.Max(180, availableWidth - iconWidth - lineWidth - codeWidth);

            _mainForm.errorsLV.Columns[0].Width = iconWidth;
            _mainForm.errorsLV.Columns[1].Width = lineWidth;
            _mainForm.errorsLV.Columns[2].Width = codeWidth;
            _mainForm.errorsLV.Columns[3].Width = messageWidth;
        }

        internal void OutputTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _mainForm.outputTabControl.TabPages.Count)
                return;

            bool dark = GlobalVariables.darkColor;
            var g = e.Graphics;
            var tp = _mainForm.outputTabControl.TabPages[e.Index];
            var tabBounds = _mainForm.outputTabControl.GetTabRect(e.Index);
            bool selected = e.Index == _mainForm.outputTabControl.SelectedIndex;

            Color tabBackColor = dark
                ? (selected ? GlobalVariables.TabSelectedColor : GlobalVariables.TabBgColor)
                : (selected ? SystemColors.Window : SystemColors.Control);
            Color textColor = dark ? Color.FromArgb(192, 215, 207) : Color.Black;
            Color borderColor = dark ? GlobalVariables.TabSelectedColor : SystemColors.ControlDark;

            using (var bgBrush = new SolidBrush(tabBackColor))
                g.FillRectangle(bgBrush, tabBounds);
            using (var borderPen = new Pen(borderColor))
                g.DrawRectangle(borderPen, tabBounds.X, tabBounds.Y, tabBounds.Width - 1, tabBounds.Height - 1);

            if (e.Index == _mainForm.outputTabControl.TabPages.Count - 1)
            {
                TabControllerManage.SetTransparentTabBar(_mainForm.outputTabControl, e,
                    GlobalVariables.formBgColor.R, GlobalVariables.formBgColor.G, GlobalVariables.formBgColor.B);
            }

            Rectangle textBounds = Rectangle.Inflate(tabBounds, -8, 0);
            TextRenderer.DrawText(g, tp.Text, tp.Font ?? _mainForm.outputTabControl.Font, textBounds, textColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.SingleLine);
        }

        internal void errorsLV_ItemActivate(object sender, EventArgs e)
        {
            if (_mainForm.errorsLV.SelectedItems.Count == 0) return;
            var item = _mainForm.errorsLV.SelectedItems[0];
            if (!int.TryParse(item.SubItems[1].Text, out int targetLine)) return;

            var editor = SelectedEditor.GetSelectedEditor();
            if (editor == null) return;

            GoToLineNumber.GoToLine(editor, targetLine);
            editor.Focus();
        }

        internal void errorsLV_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var hit = _mainForm.errorsLV.HitTest(e.Location);
            if (hit.Item == null)
            {
                _clickedErrorLine = "";
                return;
            }
            var item = hit.Item;
            _clickedErrorLine = $"{item.SubItems[2].Text}: {item.SubItems[3].Text}";
        }

        internal void errorsLV_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (_mainForm.errorsLV.ListViewItemSorter is CIARE.GUI.ListViewColumnSorter sorter)
            {
                sorter.SortColumn = e.Column;
                _mainForm.errorsLV.Sorting = SortOrder.None;
                _mainForm.errorsLV.Sort();
            }
        }

        internal void copyErrorMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_clickedErrorLine))
                Clipboard.SetText(_clickedErrorLine);
        }

        internal void askAiErrorMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_clickedErrorLine)) return;
            var editor = SelectedEditor.GetSelectedEditor();
            if (editor == null) return;
            AiManage.GetDataAIFromError(GlobalVariables.aiKey.ConvertSecureStringToString(), editor.Text, _clickedErrorLine);
        }

        /// <summary>
        /// RichtextBox mouse wheel event for store zoomfactor value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void outputRBT_MouseWheel(object sender, MouseEventArgs e) => GlobalVariables.zoomFactor = _mainForm.outputRBT.ZoomFactor;
    }
}
