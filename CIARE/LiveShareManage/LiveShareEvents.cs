using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.LiveShareManage;
using CIARE.Utils;
using Microsoft.AspNetCore.SignalR.Client;

namespace CIARE.LiveShareManage
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal sealed class LiveShareEvents
    {
        private readonly MainForm _mainForm;

        internal LiveShareEvents(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        internal void Initialize() => _mainForm.EditorTabControl.SelectedIndexChanged += (_, _) => RefreshAvailability();
        internal HubConnection hubConnection;
        internal ApiConnectionEvents _apiConnectionEvents;

        /// <summary>
        /// Start live share host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void liveShareHostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CanShareEditor(_mainForm.EditorTabControl.SelectedTab?.Controls.OfType<ICSharpCode.TextEditor.TextEditorControl>().FirstOrDefault())) return;
            using var liveShareHost = new LiveShareHost();
            liveShareHost.ShowDialog(_mainForm);
        }

        internal bool CanShareEditor(ICSharpCode.TextEditor.TextEditorControl editor)
        {
            if (editor == null || editor.IsDisposed || editor.Parent is not TabPage page) return false;
            var wpf = _mainForm.WpfDesignerFeature.Get(page);
            if (wpf != null && wpf.Page == page && wpf.IsDesignView) return false;
            var form = _mainForm.FormDesignerFeature?.Get(page);
            return form == null || form.Page != page || !form.IsDesignView;
        }

        internal void RefreshAvailability()
        {
            if (_mainForm.IsDisposed || _mainForm.Disposing || _mainForm.liveShareToolStripMenuItem.IsDisposed) return;
            bool enabled = CanShareEditor(_mainForm.EditorTabControl.SelectedTab?.Controls.OfType<ICSharpCode.TextEditor.TextEditorControl>().FirstOrDefault());
            _mainForm.liveShareToolStripMenuItem.Enabled = enabled;
            _mainForm.liveShareToolStripMenuItem.ToolTipText = enabled ? "Share the active code tab" : "Use View Code to share this form's source";
        }

        /// <summary>
        /// Send data to remote client.
        /// </summary>
        internal async void SendData()
        {
            if (!GlobalVariables.connected || GlobalVariables.codeWriter ||
                _mainForm.EditorTabControl.SelectedIndex != GlobalVariables.liveTabIndex)
                return;

            var connection = hubConnection;
            await Task.Delay(10);
            if (!GlobalVariables.connected || GlobalVariables.codeWriter || !ReferenceEquals(connection, hubConnection) || _mainForm.IsDisposed)
                return;
            await _apiConnectionEvents.SendData(connection, GlobalVariables.livePassword, GlobalVariables.sessionId, SelectedEditor.GetSelectedEditor(GlobalVariables.liveTabIndex));
        }

        /// <summary>
        /// Manage live hub disconection event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void liveStatusPb_Paint(object sender, PaintEventArgs e)
        {
            // Connection state is handled by SignalR's Reconnecting, Reconnected and Closed events.
        }
    }
}
