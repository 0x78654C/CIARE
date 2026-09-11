using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CIARE.Utils.Explorer
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class ExplorerLayout
    {
        private readonly MainForm _mainForm;

        internal ExplorerLayout(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        private const int FileExplorerDefaultWidth = 280;
        private const int FileExplorerMinWidth = 220;
        private const int EditorPaneMinWidth = 180;
        private const int FileExplorerDefaultNuGetHeight = 350;
        internal const int FileExplorerNuGetMinHeight = 90;
        private const int FileExplorerTreeMinHeight = 120;
        private const int FileExplorerLayoutApplyMaxAttempts = 20;
        private const int FileExplorerLayoutApplyInterval = 50;
        internal const string FileExplorerVisibleKey = "fileExplorerVisible";
        private const string FileExplorerWidthKey = "fileExplorerWidth";
        private const string FileExplorerNuGetHeightKey = "fileExplorerNuGetHeight";
        private static readonly string FileExplorerLayoutFilePath =
            Path.Combine(GlobalVariables.userProfileDirectory, "fileExplorerLayout.cDat");
        private int _fileExplorerWidth = FileExplorerDefaultWidth;
        private int _fileExplorerNuGetHeight = FileExplorerDefaultNuGetHeight;
        private bool _suppressFileExplorerLayoutSave;
        private bool _applyingFileExplorerLayout;
        private bool _pendingFileExplorerLayoutApply;
        private int _fileExplorerLayoutApplyAttempts;
        private System.Windows.Forms.Timer _fileExplorerLayoutApplyTimer;
        private bool _fileExplorerLayoutReadyForUserSave;
        internal bool _fileExplorerWidthDragInProgress;
        internal bool _fileExplorerNuGetHeightDragInProgress;

        internal void ToggleFileExplorer(bool show, bool saveWidth = true)
        {
            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer == null)
                return;

            _mainForm.ExplorerFeature._editorWorkspacePanel?.SuspendLayout();
            _mainForm.ExplorerFeature._editorExplorerSplitContainer.SuspendLayout();
            _mainForm.EditorTabControl.SuspendLayout();
            try
            {
                if (show)
                {
                    _mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2Collapsed = false;
                    ApplyEditorExplorerMinimumWidths();
                    ApplyFileExplorerLayoutValues();
                    QueueFileExplorerLayoutApply();
                    _mainForm.ExplorerFeature._fileExplorerShowButton.Visible = false;
                }
                else
                {
                    if (saveWidth)
                        SaveFileExplorerWidth(force: true);
                    _mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2Collapsed = true;
                    _mainForm.ExplorerFeature._fileExplorerShowButton.Visible = true;
                    PositionFileExplorerShowButton();
                    SelectedEditor.GetSelectedEditor()?.Focus();
                }

                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, FileExplorerVisibleKey, show.ToString());
            }
            finally
            {
                _mainForm.EditorTabControl.ResumeLayout(false);
                _mainForm.ExplorerFeature._editorExplorerSplitContainer.ResumeLayout(false);
                _mainForm.ExplorerFeature._editorWorkspacePanel?.ResumeLayout(false);
            }

            _mainForm.EditorLayoutFeature.QueueEditorLayoutRefresh();
        }

        internal void ToggleFileExplorer()
        {
            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer == null)
                return;

            ToggleFileExplorer(_mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2Collapsed);
        }

        private void SetFileExplorerWidth(int width)
        {
            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer == null)
                return;

            ApplyEditorExplorerMinimumWidths();

            int splitWidth = GetEditorExplorerSplitWidth();
            int availableWidth = splitWidth - _mainForm.ExplorerFeature._editorExplorerSplitContainer.SplitterWidth;
            if (availableWidth <= 0)
            {
                _mainForm.EditorLayoutFeature.QueueEditorLayoutRefresh();
                return;
            }

            int minDist = _mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel1MinSize;
            int maxDist = Math.Max(minDist, availableWidth - _mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2MinSize);
            int explorerWidth = GetClampedFileExplorerWidth(width);
            int dist = Math.Max(minDist, Math.Min(maxDist, availableWidth - explorerWidth));

            if (!_mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2Collapsed)
                _mainForm.ExplorerFeature._editorExplorerSplitContainer.SplitterDistance = dist;

            _mainForm.EditorLayoutFeature.QueueEditorLayoutRefresh();
        }

        private void SetFileExplorerNuGetHeight(int height)
        {
            if (_mainForm.ExplorerFeature._fileExplorerContentSplitContainer == null ||
                _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.IsDisposed ||
                _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.Panel2Collapsed)
            {
                return;
            }

            int splitHeight = _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.ClientSize.Height;
            int availableHeight = splitHeight - _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.SplitterWidth;
            if (availableHeight <= 0)
                return;

            int nuGetHeight = GetClampedFileExplorerNuGetHeight(height);
            _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.SplitterDistance = Math.Max(0, availableHeight - nuGetHeight);
        }

        private int GetClampedFileExplorerWidth(int width)
        {
            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer == null || _mainForm.ExplorerFeature._editorExplorerSplitContainer.IsDisposed)
                return width;

            int availableWidth = GetEditorExplorerSplitWidth() - _mainForm.ExplorerFeature._editorExplorerSplitContainer.SplitterWidth;
            if (availableWidth <= 0)
                return width;

            int minExplorerWidth = Math.Max(0, _mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2MinSize);
            int maxExplorerWidth = Math.Max(minExplorerWidth,
                availableWidth - Math.Max(0, _mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel1MinSize));
            return Math.Max(minExplorerWidth, Math.Min(width, maxExplorerWidth));
        }

        private int GetClampedFileExplorerNuGetHeight(int height)
        {
            if (_mainForm.ExplorerFeature._fileExplorerContentSplitContainer == null ||
                _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.IsDisposed)
            {
                return height;
            }

            int availableHeight = _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.ClientSize.Height -
                _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.SplitterWidth;
            if (availableHeight <= 0)
                return height;

            int minNuGetHeight = Math.Max(0, _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.Panel2MinSize);
            int maxNuGetHeight = Math.Max(minNuGetHeight, availableHeight - FileExplorerTreeMinHeight);
            return Math.Max(minNuGetHeight, Math.Min(height, maxNuGetHeight));
        }

        internal void LoadFileExplorerLayoutValues()
        {
            _fileExplorerWidth = ReadFileExplorerLayoutValue(FileExplorerWidthKey,
                FileExplorerDefaultWidth, FileExplorerMinWidth);
            _fileExplorerNuGetHeight = ReadFileExplorerLayoutValue(FileExplorerNuGetHeightKey,
                FileExplorerDefaultNuGetHeight, FileExplorerNuGetMinHeight);
        }

        private static int ReadFileExplorerLayoutValue(string key, int defaultValue, int minValue)
        {
            string savedFileValue = ReadFileExplorerLayoutFileValue(key);
            if (int.TryParse(savedFileValue, out int fileValue) && fileValue >= minValue)
                return fileValue;

            string savedValue = RegistryManagement.RegKey_Read(
                $"HKEY_CURRENT_USER\\{GlobalVariables.registryPath}", key);
            if (int.TryParse(savedValue, out int value) && value >= minValue)
                return value;

            return defaultValue;
        }

        internal void ApplyFileExplorerLayoutValues()
        {
            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer == null || _mainForm.ExplorerFeature._editorExplorerSplitContainer.IsDisposed)
                return;

            _suppressFileExplorerLayoutSave = true;
            _applyingFileExplorerLayout = true;
            try
            {
                _mainForm.ExplorerFeature._editorExplorerSplitContainer.SuspendLayout();
                _mainForm.ExplorerFeature._fileExplorerContentSplitContainer?.SuspendLayout();

                if (!_mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2Collapsed)
                    SetFileExplorerWidth(_fileExplorerWidth);
                SetFileExplorerNuGetHeight(_fileExplorerNuGetHeight);
            }
            finally
            {
                _mainForm.ExplorerFeature._fileExplorerContentSplitContainer?.ResumeLayout(true);
                _mainForm.ExplorerFeature._editorExplorerSplitContainer.ResumeLayout(true);
                _applyingFileExplorerLayout = false;
                _suppressFileExplorerLayoutSave = false;
            }
        }

        internal void QueueFileExplorerLayoutApply()
        {
            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer == null || _mainForm.ExplorerFeature._editorExplorerSplitContainer.IsDisposed || _mainForm.IsDisposed)
                return;

            _pendingFileExplorerLayoutApply = true;
            _fileExplorerLayoutApplyAttempts = 0;
            _fileExplorerLayoutReadyForUserSave = false;
            SchedulePendingFileExplorerLayoutApply();
        }

        internal void OnFileExplorerLayoutContainerSizeChanged()
        {
            if (_fileExplorerWidthDragInProgress ||
                _fileExplorerNuGetHeightDragInProgress ||
                _applyingFileExplorerLayout ||
                !_pendingFileExplorerLayoutApply ||
                _fileExplorerLayoutReadyForUserSave)
            {
                return;
            }

            SchedulePendingFileExplorerLayoutApply();
        }

        internal void CancelPendingFileExplorerLayoutApplyForUserResize()
        {
            _pendingFileExplorerLayoutApply = false;
            _fileExplorerLayoutReadyForUserSave = true;
            _fileExplorerLayoutApplyTimer?.Stop();
        }

        private void SchedulePendingFileExplorerLayoutApply()
        {
            if (!_mainForm.isLoaded || _mainForm.IsDisposed || !_mainForm.IsHandleCreated)
                return;

            EnsureFileExplorerLayoutApplyTimer();
            _fileExplorerLayoutApplyTimer.Interval =
                _fileExplorerLayoutApplyAttempts < FileExplorerLayoutApplyMaxAttempts
                    ? FileExplorerLayoutApplyInterval
                    : Math.Max(FileExplorerLayoutApplyInterval, 250);
            _fileExplorerLayoutApplyTimer.Stop();
            _fileExplorerLayoutApplyTimer.Start();
        }

        private void EnsureFileExplorerLayoutApplyTimer()
        {
            if (_fileExplorerLayoutApplyTimer != null)
                return;

            _fileExplorerLayoutApplyTimer = new System.Windows.Forms.Timer(_mainForm.components)
            {
                Interval = FileExplorerLayoutApplyInterval
            };
            _fileExplorerLayoutApplyTimer.Tick += OnFileExplorerLayoutApplyTimer;
        }

        private void OnFileExplorerLayoutApplyTimer(object sender, EventArgs e)
        {
            _fileExplorerLayoutApplyTimer?.Stop();
            ApplyPendingFileExplorerLayoutValues();
        }

        private void ApplyPendingFileExplorerLayoutValues()
        {
            if (!_pendingFileExplorerLayoutApply ||
                _mainForm.ExplorerFeature._editorExplorerSplitContainer == null ||
                _mainForm.ExplorerFeature._editorExplorerSplitContainer.IsDisposed)
            {
                return;
            }

            if (!CanApplyFileExplorerLayoutValues())
            {
                _fileExplorerLayoutApplyAttempts++;
                if (_fileExplorerLayoutApplyAttempts < FileExplorerLayoutApplyMaxAttempts)
                    SchedulePendingFileExplorerLayoutApply();
                return;
            }

            ApplyEditorExplorerMinimumWidths();
            if (!IsFileExplorerLayoutApplied())
                ApplyFileExplorerLayoutValues();
            PositionFileExplorerShowButton();
            _mainForm.EditorLayoutFeature.QueueEditorLayoutRefresh();

            _fileExplorerLayoutApplyAttempts++;
            if (!IsFileExplorerLayoutApplied())
            {
                if (_fileExplorerLayoutApplyAttempts < FileExplorerLayoutApplyMaxAttempts)
                {
                    SchedulePendingFileExplorerLayoutApply();
                    return;
                }
            }

            _pendingFileExplorerLayoutApply = false;
            _fileExplorerLayoutReadyForUserSave = true;
        }

        private bool CanApplyFileExplorerLayoutValues()
        {
            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer == null || _mainForm.ExplorerFeature._editorExplorerSplitContainer.IsDisposed)
                return false;

            if (!_mainForm.Visible || _mainForm.WindowState == FormWindowState.Minimized)
                return false;

            int splitWidth = GetEditorExplorerSplitWidth();
            if (splitWidth <= _mainForm.ExplorerFeature._editorExplorerSplitContainer.SplitterWidth)
                return false;

            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2Collapsed)
                return true;

            if (_mainForm.ExplorerFeature._fileExplorerContentSplitContainer == null || _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.IsDisposed)
                return true;

            int splitHeight = _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.ClientSize.Height;
            if (splitHeight <= _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.SplitterWidth)
                return false;

            return true;
        }

        private bool IsFileExplorerLayoutApplied()
        {
            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer == null || _mainForm.ExplorerFeature._editorExplorerSplitContainer.IsDisposed)
                return false;

            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2Collapsed)
                return true;

            if (!_mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2Collapsed)
            {
                int currentWidth = GetCurrentFileExplorerWidth();
                int targetWidth = GetClampedFileExplorerWidth(_fileExplorerWidth);
                if (currentWidth <= 0 || Math.Abs(currentWidth - targetWidth) > 1)
                    return false;
            }

            if (_mainForm.ExplorerFeature._fileExplorerContentSplitContainer == null ||
                _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.IsDisposed ||
                _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.Panel2Collapsed)
            {
                return true;
            }

            int currentHeight = GetCurrentFileExplorerNuGetHeight();
            int targetHeight = GetClampedFileExplorerNuGetHeight(_fileExplorerNuGetHeight);
            return currentHeight > 0 && Math.Abs(currentHeight - targetHeight) <= 1;
        }

        internal void QueueFileExplorerWidthSave()
        {
            if (_suppressFileExplorerLayoutSave || !_fileExplorerLayoutReadyForUserSave)
                return;

            SaveFileExplorerWidth();
        }

        internal void QueueFileExplorerNuGetHeightSave()
        {
            if (_suppressFileExplorerLayoutSave || !_fileExplorerLayoutReadyForUserSave)
                return;

            SaveFileExplorerNuGetHeight();
        }

        internal void SaveFileExplorerWidth(bool force = false)
        {
            if (!force && (_suppressFileExplorerLayoutSave || !_fileExplorerLayoutReadyForUserSave))
                return;

            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer == null ||
                _mainForm.ExplorerFeature._editorExplorerSplitContainer.IsDisposed ||
                _mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2Collapsed)
            {
                return;
            }

            int splitterWidth = GetCurrentFileExplorerWidth();
            if (splitterWidth <= 0)
                return;

            _fileExplorerWidth = Math.Max(FileExplorerMinWidth, splitterWidth);
            WriteFileExplorerLayoutValue(FileExplorerWidthKey, _fileExplorerWidth);
        }

        internal void SaveFileExplorerNuGetHeight(bool force = false)
        {
            if (!force && (_suppressFileExplorerLayoutSave || !_fileExplorerLayoutReadyForUserSave))
                return;

            if (_mainForm.ExplorerFeature._fileExplorerContentSplitContainer == null ||
                _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.IsDisposed ||
                _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.Panel2Collapsed)
            {
                return;
            }

            int splitterHeight = GetCurrentFileExplorerNuGetHeight();
            if (splitterHeight <= 0)
                return;

            _fileExplorerNuGetHeight = Math.Max(FileExplorerNuGetMinHeight, splitterHeight);
            WriteFileExplorerLayoutValue(FileExplorerNuGetHeightKey, _fileExplorerNuGetHeight);
        }

        private int GetCurrentFileExplorerWidth()
        {
            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer == null || _mainForm.ExplorerFeature._editorExplorerSplitContainer.IsDisposed)
                return 0;

            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2.Width > 0)
                return _mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2.Width;

            int availableWidth = GetEditorExplorerSplitWidth() - _mainForm.ExplorerFeature._editorExplorerSplitContainer.SplitterWidth;
            return availableWidth > 0
                ? Math.Max(0, availableWidth - _mainForm.ExplorerFeature._editorExplorerSplitContainer.SplitterDistance)
                : 0;
        }

        private int GetCurrentFileExplorerNuGetHeight()
        {
            if (_mainForm.ExplorerFeature._fileExplorerContentSplitContainer == null ||
                _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.IsDisposed)
            {
                return 0;
            }

            if (_mainForm.ExplorerFeature._fileExplorerContentSplitContainer.Panel2.Height > 0)
                return _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.Panel2.Height;

            int availableHeight = _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.ClientSize.Height -
                _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.SplitterWidth;
            return availableHeight > 0
                ? Math.Max(0, availableHeight - _mainForm.ExplorerFeature._fileExplorerContentSplitContainer.SplitterDistance)
                : 0;
        }

        private static void WriteFileExplorerLayoutValue(string key, int value)
        {
            InitializeEditor.SetCiareRegKey(GlobalVariables.registryPath, key, value.ToString());
            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, key, value.ToString());
            WriteFileExplorerLayoutFileValue(key, value);
        }

        private static string ReadFileExplorerLayoutFileValue(string key)
        {
            try
            {
                if (!File.Exists(FileExplorerLayoutFilePath))
                    return string.Empty;

                foreach (string line in File.ReadAllLines(FileExplorerLayoutFilePath))
                {
                    int separatorIndex = line.IndexOf('=');
                    if (separatorIndex <= 0)
                        continue;

                    string savedKey = line.Substring(0, separatorIndex).Trim();
                    if (string.Equals(savedKey, key, StringComparison.OrdinalIgnoreCase))
                        return line.Substring(separatorIndex + 1).Trim();
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static void WriteFileExplorerLayoutFileValue(string key, int value)
        {
            try
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(FileExplorerLayoutFilePath))
                {
                    foreach (string line in File.ReadAllLines(FileExplorerLayoutFilePath))
                    {
                        int separatorIndex = line.IndexOf('=');
                        if (separatorIndex <= 0)
                            continue;

                        values[line.Substring(0, separatorIndex).Trim()] =
                            line.Substring(separatorIndex + 1).Trim();
                    }
                }

                values[key] = value.ToString();
                Directory.CreateDirectory(GlobalVariables.userProfileDirectory);
                File.WriteAllLines(FileExplorerLayoutFilePath,
                    values.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(pair => pair.Key + "=" + pair.Value));
            }
            catch
            {
                // Layout state persistence is best-effort.
            }
        }

        private int GetEditorExplorerSplitWidth()
        {
            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer == null)
                return 0;

            return _mainForm.ExplorerFeature._editorExplorerSplitContainer.ClientSize.Width > 0
                ? _mainForm.ExplorerFeature._editorExplorerSplitContainer.ClientSize.Width
                : _mainForm.ExplorerFeature._editorExplorerSplitContainer.Width;
        }

        internal void ApplyEditorExplorerMinimumWidths()
        {
            if (_mainForm.ExplorerFeature._editorExplorerSplitContainer == null || _mainForm.ExplorerFeature._editorExplorerSplitContainer.IsDisposed)
                return;

            int splitWidth = GetEditorExplorerSplitWidth();
            int availableWidth = splitWidth - _mainForm.ExplorerFeature._editorExplorerSplitContainer.SplitterWidth;
            if (availableWidth <= 0)
                return;

            int panel1Min = EditorPaneMinWidth;
            int panel2Min = FileExplorerMinWidth;
            if (panel1Min + panel2Min > availableWidth)
            {
                panel1Min = Math.Min(EditorPaneMinWidth, Math.Max(0, availableWidth / 2));
                panel2Min = Math.Min(FileExplorerMinWidth, Math.Max(0, availableWidth - panel1Min));
            }

            int minDist = panel1Min;
            int maxDist = Math.Max(minDist, availableWidth - panel2Min);
            int currentDist = Math.Max(0, _mainForm.ExplorerFeature._editorExplorerSplitContainer.SplitterDistance);
            int safeDist = Math.Max(minDist, Math.Min(maxDist, currentDist));

            _mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel1MinSize = 0;
            _mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2MinSize = 0;

            if (!_mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2Collapsed)
                _mainForm.ExplorerFeature._editorExplorerSplitContainer.SplitterDistance = safeDist;

            _mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel1MinSize = panel1Min;
            _mainForm.ExplorerFeature._editorExplorerSplitContainer.Panel2MinSize = panel2Min;
        }

        internal void PositionFileExplorerShowButton()
        {
            if (_mainForm.ExplorerFeature._editorWorkspacePanel == null || _mainForm.ExplorerFeature._fileExplorerShowButton == null)
                return;

            _mainForm.ExplorerFeature._fileExplorerShowButton.Location = new Point(
                Math.Max(0, _mainForm.ExplorerFeature._editorWorkspacePanel.ClientSize.Width - _mainForm.ExplorerFeature._fileExplorerShowButton.Width
                    - SystemInformation.VerticalScrollBarWidth - 4),
                6);
            _mainForm.ExplorerFeature._fileExplorerShowButton.BringToFront();
        }
    }
}
