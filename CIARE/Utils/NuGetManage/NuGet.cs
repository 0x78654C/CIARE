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
using static global::CIARE.Utils.NuGetManage.NuGetMetadata;

namespace CIARE.Utils.NuGetManage
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class NuGet
    {
        private readonly MainForm _mainForm;

        internal NuGet(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        internal System.Windows.Forms.Timer _fileExplorerNuGetRefreshTimer;
        internal string _pendingProjectPackageRefreshPath = string.Empty;
        internal bool _pendingProjectPackageRestore;
        internal bool _pendingProjectPackageShowRestoreFailure;
        internal int _projectPackageRefreshVersion;
        internal int _fileExplorerNuGetListRefreshVersion;

        public void RefreshExplorerNuGetPackages()
        {
            if (_mainForm.ExplorerFeature._fileExplorerNuGetList == null || _mainForm.ExplorerFeature._fileExplorerNuGetList.IsDisposed)
                return;

            if (_mainForm.InvokeRequired)
            {
                _mainForm.TryBeginInvoke(RefreshExplorerNuGetPackages);
                return;
            }

            string projectPath = _mainForm.ProjectContextFeature.GetActivePackageProjectPath();
            int refreshVersion = Interlocked.Increment(ref _fileExplorerNuGetListRefreshVersion);
            List<ProjectNuGetPackageReference> packages = null;
            _mainForm.ExplorerFeature._fileExplorerNuGetList.BeginUpdate();
            try
            {
                _mainForm.ExplorerFeature._fileExplorerNuGetList.Items.Clear();
                if (string.IsNullOrEmpty(projectPath))
                {
                    _mainForm.ExplorerFeature._fileExplorerNuGetTitleLabel.Text = "NuGet packages";
                    _mainForm.toolTip1.SetToolTip(_mainForm.ExplorerFeature._fileExplorerNuGetTitleLabel, "Open a project folder or select a .csproj file");
                    AddFileExplorerNuGetPlaceholder("No project selected");
                    return;
                }

                _mainForm.ExplorerFeature._fileExplorerNuGetTitleLabel.Text = $"NuGet: {Path.GetFileName(projectPath)}";
                _mainForm.toolTip1.SetToolTip(_mainForm.ExplorerFeature._fileExplorerNuGetTitleLabel, projectPath);

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
                    _mainForm.ExplorerFeature._fileExplorerNuGetList.Items.Add(item);
                }
            }
            finally
            {
                _mainForm.ExplorerFeature._fileExplorerNuGetList.EndUpdate();
                ResizeFileExplorerNuGetColumns();
            }

            if (packages != null && packages.Count > 0)
                _mainForm.NuGetMetadataFeature.ScheduleExplorerNuGetPackageMetadataRefresh(projectPath, packages, refreshVersion);
        }

        public void RefreshProjectPackageContext(string projectPath, bool restoreProject,
            bool showRestoreFailure = true)
        {
            if (_mainForm.InvokeRequired)
            {
                _mainForm.TryBeginInvoke(() => RefreshProjectPackageContext(projectPath, restoreProject, showRestoreFailure));
                return;
            }

            int refreshVersion = Interlocked.Increment(ref _projectPackageRefreshVersion);
            bool useCompletionReferences = _mainForm.CompletionReferencesFeature.ShouldUseProjectPackageCompletionReferences(projectPath);
            RefreshExplorerNuGetPackages();

            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
            {
                RealTimeChecker.InvalidateReferenceCache();
                _mainForm.CompletionReferencesFeature.ClearProjectPackageCompletionReferences();
                _mainForm.EditorFeature.ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
                return;
            }

            RealTimeChecker.InvalidateReferenceCache();
            if (!useCompletionReferences)
                _mainForm.CompletionReferencesFeature.ClearProjectPackageCompletionReferences();

            Task.Run(() =>
            {
                ProcessRunResult restoreResult = null;
                if (restoreProject)
                    restoreResult = ProjectNuGetManager.RestoreProject(projectPath);

                RealTimeChecker.InvalidateReferenceCache();
                if (useCompletionReferences &&
                    refreshVersion == Volatile.Read(ref _projectPackageRefreshVersion))
                {
                    _mainForm.CompletionReferencesFeature.RefreshProjectPackageCompletionReferences(projectPath);
                }
                return restoreResult;
            }).ContinueWith(task =>
            {
                if (_mainForm.IsDisposed || !_mainForm.IsHandleCreated || refreshVersion != _projectPackageRefreshVersion)
                    return;

                _mainForm.TryBeginInvoke(() =>
                {
                    if (refreshVersion != _projectPackageRefreshVersion)
                        return;

                    RefreshExplorerNuGetPackages();
                    _mainForm.EditorFeature.ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());

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

            if (_mainForm.InvokeRequired)
            {
                _mainForm.TryBeginInvoke(RefreshStandaloneReferenceContext);
                return;
            }

            _mainForm.CompletionParsingFeature.ReloadRef();
            _mainForm.EditorFeature.ScheduleCurrentTypeCheck(SelectedEditor.GetSelectedEditor());
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
            _mainForm.ExplorerFeature._fileExplorerNuGetList.Items.Add(item);
        }

        internal void ResizeFileExplorerNuGetColumns()
        {
            if (_mainForm.ExplorerFeature._fileExplorerNuGetList == null ||
                _mainForm.ExplorerFeature._fileExplorerNuGetList.IsDisposed ||
                _mainForm.ExplorerFeature._fileExplorerNuGetList.Columns.Count < 4)
            {
                return;
            }

            int availableWidth = _mainForm.ExplorerFeature._fileExplorerNuGetList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4;
            if (availableWidth <= 0)
                return;

            int versionWidth = Math.Max(64, Math.Min(86, availableWidth / 5));
            int updateWidth = Math.Max(72, Math.Min(96, availableWidth / 4));
            int statusWidth = Math.Max(62, Math.Min(76, availableWidth / 5));
            int packageWidth = Math.Max(90, availableWidth - versionWidth - updateWidth - statusWidth);

            _mainForm.ExplorerFeature._fileExplorerNuGetVersionColumn.Width = versionWidth;
            _mainForm.ExplorerFeature._fileExplorerNuGetUpdateColumn.Width = updateWidth;
            _mainForm.ExplorerFeature._fileExplorerNuGetStatusColumn.Width = statusWidth;
            _mainForm.ExplorerFeature._fileExplorerNuGetPackageColumn.Width = packageWidth;
        }

        internal void ScheduleExplorerNuGetRefresh()
        {
            if (_mainForm.IsDisposed || !_mainForm.IsHandleCreated)
                return;

            _mainForm.TryBeginInvoke(() => ScheduleProjectPackageContextRefresh(_mainForm.ProjectContextFeature.GetActiveEditorPackageProjectPath(),
                restoreProject: false, showRestoreFailure: false));
        }

        private void ScheduleProjectPackageContextRefresh(string projectPath, bool restoreProject,
            bool showRestoreFailure = true)
        {
            if (_mainForm.IsDisposed || !_mainForm.IsHandleCreated)
                return;

            if (_mainForm.InvokeRequired)
            {
                _mainForm.TryBeginInvoke(() => ScheduleProjectPackageContextRefresh(projectPath, restoreProject,
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

        internal void OnFileExplorerNuGetRefreshTimer(object sender, EventArgs e)
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

        internal static bool ShouldRefreshExplorerNuGetPackages(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            return string.Equals(Path.GetExtension(path), ".csproj", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(path), "Directory.Packages.props", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(path), "project.assets.json", StringComparison.OrdinalIgnoreCase);
        }

        internal void fileExplorerNuGetList_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || _mainForm.ExplorerFeature._fileExplorerNuGetList == null)
                return;

            ListViewHitTestInfo hit = _mainForm.ExplorerFeature._fileExplorerNuGetList.HitTest(e.Location);
            if (!(hit.Item?.Tag is ProjectNuGetPackageReference package))
                return;

            hit.Item.Selected = true;
            _mainForm.ExplorerFeature._fileExplorerNuGetUpdateMenuItem.Tag = package;
            _mainForm.ExplorerFeature._fileExplorerNuGetUpdateMenuItem.Enabled = package.HasUpdate &&
                !string.IsNullOrWhiteSpace(package.LatestVersion);
            _mainForm.ExplorerFeature._fileExplorerNuGetUpdateMenuItem.Text = _mainForm.ExplorerFeature._fileExplorerNuGetUpdateMenuItem.Enabled
                ? $"Update {package.Name} to {package.LatestVersion}"
                : "No update available";

            _mainForm.ExplorerFeature._fileExplorerNuGetRemoveMenuItem.Tag = package;
            _mainForm.ExplorerFeature._fileExplorerNuGetRemoveMenuItem.Text = $"Remove {package.Name} from Project";
            _mainForm.ExplorerFeature._fileExplorerNuGetContextMenu.Show(_mainForm.ExplorerFeature._fileExplorerNuGetList, e.Location);
        }

        internal void fileExplorerNuGetUpdateMenuItem_Click(object sender, EventArgs e)
        {
            if (!(_mainForm.ExplorerFeature._fileExplorerNuGetUpdateMenuItem.Tag is ProjectNuGetPackageReference package) ||
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

        internal void fileExplorerNuGetRemoveMenuItem_Click(object sender, EventArgs e)
        {
            if (!(_mainForm.ExplorerFeature._fileExplorerNuGetRemoveMenuItem.Tag is ProjectNuGetPackageReference package))
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
