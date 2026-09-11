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

namespace CIARE
{
    public partial class MainForm
    {
        private const int FileExplorerDefaultWidth = 280;
        private const int FileExplorerMinWidth = 220;
        private const int EditorPaneMinWidth = 180;
        private const int FileExplorerDefaultNuGetHeight = 350;
        private const int FileExplorerNuGetMinHeight = 90;
        private const int FileExplorerTreeMinHeight = 120;
        private const int FileExplorerLayoutApplyMaxAttempts = 20;
        private const int FileExplorerLayoutApplyInterval = 50;
        private const string FileExplorerVisibleKey = "fileExplorerVisible";
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
        private bool _fileExplorerWidthDragInProgress;
        private bool _fileExplorerNuGetHeightDragInProgress;

        private void ToggleFileExplorer(bool show, bool saveWidth = true)
        {
            if (_editorExplorerSplitContainer == null)
                return;

            _editorWorkspacePanel?.SuspendLayout();
            _editorExplorerSplitContainer.SuspendLayout();
            EditorTabControl.SuspendLayout();
            try
            {
                if (show)
                {
                    _editorExplorerSplitContainer.Panel2Collapsed = false;
                    ApplyEditorExplorerMinimumWidths();
                    ApplyFileExplorerLayoutValues();
                    QueueFileExplorerLayoutApply();
                    _fileExplorerShowButton.Visible = false;
                }
                else
                {
                    if (saveWidth)
                        SaveFileExplorerWidth(force: true);
                    _editorExplorerSplitContainer.Panel2Collapsed = true;
                    _fileExplorerShowButton.Visible = true;
                    PositionFileExplorerShowButton();
                    SelectedEditor.GetSelectedEditor()?.Focus();
                }

                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, FileExplorerVisibleKey, show.ToString());
            }
            finally
            {
                EditorTabControl.ResumeLayout(false);
                _editorExplorerSplitContainer.ResumeLayout(false);
                _editorWorkspacePanel?.ResumeLayout(false);
            }

            QueueEditorLayoutRefresh();
        }

        private void ToggleFileExplorer()
        {
            if (_editorExplorerSplitContainer == null)
                return;

            ToggleFileExplorer(_editorExplorerSplitContainer.Panel2Collapsed);
        }

        private void SetFileExplorerWidth(int width)
        {
            if (_editorExplorerSplitContainer == null)
                return;

            ApplyEditorExplorerMinimumWidths();

            int splitWidth = GetEditorExplorerSplitWidth();
            int availableWidth = splitWidth - _editorExplorerSplitContainer.SplitterWidth;
            if (availableWidth <= 0)
            {
                QueueEditorLayoutRefresh();
                return;
            }

            int minDist = _editorExplorerSplitContainer.Panel1MinSize;
            int maxDist = Math.Max(minDist, availableWidth - _editorExplorerSplitContainer.Panel2MinSize);
            int explorerWidth = GetClampedFileExplorerWidth(width);
            int dist = Math.Max(minDist, Math.Min(maxDist, availableWidth - explorerWidth));

            if (!_editorExplorerSplitContainer.Panel2Collapsed)
                _editorExplorerSplitContainer.SplitterDistance = dist;

            QueueEditorLayoutRefresh();
        }

        private void SetFileExplorerNuGetHeight(int height)
        {
            if (_fileExplorerContentSplitContainer == null ||
                _fileExplorerContentSplitContainer.IsDisposed ||
                _fileExplorerContentSplitContainer.Panel2Collapsed)
            {
                return;
            }

            int splitHeight = _fileExplorerContentSplitContainer.ClientSize.Height;
            int availableHeight = splitHeight - _fileExplorerContentSplitContainer.SplitterWidth;
            if (availableHeight <= 0)
                return;

            int nuGetHeight = GetClampedFileExplorerNuGetHeight(height);
            _fileExplorerContentSplitContainer.SplitterDistance = Math.Max(0, availableHeight - nuGetHeight);
        }

        private int GetClampedFileExplorerWidth(int width)
        {
            if (_editorExplorerSplitContainer == null || _editorExplorerSplitContainer.IsDisposed)
                return width;

            int availableWidth = GetEditorExplorerSplitWidth() - _editorExplorerSplitContainer.SplitterWidth;
            if (availableWidth <= 0)
                return width;

            int minExplorerWidth = Math.Max(0, _editorExplorerSplitContainer.Panel2MinSize);
            int maxExplorerWidth = Math.Max(minExplorerWidth,
                availableWidth - Math.Max(0, _editorExplorerSplitContainer.Panel1MinSize));
            return Math.Max(minExplorerWidth, Math.Min(width, maxExplorerWidth));
        }

