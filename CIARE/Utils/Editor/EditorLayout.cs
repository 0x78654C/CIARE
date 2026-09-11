using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using ICSharpCode.TextEditor;

namespace CIARE
{
    public partial class MainForm
    {
        private bool _refreshingEditorLayoutBounds;
        private bool _pendingEditorLayoutRefresh;
        private System.Windows.Forms.Timer _editorLayoutRefreshTimer;
        private static readonly PropertyInfo DoubleBufferedProperty =
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Color GetEditorSurfaceBackColor()
        {
            return GlobalVariables.darkColor ? GlobalVariables.controlBgColor : SystemColors.Window;
        }

        private static void EnableBufferedPainting(params Control[] controls)
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

        private void ConfigureEditorTabControlLayout(bool configureAllTabs = false)
        {
            if (EditorTabControl == null)
                return;

            EditorTabControl.SuspendLayout();
            try
            {
                if (EditorTabControl.Anchor != AnchorStyles.None)
                    EditorTabControl.Anchor = AnchorStyles.None;
                if (EditorTabControl.Dock != DockStyle.Fill)
                    EditorTabControl.Dock = DockStyle.Fill;
                if (EditorTabControl.Location != Point.Empty)
                    EditorTabControl.Location = Point.Empty;
                if (EditorTabControl.Margin != Padding.Empty)
                    EditorTabControl.Margin = Padding.Empty;

                Color editorSurfaceBackColor = GetEditorSurfaceBackColor();
                if (EditorTabControl.BackColor != editorSurfaceBackColor)
                    EditorTabControl.BackColor = editorSurfaceBackColor;

                if (configureAllTabs)
                {
                    foreach (TabPage tabPage in EditorTabControl.TabPages)
                        ConfigureEditorTabPageLayout(tabPage, editorSurfaceBackColor);
                }
                else if (EditorTabControl.SelectedTab != null)
                {
                    ConfigureEditorTabPageLayout(EditorTabControl.SelectedTab, editorSurfaceBackColor);
                }
            }
            finally
            {
                EditorTabControl.ResumeLayout(false);
            }
        }

        private void ConfigureEditorTabPageLayout(TabPage tabPage)
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

        private void ConfigureEditorControlLayout(TextEditorControl editor)
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

        private void QueueEditorLayoutRefresh()
        {
            if (EditorTabControl == null || EditorTabControl.IsDisposed || IsDisposed)
                return;

            _pendingEditorLayoutRefresh = true;
            if (!isLoaded || _refreshingEditorLayoutBounds)
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

            _editorLayoutRefreshTimer = new System.Windows.Forms.Timer(components)
            {
                Interval = 16
            };
            _editorLayoutRefreshTimer.Tick += OnEditorLayoutRefreshTimer;
        }

        private void OnEditorLayoutRefreshTimer(object sender, EventArgs e)
        {
            _editorLayoutRefreshTimer?.Stop();
            if (!_pendingEditorLayoutRefresh)
                return;

            _pendingEditorLayoutRefresh = false;
            RefreshEditorLayoutBounds();
        }

        private void RefreshEditorLayoutBounds()
        {
            if (EditorTabControl == null || EditorTabControl.IsDisposed)
                return;

            if (_refreshingEditorLayoutBounds)
                return;

            _refreshingEditorLayoutBounds = true;
            try
            {
                ConfigureEditorTabControlLayout();
                EditorTabControl.PerformLayout();

                EditorTabControl.SelectedTab?.PerformLayout();
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
        private void SplitWindowHorizontally(object sender, DoWorkEventArgs e)
        {
            this.Invoke(delegate
            {
                SplitEditorWindow.SplitWindow(SelectedEditor.GetSelectedEditor(), true);
            });
        }

        /// <summary>
        /// Split window verticaly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SplitWindowVertically(object sender, DoWorkEventArgs e)
        {
            this.Invoke(delegate
            {
                SplitEditorWindow.SplitWindow(SelectedEditor.GetSelectedEditor(), false);
            });
        }

        /// <summary>
        /// Set realtime spliter position controler event to middle on texteditor resize.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textEditorControl1_Resize(object sender, EventArgs e)
        {
            if (sender is TextEditorControl editor)
            {
                if (editor.secondaryTextArea != null)
                    SplitEditorWindow.SetSplitWindowSize(editor, GlobalVariables.splitWindowPosition);
            }
        }
    }
}
