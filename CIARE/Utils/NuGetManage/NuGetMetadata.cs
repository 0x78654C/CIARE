using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIARE.Utils;
using CIARE.Utils.NuGetManage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CIARE.Utils.NuGetManage
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class NuGetMetadata
    {
        private readonly MainForm _mainForm;

        internal NuGetMetadata(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        internal void ScheduleExplorerNuGetPackageMetadataRefresh(string projectPath,
            List<ProjectNuGetPackageReference> packages, int refreshVersion)
        {
            var usageSnapshot = CreateExplorerNuGetPackageSnapshot(packages);
            Task.Run(() =>
            {
                try
                {
                    ProjectNuGetManager.PopulateUnusedPackageStatus(projectPath, usageSnapshot);
                }
                catch
                {
                    foreach (var package in usageSnapshot)
                        package.UnusedCheckCompleted = true;
                }

                return usageSnapshot;
            }).ContinueWith(task =>
            {
                if (_mainForm.IsDisposed ||
                    !_mainForm.IsHandleCreated ||
                    refreshVersion != _mainForm.NuGetFeature._fileExplorerNuGetListRefreshVersion)
                {
                    return;
                }

                var result = task.Status == TaskStatus.RanToCompletion ? task.Result : usageSnapshot;
                _mainForm.TryBeginInvoke(() => ApplyExplorerNuGetPackageMetadata(projectPath, result,
                    refreshVersion));
            });

            var updateSnapshot = CreateExplorerNuGetPackageSnapshot(packages);
            Task.Run(() =>
            {
                ProjectNuGetManager.PopulateLatestPackageVersions(updateSnapshot);
                return updateSnapshot;
            }).ContinueWith(task =>
            {
                if (task.Status != TaskStatus.RanToCompletion ||
                    _mainForm.IsDisposed ||
                    !_mainForm.IsHandleCreated ||
                    refreshVersion != _mainForm.NuGetFeature._fileExplorerNuGetListRefreshVersion)
                {
                    return;
                }

                _mainForm.TryBeginInvoke(() => ApplyExplorerNuGetPackageUpdateMetadata(projectPath, task.Result,
                    refreshVersion));
            });
        }

        private static List<ProjectNuGetPackageReference> CreateExplorerNuGetPackageSnapshot(
            IEnumerable<ProjectNuGetPackageReference> packages)
        {
            return packages
                .Select(package => new ProjectNuGetPackageReference
                {
                    Name = package.Name,
                    Version = package.Version,
                    ProjectPath = package.ProjectPath
                })
                .ToList();
        }

        private void ApplyExplorerNuGetPackageMetadata(string projectPath,
            List<ProjectNuGetPackageReference> packages, int refreshVersion)
        {
            if (refreshVersion != _mainForm.NuGetFeature._fileExplorerNuGetListRefreshVersion ||
                !string.Equals(_mainForm.ProjectContextFeature.GetActivePackageProjectPath(), projectPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var metadataByName = packages
                .GroupBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            _mainForm.ExplorerFeature._fileExplorerNuGetList.BeginUpdate();
            try
            {
                foreach (ListViewItem item in _mainForm.ExplorerFeature._fileExplorerNuGetList.Items)
                {
                    if (!(item.Tag is ProjectNuGetPackageReference package) ||
                        !metadataByName.TryGetValue(package.Name, out var metadata))
                    {
                        continue;
                    }

                    package.UnusedCheckCompleted = metadata.UnusedCheckCompleted;
                    package.IsUnused = metadata.IsUnused;

                    EnsureExplorerNuGetSubItemCount(item);
                    item.SubItems[2].Text = FormatExplorerNuGetUpdateText(package);
                    item.SubItems[3].Text = FormatExplorerNuGetStatusText(package);
                    item.ToolTipText = FormatExplorerNuGetToolTip(package);
                    item.ForeColor = package.UnusedCheckCompleted && package.IsUnused
                        ? (GlobalVariables.darkColor ? Color.FromArgb(245, 174, 96) : Color.DarkOrange)
                        : _mainForm.ExplorerFeature._fileExplorerNuGetList.ForeColor;
                }
            }
            finally
            {
                _mainForm.ExplorerFeature._fileExplorerNuGetList.EndUpdate();
                _mainForm.NuGetFeature.ResizeFileExplorerNuGetColumns();
            }
        }

        private void ApplyExplorerNuGetPackageUpdateMetadata(string projectPath,
            List<ProjectNuGetPackageReference> packages, int refreshVersion)
        {
            if (refreshVersion != _mainForm.NuGetFeature._fileExplorerNuGetListRefreshVersion ||
                !string.Equals(_mainForm.ProjectContextFeature.GetActivePackageProjectPath(), projectPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var metadataByName = packages
                .GroupBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            _mainForm.ExplorerFeature._fileExplorerNuGetList.BeginUpdate();
            try
            {
                foreach (ListViewItem item in _mainForm.ExplorerFeature._fileExplorerNuGetList.Items)
                {
                    if (!(item.Tag is ProjectNuGetPackageReference package) ||
                        !metadataByName.TryGetValue(package.Name, out var metadata))
                    {
                        continue;
                    }

                    package.LatestVersion = metadata.LatestVersion;
                    package.HasUpdate = metadata.HasUpdate;

                    EnsureExplorerNuGetSubItemCount(item);
                    item.SubItems[2].Text = FormatExplorerNuGetUpdateText(package);
                    item.ToolTipText = FormatExplorerNuGetToolTip(package);
                }
            }
            finally
            {
                _mainForm.ExplorerFeature._fileExplorerNuGetList.EndUpdate();
                _mainForm.NuGetFeature.ResizeFileExplorerNuGetColumns();
            }
        }

        private static void EnsureExplorerNuGetSubItemCount(ListViewItem item)
        {
            while (item.SubItems.Count < 4)
                item.SubItems.Add(string.Empty);
        }

        private static string FormatExplorerNuGetUpdateText(ProjectNuGetPackageReference package)
        {
            if (package.HasUpdate)
                return package.LatestVersion;

            return string.IsNullOrWhiteSpace(package.LatestVersion) ? "Unknown" : "Current";
        }

        private static string FormatExplorerNuGetStatusText(ProjectNuGetPackageReference package)
        {
            if (!package.UnusedCheckCompleted)
                return "Checking...";

            return package.IsUnused ? "Unused" : "Used";
        }

        internal static string FormatExplorerNuGetToolTip(ProjectNuGetPackageReference package)
        {
            string updateStatus = package.HasUpdate
                ? $"Update available: {package.LatestVersion}"
                : (!string.IsNullOrWhiteSpace(package.LatestVersion) ? "No update available" : "Update unknown");
            string usageStatus = package.UnusedCheckCompleted
                ? (package.IsUnused ? "Marked unused by source scan" : "Used by source scan")
                : "Usage unknown";

            return $"{package.Name} {package.Version}{Environment.NewLine}{updateStatus}{Environment.NewLine}{usageStatus}";
        }
    }
}