        private int GetClampedFileExplorerNuGetHeight(int height)
        {
            if (_fileExplorerContentSplitContainer == null ||
                _fileExplorerContentSplitContainer.IsDisposed)
            {
                return height;
            }

            int availableHeight = _fileExplorerContentSplitContainer.ClientSize.Height -
                _fileExplorerContentSplitContainer.SplitterWidth;
            if (availableHeight <= 0)
                return height;

            int minNuGetHeight = Math.Max(0, _fileExplorerContentSplitContainer.Panel2MinSize);
            int maxNuGetHeight = Math.Max(minNuGetHeight, availableHeight - FileExplorerTreeMinHeight);
            return Math.Max(minNuGetHeight, Math.Min(height, maxNuGetHeight));
        }

        private void LoadFileExplorerLayoutValues()
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

        private void ApplyFileExplorerLayoutValues()
        {
            if (_editorExplorerSplitContainer == null || _editorExplorerSplitContainer.IsDisposed)
                return;

            _suppressFileExplorerLayoutSave = true;
            _applyingFileExplorerLayout = true;
            try
            {
                _editorExplorerSplitContainer.SuspendLayout();
                _fileExplorerContentSplitContainer?.SuspendLayout();

                if (!_editorExplorerSplitContainer.Panel2Collapsed)
                    SetFileExplorerWidth(_fileExplorerWidth);
                SetFileExplorerNuGetHeight(_fileExplorerNuGetHeight);
            }
            finally
            {
                _fileExplorerContentSplitContainer?.ResumeLayout(true);
                _editorExplorerSplitContainer.ResumeLayout(true);
                _applyingFileExplorerLayout = false;
                _suppressFileExplorerLayoutSave = false;
            }
        }

        private void QueueFileExplorerLayoutApply()
        {
            if (_editorExplorerSplitContainer == null || _editorExplorerSplitContainer.IsDisposed || IsDisposed)
                return;

            _pendingFileExplorerLayoutApply = true;
            _fileExplorerLayoutApplyAttempts = 0;
            _fileExplorerLayoutReadyForUserSave = false;
            SchedulePendingFileExplorerLayoutApply();
        }

        private void OnFileExplorerLayoutContainerSizeChanged()
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

        private void CancelPendingFileExplorerLayoutApplyForUserResize()
        {
            _pendingFileExplorerLayoutApply = false;
            _fileExplorerLayoutReadyForUserSave = true;
            _fileExplorerLayoutApplyTimer?.Stop();
        }

        private void SchedulePendingFileExplorerLayoutApply()
        {
            if (!isLoaded || IsDisposed || !IsHandleCreated)
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

            _fileExplorerLayoutApplyTimer = new System.Windows.Forms.Timer(components)
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
                _editorExplorerSplitContainer == null ||
                _editorExplorerSplitContainer.IsDisposed)
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
            QueueEditorLayoutRefresh();

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
            if (_editorExplorerSplitContainer == null || _editorExplorerSplitContainer.IsDisposed)
                return false;

            if (!Visible || WindowState == FormWindowState.Minimized)
                return false;

            int splitWidth = GetEditorExplorerSplitWidth();
            if (splitWidth <= _editorExplorerSplitContainer.SplitterWidth)
                return false;

            if (_editorExplorerSplitContainer.Panel2Collapsed)
                return true;

            if (_fileExplorerContentSplitContainer == null || _fileExplorerContentSplitContainer.IsDisposed)
                return true;

            int splitHeight = _fileExplorerContentSplitContainer.ClientSize.Height;
            if (splitHeight <= _fileExplorerContentSplitContainer.SplitterWidth)
                return false;

