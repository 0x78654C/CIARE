using System;
using System.Drawing;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using CIARE.Utils.FilesOpenOS;

namespace CIARE
{
    public partial class MainForm
    {
        private System.Windows.Forms.Timer _windowPlacementSaveTimer;
        private bool _isFullScreen = false;
        private FormBorderStyle _savedBorderStyle;
        private FormWindowState _savedWindowState;
        private bool _markStartFileChkVisible;

        /// <summary>
        /// Toggle full screen mode: hides the menu bar and toolbar, leaving only the tab strip visible.
        /// </summary>
        private void ToggleFullScreen()
        {
            if (!_isFullScreen)
            {
                _windowPlacementSaveTimer?.Stop();
                SaveWindowPlacement();
                _savedBorderStyle = this.FormBorderStyle;
                _savedWindowState = this.WindowState;
                _markStartFileChkVisible = markStartFileChk.Visible;
                _isFullScreen = true;

                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;

                menuStrip1.Visible = false;
                runCodePb.Visible = false;
                label2.Visible = false;
                label3.Visible = false;
                linesCountLbl.Visible = false;
                linesPositionLbl.Visible = false;
                typeCheckLbl.Visible = false;
                warningsCheckLbl.Visible = false;
                liveStatusPb.Visible = false;
                markStartFileChk.Visible = false;

                fullScreenToolStripMenuItem.Checked = true;
            }
            else
            {
                this.FormBorderStyle = _savedBorderStyle;
                this.WindowState = _savedWindowState;

                menuStrip1.Visible = true;
                runCodePb.Visible = true;
                label2.Visible = true;
                label3.Visible = true;
                linesCountLbl.Visible = true;
                linesPositionLbl.Visible = true;
                typeCheckLbl.Visible = true;
                warningsCheckLbl.Visible = true;
                liveStatusPb.Visible = true;
                markStartFileChk.Visible = _markStartFileChkVisible;

                fullScreenToolStripMenuItem.Checked = false;
                _isFullScreen = false;
            }
            QueueEditorLayoutRefresh();
        }

        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e) => ToggleFullScreen();

        /// <summary>
        /// Form resize event used to store window size in registry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (!isLoaded || _isFullScreen || WindowState == FormWindowState.Minimized)
                return;

            if (_windowPlacementSaveTimer == null)
            {
                _windowPlacementSaveTimer = new System.Windows.Forms.Timer(components) { Interval = 250 };
                _windowPlacementSaveTimer.Tick += (timerSender, args) =>
                {
                    _windowPlacementSaveTimer.Stop();
                    SaveWindowPlacement();
                };
            }
            _windowPlacementSaveTimer.Stop();
            _windowPlacementSaveTimer.Start();
            QueueEditorLayoutRefresh();
        }

        private void SaveWindowPlacement()
        {
            if (!isLoaded || _isFullScreen || WindowState == FormWindowState.Minimized)
                return;

            Size normalSize = WindowState == FormWindowState.Normal ? Size : RestoreBounds.Size;
            if (normalSize.Width > 0 && normalSize.Height > 0)
                InitializeEditor.SetEditorWindowSize(GlobalVariables.registryPath, normalSize.Width, normalSize.Height);
            InitializeEditor.SetMaximizedWindowState(GlobalVariables.registryPath,
                WindowState == FormWindowState.Maximized);
        }

        /// <summary>
        /// Mark file for auto start event on Windows reboot.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void markStartFileChk_CheckedChanged(object sender, EventArgs e)
        {
            AutoStartFile autoStartFile = new AutoStartFile(GlobalVariables.regUserRunPath, GlobalVariables.markFile, GlobalVariables.markFileTemp, GlobalVariables.openedFilePath);
            autoStartFile.SetFilePath(markStartFileChk);
            if (GlobalVariables.OWinLoginState)
                autoStartFile.SetRegistryRunApp();
        }

        /// <summary>
        /// Refresh top most.
        /// </summary>
        public void RefreshTopMost()
        {
            TopMost = true;
            TopMost = false;
        }
    }
}
