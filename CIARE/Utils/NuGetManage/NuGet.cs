using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Roslyn;
using CIARE.Utils;
using CIARE.Utils.NuGetManage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CIARE
{
    public partial class MainForm
    {
        private System.Windows.Forms.Timer _fileExplorerNuGetRefreshTimer;
        private string _pendingProjectPackageRefreshPath = string.Empty;
        private bool _pendingProjectPackageRestore;
        private bool _pendingProjectPackageShowRestoreFailure;
        private int _projectPackageRefreshVersion;
        private int _fileExplorerNuGetListRefreshVersion;

        public void RefreshExplorerNuGetPackages()
        {
            if (_fileExplorerNuGetList == null || _fileExplorerNuGetList.IsDisposed)
                return;

            if (InvokeRequired)
            {
                TryBeginInvoke(RefreshExplorerNuGetPackages);
                return;
            }

            string projectPath = GetActivePackageProjectPath();
            int refreshVersion = Interlocked.Increment(ref _fileExplorerNuGetListRefreshVersion);
            List<ProjectNuGetPackageReference> packages = null;
            _fileExplorerNuGetList.BeginUpdate();
            try
            {
                _fileExplorerNuGetList.Items.Clear();
                if (string.IsNullOrEmpty(projectPath))
                {
                    _fileExplorerNuGetTitleLabel.Text = "NuGet packages";
                    toolTip1.SetToolTip(_fileExplorerNuGetTitleLabel, "Open a project folder or select a .csproj file");
                    AddFileExplorerNuGetPlaceholder("No project selected");
                    return;
                }

                _fileExplorerNuGetTitleLabel.Text = $"NuGet: {Path.GetFileName(projectPath)}";
                toolTip1.SetToolTip(_fileExplorerNuGetTitleLabel, projectPath);

                packages = ProjectNuGetManager.GetPackageReferences(projectPath);
                if (packages.Count == 0)
                {
                    AddFileExplorerNuGetPlaceholder("No PackageReference items");
                    return;
                }

                foreach (var package in packages)
                {
                    var item = new ListViewItem(new[]
                    {
                        package.Name,
                        package.Version,
                        "Checking...",
                        "Checking..."
                    })
                    {
                        Tag = package,
                        ToolTipText = FormatExplorerNuGetToolTip(package)
                    };
                    _fileExplorerNuGetList.Items.Add(item);
                }
            }
            finally
            {
                _fileExplorerNuGetList.EndUpdate();
                ResizeFileExplorerNuGetColumns();
            }

            if (packages != null && packages.Count > 0)
                ScheduleExplorerNuGetPackageMetadataRefresh(projectPath, packages, refreshVersion);
        }

        public void RefreshProjectPackageContext(string projectPath, bool restoreProject,
            bool showRestoreFailure = true)
        {
            if (InvokeRequired)
            {
                TryBeginInvoke(() => RefreshProjectPackageContext(projectPath, restoreProject, showRestoreFailure));
                return;
            }

            int refreshVersion = Interlocked.Increment(ref _projectPackageRefreshVersion);
            bool useCompletionReferences = ShouldUseProjectPackageCompletionReferences(projectPath);
            RefreshExplorerNuGetPackages();

            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
            {
                RealTimeChecker.InvalidateReferenceCache();
                ClearProjectPackageCompletionReferences();
                ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
                return;
            }

            RealTimeChecker.InvalidateReferenceCache();
            if (!useCompletionReferences)
                ClearProjectPackageCompletionReferences();

            Task.Run(() =>
            {
                ProcessRunResult restoreResult = null;
                if (restoreProject)
                    restoreResult = ProjectNuGetManager.RestoreProject(projectPath);

                RealTimeChecker.InvalidateReferenceCache();
                if (useCompletionReferences &&
                    refreshVersion == Volatile.Read(ref _projectPackageRefreshVersion))
                {
                    RefreshProjectPackageCompletionReferences(projectPath);
                }
                return restoreResult;
            }).ContinueWith(task =>
            {
                if (IsDisposed || !IsHandleCreated || refreshVersion != _projectPackageRefreshVersion)
                    return;

                TryBeginInvoke(() =>
                {
                    if (refreshVersion != _projectPackageRefreshVersion)
                        return;

                    RefreshExplorerNuGetPackages();
                    ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());

                    ProcessRunResult restoreResult = null;
                    if (task.Status == TaskStatus.RanToCompletion)
                        restoreResult = task.Result;
                    else if (task.Exception != null)
                        restoreResult = new ProcessRunResult(-1,
                            task.Exception.GetBaseException()?.Message ?? task.Exception.Message);

                    if (showRestoreFailure && restoreResult != null && !restoreResult.Success)
                    {
                        MessageBox.Show(FormatProjectRestoreFailure(restoreResult.Output),
                            "NuGet restore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                });
            });
        }

        public void RefreshStandaloneReferenceContext()
        {
            RealTimeChecker.InvalidateReferenceCache();

            if (InvokeRequired)
            {
                TryBeginInvoke(RefreshStandaloneReferenceContext);
                return;
            }

            ReloadRef();
            ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
        }

        private static string FormatProjectRestoreFailure(string restoreOutput)
        {
            string output = (restoreOutput ?? string.Empty).Trim();
            if (output.Length > 1800)
                output = output.Substring(0, 1800) + Environment.NewLine + "...";

            return string.IsNullOrWhiteSpace(output)
                ? "NuGet restore failed. Project references may be stale until restore succeeds."
                : "NuGet restore failed. Project references may be stale until restore succeeds." +
                  Environment.NewLine + Environment.NewLine + output;
        }

        private void AddFileExplorerNuGetPlaceholder(string text)
        {
            var item = new ListViewItem(new[] { text, string.Empty, string.Empty, string.Empty })
            {
                ForeColor = GlobalVariables.darkColor ? Color.FromArgb(150, 170, 165) : SystemColors.GrayText
            };
            _fileExplorerNuGetList.Items.Add(item);
        }

        private void ResizeFileExplorerNuGetColumns()
        {
            if (_fileExplorerNuGetList == null ||
                _fileExplorerNuGetList.IsDisposed ||
                _fileExplorerNuGetList.Columns.Count < 4)
            {
                return;
            }

            int availableWidth = _fileExplorerNuGetList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4;
            if (availableWidth <= 0)
                return;

            int versionWidth = Math.Max(64, Math.Min(86, availableWidth / 5));
            int updateWidth = Math.Max(72, Math.Min(96, availableWidth / 4));
            int statusWidth = Math.Max(62, Math.Min(76, availableWidth / 5));
            int packageWidth = Math.Max(90, availableWidth - versionWidth - updateWidth - statusWidth);

            _fileExplorerNuGetVersionColumn.Width = versionWidth;
            _fileExplorerNuGetUpdateColumn.Width = updateWidth;
            _fileExplorerNuGetStatusColumn.Width = statusWidth;
            _fileExplorerNuGetPackageColumn.Width = packageWidth;
        }

        private void ScheduleExplorerNuGetRefresh()
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            TryBeginInvoke(() => ScheduleProjectPackageContextRefresh(GetActiveEditorPackageProjectPath(),
                restoreProject: false, showRestoreFailure: false));
        }

        private void ScheduleProjectPackageContextRefresh(string projectPath, bool restoreProject,
            bool showRestoreFailure = true)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            if (InvokeRequired)
            {
                TryBeginInvoke(() => ScheduleProjectPackageContextRefresh(projectPath, restoreProject,
                    showRestoreFailure));
                return;
            }

            _pendingProjectPackageRefreshPath = projectPath;
            _pendingProjectPackageRestore = _pendingProjectPackageRestore || restoreProject;
            _pendingProjectPackageShowRestoreFailure =
                _pendingProjectPackageShowRestoreFailure || showRestoreFailure;

            if (_fileExplorerNuGetRefreshTimer == null)
            {
                _fileExplorerNuGetRefreshTimer = new System.Windows.Forms.Timer { Interval = 300 };
                _fileExplorerNuGetRefreshTimer.Tick += OnFileExplorerNuGetRefreshTimer;
            }

            _fileExplorerNuGetRefreshTimer.Stop();
            _fileExplorerNuGetRefreshTimer.Start();
        }

        private void OnFileExplorerNuGetRefreshTimer(object sender, EventArgs e)
        {
            _fileExplorerNuGetRefreshTimer?.Stop();

            string projectPath = _pendingProjectPackageRefreshPath;
            bool restoreProject = _pendingProjectPackageRestore;
            bool showRestoreFailure = _pendingProjectPackageShowRestoreFailure;

            _pendingProjectPackageRefreshPath = string.Empty;
            _pendingProjectPackageRestore = false;
            _pendingProjectPackageShowRestoreFailure = false;

            RefreshProjectPackageContext(projectPath, restoreProject, showRestoreFailure);
        }

        private static bool ShouldRefreshExplorerNuGetPackages(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            return string.Equals(Path.GetExtension(path), ".csproj", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(path), "Directory.Packages.props", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(path), "project.assets.json", StringComparison.OrdinalIgnoreCase);
        }

        private void fileExplorerNuGetList_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || _fileExplorerNuGetList == null)
                return;

            ListViewHitTestInfo hit = _fileExplorerNuGetList.HitTest(e.Location);
            if (!(hit.Item?.Tag is ProjectNuGetPackageReference package))
                return;

            hit.Item.Selected = true;
            _fileExplorerNuGetUpdateMenuItem.Tag = package;
            _fileExplorerNuGetUpdateMenuItem.Enabled = package.HasUpdate &&
                !string.IsNullOrWhiteSpace(package.LatestVersion);
            _fileExplorerNuGetUpdateMenuItem.Text = _fileExplorerNuGetUpdateMenuItem.Enabled
                ? $"Update {package.Name} to {package.LatestVersion}"
                : "No update available";

            _fileExplorerNuGetRemoveMenuItem.Tag = package;
            _fileExplorerNuGetRemoveMenuItem.Text = $"Remove {package.Name} from Project";
            _fileExplorerNuGetContextMenu.Show(_fileExplorerNuGetList, e.Location);
        }

        private void fileExplorerNuGetUpdateMenuItem_Click(object sender, EventArgs e)
        {
            if (!(_fileExplorerNuGetUpdateMenuItem.Tag is ProjectNuGetPackageReference package) ||
                !package.HasUpdate ||
                string.IsNullOrWhiteSpace(package.LatestVersion))
            {
                return;
            }

            DialogResult dialog = MessageBox.Show(
                $"Update NuGet package {package.Name} from {package.Version} to {package.LatestVersion}?",
                "CIARE", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialog != DialogResult.Yes)
                return;

            if (!ProjectNuGetManager.UpdatePackageReference(package.ProjectPath, package.Name,
                    package.LatestVersion, out string message))
            {
                MessageBox.Show(message, "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RefreshProjectPackageContext(package.ProjectPath, restoreProject: true);
            MessageBox.Show(message, "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void fileExplorerNuGetRemoveMenuItem_Click(object sender, EventArgs e)
        {
            if (!(_fileExplorerNuGetRemoveMenuItem.Tag is ProjectNuGetPackageReference package))
                return;

            DialogResult dialog = MessageBox.Show(
                $"Remove NuGet package {package.Name} from {Path.GetFileName(package.ProjectPath)}?",
                "CIARE", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialog != DialogResult.Yes)
                return;

            if (!ProjectNuGetManager.RemovePackageReference(package.ProjectPath, package.Name, out string message))
            {
                MessageBox.Show(message, "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RefreshProjectPackageContext(package.ProjectPath, restoreProject: true);
            MessageBox.Show(message, "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
