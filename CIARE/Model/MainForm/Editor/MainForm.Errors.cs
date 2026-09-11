using System;
using System.Drawing;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using CIARE.Utils.OpenAISettings;

namespace CIARE
{
    public partial class MainForm
    {
        private string _clickedErrorLine = "";

        private void ConfigureErrorsListView()
        {
            if (errorsLV == null)
                return;

            if (errorsLV.Columns.Count == 0)
            {
                errorsLV.Columns.Add(string.Empty, 34, HorizontalAlignment.Center);
                errorsLV.Columns.Add("Line", 64, HorizontalAlignment.Right);
                errorsLV.Columns.Add("Code", 84, HorizontalAlignment.Left);
                errorsLV.Columns.Add("Message", 320, HorizontalAlignment.Left);
            }

            errorsLV.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            errorsLV.ShowItemToolTips = true;
            if (errorsLV.ListViewItemSorter == null)
                errorsLV.ListViewItemSorter = new CIARE.GUI.ListViewColumnSorter();
            errorsLV.Resize += errorsLV_Resize;
            ResizeErrorsListViewColumns();
        }

        private void errorsLV_Resize(object sender, EventArgs e)
        {
            ResizeErrorsListViewColumns();
        }

        private void ResizeErrorsListViewColumns()
        {
            if (errorsLV == null || errorsLV.IsDisposed || errorsLV.Columns.Count < 4)
                return;

            int availableWidth = errorsLV.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 6;
            if (availableWidth <= 0)
                return;

            int iconWidth = 34;
            int lineWidth = 64;
            int codeWidth = 84;
            int messageWidth = Math.Max(180, availableWidth - iconWidth - lineWidth - codeWidth);

            errorsLV.Columns[0].Width = iconWidth;
            errorsLV.Columns[1].Width = lineWidth;
            errorsLV.Columns[2].Width = codeWidth;
            errorsLV.Columns[3].Width = messageWidth;
        }

        private void OutputTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= outputTabControl.TabPages.Count)
                return;

            bool dark = GlobalVariables.darkColor;
            var g = e.Graphics;
            var tp = outputTabControl.TabPages[e.Index];
            var tabBounds = outputTabControl.GetTabRect(e.Index);
            bool selected = e.Index == outputTabControl.SelectedIndex;

            Color tabBackColor = dark
                ? (selected ? GlobalVariables.TabSelectedColor : GlobalVariables.TabBgColor)
                : (selected ? SystemColors.Window : SystemColors.Control);
            Color textColor = dark ? Color.FromArgb(192, 215, 207) : Color.Black;
            Color borderColor = dark ? GlobalVariables.TabSelectedColor : SystemColors.ControlDark;

            using (var bgBrush = new SolidBrush(tabBackColor))
                g.FillRectangle(bgBrush, tabBounds);
            using (var borderPen = new Pen(borderColor))
                g.DrawRectangle(borderPen, tabBounds.X, tabBounds.Y, tabBounds.Width - 1, tabBounds.Height - 1);

            if (e.Index == outputTabControl.TabPages.Count - 1)
            {
                TabControllerManage.SetTransparentTabBar(outputTabControl, e,
                    GlobalVariables.formBgColor.R, GlobalVariables.formBgColor.G, GlobalVariables.formBgColor.B);
            }

            Rectangle textBounds = Rectangle.Inflate(tabBounds, -8, 0);
            TextRenderer.DrawText(g, tp.Text, tp.Font ?? outputTabControl.Font, textBounds, textColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.SingleLine);
        }

        private void errorsLV_ItemActivate(object sender, EventArgs e)
        {
            if (errorsLV.SelectedItems.Count == 0) return;
            var item = errorsLV.SelectedItems[0];
            if (!int.TryParse(item.SubItems[1].Text, out int targetLine)) return;

            var editor = SelectedEditor.GetSelectedEditor();
            if (editor == null) return;

            GoToLineNumber.GoToLine(editor, targetLine);
            editor.Focus();
        }

        private void errorsLV_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var hit = errorsLV.HitTest(e.Location);
            if (hit.Item == null)
            {
                _clickedErrorLine = "";
                return;
            }
            var item = hit.Item;
            _clickedErrorLine = $"{item.SubItems[2].Text}: {item.SubItems[3].Text}";
        }

        private void errorsLV_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (errorsLV.ListViewItemSorter is CIARE.GUI.ListViewColumnSorter sorter)
            {
                sorter.SortColumn = e.Column;
                errorsLV.Sorting = SortOrder.None;
                errorsLV.Sort();
            }
        }

        private void copyErrorMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_clickedErrorLine))
                Clipboard.SetText(_clickedErrorLine);
        }

        private void askAiErrorMenuItem_Click(object sender, EventArgs e)
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
        private void outputRBT_MouseWheel(object sender, MouseEventArgs e) => GlobalVariables.zoomFactor = outputRBT.ZoomFactor;
    }
}
