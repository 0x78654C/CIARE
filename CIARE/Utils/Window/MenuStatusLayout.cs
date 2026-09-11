using System;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Utils.Window
{
    [SupportedOSPlatform("windows")]
    internal sealed class MenuStatusLayout
    {
        private readonly MainForm _mainForm;
        private Panel _headerPanel;
        private Panel _statusPanel;
        private Control[] _statusControls;
        private bool _refreshing;
        private bool _fullScreen;

        internal MenuStatusLayout(MainForm mainForm)
        {
            _mainForm = mainForm;
        }

        internal void Initialize()
        {
            if (_statusPanel != null)
                return;

            _headerPanel = new Panel { Name = "editorHeaderPanel", Dock = DockStyle.Top, TabStop = false };
            _statusPanel = new Panel { Name = "editorStatusPanel", TabStop = false };
            // Pack from the right, keeping the caret and line count together.
            _statusControls = new Control[]
            {
                _mainForm.liveStatusPb, _mainForm.linesCountLbl, _mainForm.linesPositionLbl,
                _mainForm.warningsCheckLbl, _mainForm.typeCheckLbl
            };
            _mainForm.SuspendLayout();
            try
            {
                int menuIndex = _mainForm.Controls.GetChildIndex(_mainForm.menuStrip1);
                _mainForm.Controls.Add(_headerPanel);
                _mainForm.Controls.SetChildIndex(_headerPanel, menuIndex);
                foreach (var control in new Control[]
                {
                    _mainForm.menuStrip1, _mainForm.label2, _mainForm.runCodePb,
                    _mainForm.label3, _mainForm.markStartFileChk
                })
                    _headerPanel.Controls.Add(control);
                _mainForm.menuStrip1.Dock = DockStyle.None;
                _mainForm.menuStrip1.AutoSize = false;

                foreach (var control in _statusControls)
                {
                    control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    if (control is Label label)
                    {
                        label.AutoSize = false;
                        label.AutoEllipsis = true;
                    }
                    _statusPanel.Controls.Add(control);
                    control.TextChanged += Refresh;
                    control.FontChanged += Refresh;
                }

                _headerPanel.Controls.Add(_statusPanel);
                _mainForm.Layout += Refresh;
                _mainForm.ClientSizeChanged += Refresh;
                _mainForm.VisibleChanged += Refresh;
                _mainForm.DpiChanged += Refresh;
                _mainForm.menuStrip1.Layout += Refresh;
                _mainForm.markStartFileChk.VisibleChanged += Refresh;
            }
            finally
            {
                _mainForm.ResumeLayout(true);
            }
            Refresh();
        }

        internal void SetFullScreen(bool fullScreen)
        {
            _fullScreen = fullScreen;
            if (_headerPanel != null)
                _headerPanel.Visible = !fullScreen;
            Refresh();
        }

        private void Refresh(object sender, EventArgs e) => Refresh();

        internal void Refresh()
        {
            if (_statusPanel == null || _refreshing || _fullScreen || _mainForm.IsDisposed)
                return;

            _refreshing = true;
            _mainForm.SuspendLayout();
            try
            {
                var menu = _mainForm.menuStrip1;
                int gap = Math.Max(4, _mainForm.DeviceDpi * 6 / 96);
                int inset = Math.Max(1, _mainForm.DeviceDpi * 2 / 96);
                Size menuSize = menu.GetPreferredSize(Size.Empty);
                menu.Size = new Size(Math.Min(_mainForm.ClientSize.Width, menuSize.Width), menuSize.Height);
                menu.PerformLayout();
                Size[] sizes = _statusControls.Select(control => control is Label label
                    ? (string.IsNullOrEmpty(label.Text) ? Size.Empty : label.GetPreferredSize(Size.Empty))
                    : control.Size).ToArray();
                int rowHeight = Math.Max(menu.Height, Math.Max(_mainForm.runCodePb.Height, sizes.Max(size => size.Height)) + 2 * inset);
                menu.Location = new Point(0, (rowHeight - menu.Height) / 2);
                int menuRight = menu.Items.Cast<ToolStripItem>().Where(item => item.Available)
                    .Select(item => item.Bounds.Right + menu.Left).DefaultIfEmpty(menu.Left).Max();
                int left = menuRight + gap;
                foreach (var control in new Control[] { _mainForm.label2, _mainForm.runCodePb, _mainForm.label3 })
                {
                    control.Location = new Point(left, Math.Max(0, (rowHeight - control.Height) / 2));
                    left = control.Right + inset;
                }
                if (_mainForm.markStartFileChk.Visible)
                {
                    _mainForm.markStartFileChk.Location = new Point(left + gap,
                        Math.Max(0, (rowHeight - _mainForm.markStartFileChk.Height) / 2));
                    left = _mainForm.markStartFileChk.Right + gap;
                }

                int requiredWidth = sizes.Where(size => size.Width > 0).Sum(size => size.Width + gap) + gap;
                bool belowMenu = requiredWidth > _mainForm.ClientSize.Width - left - gap;
                int width = Math.Max(1, belowMenu ? _mainForm.ClientSize.Width : requiredWidth);
                int right = width - gap;
                int rowTop = 0;

                for (int index = 0; index < _statusControls.Length; index++)
                {
                    Size size = sizes[index];
                    int controlWidth = Math.Min(size.Width, Math.Max(0, width - 2 * gap));
                    if (controlWidth > 0 && right - controlWidth < gap && right < width - gap)
                    {
                        rowTop += rowHeight;
                        right = width - gap;
                    }
                    _statusControls[index].Bounds = new Rectangle(Math.Max(gap, right - controlWidth),
                        rowTop + Math.Max(0, (rowHeight - size.Height) / 2), controlWidth, size.Height);
                    if (controlWidth > 0)
                        right -= controlWidth + gap;
                }

                _statusPanel.Bounds = new Rectangle(belowMenu ? 0 : _mainForm.ClientSize.Width - width,
                    belowMenu ? rowHeight : 0, width, rowTop + rowHeight);
                _headerPanel.Height = _statusPanel.Bottom;
            }
            finally
            {
                _mainForm.ResumeLayout(true);
                _refreshing = false;
            }
        }
    }
}
