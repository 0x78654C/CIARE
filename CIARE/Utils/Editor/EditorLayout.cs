using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using ICSharpCode.TextEditor;

namespace CIARE.Utils.Editor
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class EditorLayout
    {
        private readonly MainForm _mainForm;

        internal EditorLayout(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        private bool _refreshingEditorLayoutBounds;
        internal bool _pendingEditorLayoutRefresh;
        internal System.Windows.Forms.Timer _editorLayoutRefreshTimer;
        private static readonly PropertyInfo DoubleBufferedProperty =
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static Color GetEditorSurfaceBackColor()
        {
            return GlobalVariables.darkColor ? GlobalVariables.controlBgColor : SystemColors.Window;
        }

        internal static void EnableBufferedPainting(params Control[] controls)
        {
            foreach (Control control in controls)
            {
                if (control == null)
                    continue;

                try
                {
                    DoubleBufferedProperty?.SetValue(control, true, null);
                }
                catch
                {
                }
            }
        }

        internal void ConfigureEditorTabControlLayout(bool configureAllTabs = false)
        {
            if (_mainForm.EditorTabControl == null)
                return;

            _mainForm.EditorTabControl.SuspendLayout();
            try
            {
                if (_mainForm.EditorTabControl.Anchor != AnchorStyles.None)
                    _mainForm.EditorTabControl.Anchor = AnchorStyles.None;
                if (_mainForm.EditorTabControl.Dock != DockStyle.Fill)
                    _mainForm.EditorTabControl.Dock = DockStyle.Fill;
                if (_mainForm.EditorTabControl.Location != Point.Empty)
                    _mainForm.EditorTabControl.Location = Point.Empty;
                if (_mainForm.EditorTabControl.Margin != Padding.Empty)
                    _mainForm.EditorTabControl.Margin = Padding.Empty;

                Color editorSurfaceBackColor = GetEditorSurfaceBackColor();
                if (_mainForm.EditorTabControl.BackColor != editorSurfaceBackColor)
                    _mainForm.EditorTabControl.BackColor = editorSurfaceBackColor;

                if (configureAllTabs)
                {
                    foreach (TabPage tabPage in _mainForm.EditorTabControl.TabPages)
                        ConfigureEditorTabPageLayout(tabPage, editorSurfaceBackColor);
                }
                else if (_mainForm.EditorTabControl.SelectedTab != null)
                {
                    ConfigureEditorTabPageLayout(_mainForm.EditorTabControl.SelectedTab, editorSurfaceBackColor);
                }
            }
            finally
            {
                _mainForm.EditorTabControl.ResumeLayout(false);
            }
        }

        internal void ConfigureEditorTabPageLayout(TabPage tabPage)
        {
            ConfigureEditorTabPageLayout(tabPage, GetEditorSurfaceBackColor());
        }

        private void ConfigureEditorTabPageLayout(TabPage tabPage, Color editorSurfaceBackColor)
        {
            if (tabPage == null)
                return;

            if (tabPage.AutoScroll)
                tabPage.AutoScroll = false;
            if (tabPage.Margin != Padding.Empty)
                tabPage.Margin = Padding.Empty;
            if (tabPage.Padding != Padding.Empty)
                tabPage.Padding = Padding.Empty;
            if (tabPage.UseVisualStyleBackColor)
                tabPage.UseVisualStyleBackColor = false;
            if (tabPage.BackColor != editorSurfaceBackColor)
                tabPage.BackColor = editorSurfaceBackColor;

            foreach (Control control in tabPage.Controls)
            {
                if (control is TextEditorControl editor)
                    ConfigureEditorControlLayout(editor);
            }
        }

        internal void ConfigureEditorControlLayout(TextEditorControl editor)
        {
            if (editor == null)
                return;

            editor.SuspendLayout();
            if (editor.Anchor != AnchorStyles.None)
                editor.Anchor = AnchorStyles.None;
            if (editor.Dock != DockStyle.Fill)
                editor.Dock = DockStyle.Fill;
            if (editor.Location != Point.Empty)
                editor.Location = Point.Empty;
            if (editor.Margin != Padding.Empty)
                editor.Margin = Padding.Empty;
            ConfigureEditorScrollBars(editor);
            editor.ResumeLayout(true);
        }

        private void ConfigureEditorScrollBars(TextEditorControl editor)
        {
            var textAreaControl = editor?.ActiveTextAreaControl;
            if (textAreaControl == null)
                return;

            if (textAreaControl.AutoHideScrollbars)
                textAreaControl.AutoHideScrollbars = false;
            if (!textAreaControl.VScrollBar.Visible)
                textAreaControl.VScrollBar.Visible = true;
            if (!textAreaControl.HScrollBar.Visible)
                textAreaControl.HScrollBar.Visible = true;
        }

        internal void QueueEditorLayoutRefresh()
        {
            if (_mainForm.EditorTabControl == null || _mainForm.EditorTabControl.IsDisposed || _mainForm.IsDisposed)
                return;

            _pendingEditorLayoutRefresh = true;
            if (!_mainForm.isLoaded || _refreshingEditorLayoutBounds)
                return;
            EnsureEditorLayoutRefreshTimer();
            // Keep servicing layout during a continuous resize instead of waiting
            // for a pause in size events. Docking already updates the child bounds.
            if (!_editorLayoutRefreshTimer.Enabled)
                _editorLayoutRefreshTimer.Start();
        }

        private void EnsureEditorLayoutRefreshTimer()
        {
            if (_editorLayoutRefreshTimer != null)
                return;

            _editorLayoutRefreshTimer = new System.Windows.Forms.Timer(_mainForm.components)
            {
                Interval = 16
            };
            _editorLayoutRefreshTimer.Tick += OnEditorLayoutRefreshTimer;
        }

        internal void OnEditorLayoutRefreshTimer(object sender, EventArgs e)
        {
            _editorLayoutRefreshTimer?.Stop();
            if (!_pendingEditorLayoutRefresh)
                return;

            _pendingEditorLayoutRefresh = false;
            RefreshEditorLayoutBounds();
        }

        internal void RefreshEditorLayoutBounds()
        {
            if (_mainForm.EditorTabControl == null || _mainForm.EditorTabControl.IsDisposed)
                return;

            if (_refreshingEditorLayoutBounds)
                return;

            _refreshingEditorLayoutBounds = true;
            try
            {
                ConfigureEditorTabControlLayout();
                _mainForm.EditorTabControl.PerformLayout();

                _mainForm.EditorTabControl.SelectedTab?.PerformLayout();
            }
            finally
            {
                _refreshingEditorLayoutBounds = false;
            }
        }

        /// <summary>
        /// Split window horizontaly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void SplitWindowHorizontally(object sender, DoWorkEventArgs e)
        {
            _mainForm.Invoke(delegate
            {
                SplitEditorWindow.SplitWindow(SelectedEditor.GetSelectedEditor(), true);
            });
        }

        /// <summary>
        /// Split window verticaly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void SplitWindowVertically(object sender, DoWorkEventArgs e)
        {
            _mainForm.Invoke(delegate
            {
                SplitEditorWindow.SplitWindow(SelectedEditor.GetSelectedEditor(), false);
            });
        }

        /// <summary>
        /// Set realtime spliter position controler event to middle on texteditor resize.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void textEditorControl1_Resize(object sender, EventArgs e)
        {
            if (sender is TextEditorControl editor)
            {
                if (editor.secondaryTextArea != null)
                    SplitEditorWindow.SetSplitWindowSize(editor, GlobalVariables.splitWindowPosition);
            }
        }
    }
}
