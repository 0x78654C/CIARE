using System;
using System.Drawing;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using CIARE.Utils.FilesOpenOS;

namespace CIARE.Utils.Window
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class WindowState
    {
        private readonly MainForm _mainForm;

        internal WindowState(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        internal System.Windows.Forms.Timer _windowPlacementSaveTimer;
        private bool _isFullScreen = false;
        private FormBorderStyle _savedBorderStyle;
        private FormWindowState _savedWindowState;
        private bool _markStartFileChkVisible;

        /// <summary>
        /// Toggle full screen mode: hides the menu bar and toolbar, leaving only the tab strip visible.
        /// </summary>
        internal void ToggleFullScreen()
        {
            if (!_isFullScreen)
            {
                _windowPlacementSaveTimer?.Stop();
                SaveWindowPlacement();
                _savedBorderStyle = _mainForm.FormBorderStyle;
                _savedWindowState = _mainForm.WindowState;
                _markStartFileChkVisible = _mainForm.markStartFileChk.Visible;
                _isFullScreen = true;

                _mainForm.WindowState = FormWindowState.Normal;
                _mainForm.FormBorderStyle = FormBorderStyle.None;
                _mainForm.WindowState = FormWindowState.Maximized;

                _mainForm.menuStrip1.Visible = false;
                _mainForm.runCodePb.Visible = false;
                _mainForm.label2.Visible = false;
                _mainForm.label3.Visible = false;
                _mainForm.linesCountLbl.Visible = false;
                _mainForm.linesPositionLbl.Visible = false;
                _mainForm.typeCheckLbl.Visible = false;
                _mainForm.warningsCheckLbl.Visible = false;
                _mainForm.liveStatusPb.Visible = false;
                _mainForm.markStartFileChk.Visible = false;

                _mainForm.fullScreenToolStripMenuItem.Checked = true;
            }
            else
            {
                _mainForm.FormBorderStyle = _savedBorderStyle;
                _mainForm.WindowState = _savedWindowState;

                _mainForm.menuStrip1.Visible = true;
                _mainForm.runCodePb.Visible = true;
                _mainForm.label2.Visible = true;
                _mainForm.label3.Visible = true;
                _mainForm.linesCountLbl.Visible = true;
                _mainForm.linesPositionLbl.Visible = true;
                _mainForm.typeCheckLbl.Visible = true;
                _mainForm.warningsCheckLbl.Visible = true;
                _mainForm.liveStatusPb.Visible = true;
                _mainForm.markStartFileChk.Visible = _markStartFileChkVisible;

                _mainForm.fullScreenToolStripMenuItem.Checked = false;
                _isFullScreen = false;
            }
            _mainForm.MenuStatusLayoutFeature.SetFullScreen(_isFullScreen);
            _mainForm.EditorLayoutFeature.QueueEditorLayoutRefresh();
        }

        internal void fullScreenToolStripMenuItem_Click(object sender, EventArgs e) => ToggleFullScreen();

        /// <summary>
        /// Form resize event used to store window size in registry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void MainForm_Resize(object sender, EventArgs e)
        {
            if (!_mainForm.isLoaded || _isFullScreen || _mainForm.WindowState == FormWindowState.Minimized)
                return;

            if (_windowPlacementSaveTimer == null)
            {
                _windowPlacementSaveTimer = new System.Windows.Forms.Timer(_mainForm.components) { Interval = 250 };
                _windowPlacementSaveTimer.Tick += (timerSender, args) =>
                {
                    _windowPlacementSaveTimer.Stop();
                    SaveWindowPlacement();
                };
            }
            _windowPlacementSaveTimer.Stop();
            _windowPlacementSaveTimer.Start();
            _mainForm.EditorLayoutFeature.QueueEditorLayoutRefresh();
        }

        internal void SaveWindowPlacement()
        {
            if (!_mainForm.isLoaded || _isFullScreen || _mainForm.WindowState == FormWindowState.Minimized)
                return;

            Size normalSize = _mainForm.WindowState == FormWindowState.Normal ? _mainForm.Size : _mainForm.RestoreBounds.Size;
            if (normalSize.Width > 0 && normalSize.Height > 0)
                InitializeEditor.SetEditorWindowSize(GlobalVariables.registryPath, normalSize.Width, normalSize.Height);
            InitializeEditor.SetMaximizedWindowState(GlobalVariables.registryPath,
                _mainForm.WindowState == FormWindowState.Maximized);
        }

        /// <summary>
        /// Mark file for auto start event on Windows reboot.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void markStartFileChk_CheckedChanged(object sender, EventArgs e)
        {
            AutoStartFile autoStartFile = new AutoStartFile(GlobalVariables.regUserRunPath, GlobalVariables.markFile, GlobalVariables.markFileTemp, GlobalVariables.openedFilePath);
            autoStartFile.SetFilePath(_mainForm.markStartFileChk);
            if (GlobalVariables.OWinLoginState)
                autoStartFile.SetRegistryRunApp();
        }

        /// <summary>
        /// Refresh top most.
        /// </summary>
        public void RefreshTopMost()
        {
            _mainForm.TopMost = true;
            _mainForm.TopMost = false;
        }
    }
}