            return true;
        }

        private bool IsFileExplorerLayoutApplied()
        {
            if (_editorExplorerSplitContainer == null || _editorExplorerSplitContainer.IsDisposed)
                return false;

            if (_editorExplorerSplitContainer.Panel2Collapsed)
                return true;

            if (!_editorExplorerSplitContainer.Panel2Collapsed)
            {
                int currentWidth = GetCurrentFileExplorerWidth();
                int targetWidth = GetClampedFileExplorerWidth(_fileExplorerWidth);
                if (currentWidth <= 0 || Math.Abs(currentWidth - targetWidth) > 1)
                    return false;
            }

            if (_fileExplorerContentSplitContainer == null ||
                _fileExplorerContentSplitContainer.IsDisposed ||
                _fileExplorerContentSplitContainer.Panel2Collapsed)
            {
                return true;
            }

            int currentHeight = GetCurrentFileExplorerNuGetHeight();
            int targetHeight = GetClampedFileExplorerNuGetHeight(_fileExplorerNuGetHeight);
            return currentHeight > 0 && Math.Abs(currentHeight - targetHeight) <= 1;
        }

        private void QueueFileExplorerWidthSave()
        {
            if (_suppressFileExplorerLayoutSave || !_fileExplorerLayoutReadyForUserSave)
                return;

            SaveFileExplorerWidth();
        }

        private void QueueFileExplorerNuGetHeightSave()
        {
            if (_suppressFileExplorerLayoutSave || !_fileExplorerLayoutReadyForUserSave)
                return;

            SaveFileExplorerNuGetHeight();
        }

        private void SaveFileExplorerWidth(bool force = false)
        {
            if (!force && (_suppressFileExplorerLayoutSave || !_fileExplorerLayoutReadyForUserSave))
                return;

            if (_editorExplorerSplitContainer == null ||
                _editorExplorerSplitContainer.IsDisposed ||
                _editorExplorerSplitContainer.Panel2Collapsed)
            {
                return;
            }

            int splitterWidth = GetCurrentFileExplorerWidth();
            if (splitterWidth <= 0)
                return;

            _fileExplorerWidth = Math.Max(FileExplorerMinWidth, splitterWidth);
            WriteFileExplorerLayoutValue(FileExplorerWidthKey, _fileExplorerWidth);
        }

        private void SaveFileExplorerNuGetHeight(bool force = false)
        {
            if (!force && (_suppressFileExplorerLayoutSave || !_fileExplorerLayoutReadyForUserSave))
                return;

            if (_fileExplorerContentSplitContainer == null ||
                _fileExplorerContentSplitContainer.IsDisposed ||
                _fileExplorerContentSplitContainer.Panel2Collapsed)
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
            if (_editorExplorerSplitContainer == null || _editorExplorerSplitContainer.IsDisposed)
                return 0;

            if (_editorExplorerSplitContainer.Panel2.Width > 0)
                return _editorExplorerSplitContainer.Panel2.Width;

            int availableWidth = GetEditorExplorerSplitWidth() - _editorExplorerSplitContainer.SplitterWidth;
            return availableWidth > 0
                ? Math.Max(0, availableWidth - _editorExplorerSplitContainer.SplitterDistance)
                : 0;
        }

        private int GetCurrentFileExplorerNuGetHeight()
        {
            if (_fileExplorerContentSplitContainer == null ||
                _fileExplorerContentSplitContainer.IsDisposed)
            {
                return 0;
            }

            if (_fileExplorerContentSplitContainer.Panel2.Height > 0)
                return _fileExplorerContentSplitContainer.Panel2.Height;

            int availableHeight = _fileExplorerContentSplitContainer.ClientSize.Height -
                _fileExplorerContentSplitContainer.SplitterWidth;
            return availableHeight > 0
                ? Math.Max(0, availableHeight - _fileExplorerContentSplitContainer.SplitterDistance)
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
            if (_editorExplorerSplitContainer == null)
                return 0;

            return _editorExplorerSplitContainer.ClientSize.Width > 0
                ? _editorExplorerSplitContainer.ClientSize.Width
                : _editorExplorerSplitContainer.Width;
        }

        private void ApplyEditorExplorerMinimumWidths()
        {
            if (_editorExplorerSplitContainer == null || _editorExplorerSplitContainer.IsDisposed)
                return;

            int splitWidth = GetEditorExplorerSplitWidth();
            int availableWidth = splitWidth - _editorExplorerSplitContainer.SplitterWidth;
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
            int currentDist = Math.Max(0, _editorExplorerSplitContainer.SplitterDistance);
            int safeDist = Math.Max(minDist, Math.Min(maxDist, currentDist));

            _editorExplorerSplitContainer.Panel1MinSize = 0;
            _editorExplorerSplitContainer.Panel2MinSize = 0;

            if (!_editorExplorerSplitContainer.Panel2Collapsed)
                _editorExplorerSplitContainer.SplitterDistance = safeDist;

            _editorExplorerSplitContainer.Panel1MinSize = panel1Min;
            _editorExplorerSplitContainer.Panel2MinSize = panel2Min;
        }

        private void PositionFileExplorerShowButton()
        {
            if (_editorWorkspacePanel == null || _fileExplorerShowButton == null)
                return;

            _fileExplorerShowButton.Location = new Point(
                Math.Max(0, _editorWorkspacePanel.ClientSize.Width - _fileExplorerShowButton.Width
                    - SystemInformation.VerticalScrollBarWidth - 4),
                6);
            _fileExplorerShowButton.BringToFront();
        }
    }
}
