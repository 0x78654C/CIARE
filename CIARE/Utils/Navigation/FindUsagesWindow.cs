using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using Button = System.Windows.Forms.Button;

namespace CIARE
{
    public partial class MainForm
    {
        private Form _findUsagesWindow;

        private void ShowFindUsagesResults(string identifier, List<UsageLocation> usages)
        {
            CloseFindUsagesWindow();

            var form = new Form
            {
                Text = "Find Usages - " + identifier,
                Icon = this.Icon,
                Size = new Size(900, 480),
                MinimumSize = new Size(640, 320),
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                ShowIcon = true,
                KeyPreview = true,
            };
            form.Location = new Point(
                Math.Max(0, Left + 80),
                Math.Max(0, Top + 80));

            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(10, 9, 10, 0),
                Text = $"Usages of '{identifier}' ({usages.Count})"
            };

            var list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                MultiSelect = false,
                ShowItemToolTips = true,
                Activation = ItemActivation.Standard
            };
            list.Columns.Add("File", 340);
            list.Columns.Add("Line", 70);
            list.Columns.Add("Column", 70);
            list.Columns.Add("Code", 400);

            string workspaceFolder = GetUsageWorkspaceFolder(GetActiveEditorFilePath());
            bool hasWorkspaceFolder = Directory.Exists(workspaceFolder);
            var usageItems = new List<ListViewItem>(Math.Max(1, usages.Count));
            foreach (var usage in usages)
            {
                var item = new ListViewItem(GetUsageWindowDisplayPath(usage.FilePath, workspaceFolder, hasWorkspaceFolder))
                {
                    Tag = usage,
                    ToolTipText = usage.FilePath
                };
                item.SubItems.Add(usage.Line.ToString());
                item.SubItems.Add(usage.Column.ToString());
                item.SubItems.Add(usage.Text);
                usageItems.Add(item);
            }

            if (usages.Count == 0)
                usageItems.Add(new ListViewItem(new[] { "No usages found.", string.Empty, string.Empty, string.Empty }));

            list.BeginUpdate();
            try
            {
                list.Items.AddRange(usageItems.ToArray());
            }
            finally
            {
                list.EndUpdate();
            }

            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8),
                WrapContents = false
            };
            var closeButton = new Button
            {
                Text = "Close",
                Width = 90,
                Height = 26
            };
            var openButton = new Button
            {
                Text = "Open",
                Width = 90,
                Height = 26,
                Enabled = usages.Count > 0
            };
            footer.Controls.Add(closeButton);
            footer.Controls.Add(openButton);

            void OpenSelectedUsage()
            {
                if (list.SelectedItems.Count == 0)
                    return;

                if (list.SelectedItems[0].Tag is UsageLocation usage)
                    NavigateToUsageLocation(usage.FilePath, usage.Line, usage.Column);
            }

            openButton.Click += (sender, e) => OpenSelectedUsage();
            closeButton.Click += (sender, e) => form.Close();
            list.ItemActivate += (sender, e) => OpenSelectedUsage();
            list.SelectedIndexChanged += (sender, e) =>
                openButton.Enabled = list.SelectedItems.Count > 0 && list.SelectedItems[0].Tag is UsageLocation;
            list.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    OpenSelectedUsage();
                    e.Handled = true;
                }
            };
            form.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    form.Close();
            };
            list.Resize += (sender, e) => ResizeFindUsagesColumns(list);

            ApplyFindUsagesWindowTheme(form, header, list, footer, openButton, closeButton);
            form.HandleCreated += (sender, e) => ApplyFindUsagesDarkTitleBar(form);

            form.Controls.Add(list);
            form.Controls.Add(footer);
            form.Controls.Add(header);
            form.Shown += (sender, e) =>
            {
                ApplyFindUsagesDarkTitleBar(form);
                ResizeFindUsagesColumns(list);
                if (list.Items.Count > 0 && list.Items[0].Tag is UsageLocation)
                    list.Items[0].Selected = true;
                list.Focus();
            };
            form.FormClosed += (sender, e) =>
            {
                if (ReferenceEquals(_findUsagesWindow, form))
                    _findUsagesWindow = null;
            };

            _findUsagesWindow = form;
            form.Show(this);
        }

        private void ShowFindUsagesMessage(string message)
        {
            MessageBox.Show(this, message, "Find Usages", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CloseFindUsagesWindow()
        {
            if (_findUsagesWindow != null && !_findUsagesWindow.IsDisposed)
                _findUsagesWindow.Close();
            _findUsagesWindow = null;
        }

        private string GetUsageWindowDisplayPath(string filePath)
        {
            string workspaceFolder = GetUsageWorkspaceFolder(GetActiveEditorFilePath());
            return GetUsageWindowDisplayPath(filePath, workspaceFolder, Directory.Exists(workspaceFolder));
        }

        private static string GetUsageWindowDisplayPath(string filePath, string workspaceFolder, bool hasWorkspaceFolder)
        {
            if (string.IsNullOrEmpty(filePath) || string.Equals(filePath, CurrentFileUsageDisplayName, StringComparison.Ordinal))
                return CurrentFileUsageDisplayName;

            try
            {
                if (hasWorkspaceFolder && IsPathInsideFolder(filePath, workspaceFolder))
                    return Path.GetRelativePath(workspaceFolder, filePath);
            }
            catch { }

            return filePath;
        }

        private static void ResizeFindUsagesColumns(ListView list)
        {
            if (list == null || list.Columns.Count < 4)
                return;

            int width = Math.Max(480, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 6);
            int lineWidth = 70;
            int columnWidth = 70;
            int fileWidth = Math.Max(220, width * 40 / 100);
            int codeWidth = Math.Max(180, width - fileWidth - lineWidth - columnWidth);

            list.Columns[0].Width = fileWidth;
            list.Columns[1].Width = lineWidth;
            list.Columns[2].Width = columnWidth;
            list.Columns[3].Width = codeWidth;
        }

        private static void ApplyFindUsagesWindowTheme(Form form, Label header, ListView list,
            FlowLayoutPanel footer, params Button[] buttons)
        {
            if (!GlobalVariables.darkColor)
                return;

            Color background = GlobalVariables.controlBgColor;
            Color foreground = Color.FromArgb(192, 215, 207);
            form.BackColor = background;
            form.ForeColor = foreground;
            header.BackColor = background;
            header.ForeColor = foreground;
            list.BackColor = Color.FromArgb(30, 30, 30);
            list.ForeColor = foreground;
            footer.BackColor = background;

            foreach (var button in buttons)
            {
                button.BackColor = Color.FromArgb(45, 45, 48);
                button.ForeColor = foreground;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85);
            }
        }

        private static void ApplyFindUsagesDarkTitleBar(Form form)
        {
            if (!GlobalVariables.darkColor || form == null || form.IsDisposed)
                return;

            try
            {
                FrmColorMod.EnableDarkTitleBar(form.Handle);
            }
            catch
            {
                // The usages window should still work if DWM dark title bars are unavailable.
            }
        }
    }
}
