using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.LiveShareManage;
using CIARE.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using Button = System.Windows.Forms.Button;

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
        internal HubConnection hubConnection;
        internal ApiConnectionEvents _apiConnectionEvents;

        /// <summary>
        /// Start live share host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void liveShareHostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LiveShareHost liveShareHost = new LiveShareHost();
            liveShareHost.ShowDialog();
        }

        /// <summary>
        /// Send data to remote client.
        /// </summary>
        internal async void SendData()
        {
            GlobalVariables.codeWriter = false;

            if (!GlobalVariables.connected)
                return;

            await Task.Delay(10);
            await _apiConnectionEvents.SendData(hubConnection, GlobalVariables.livePassword, GlobalVariables.sessionId, SelectedEditor.GetSelectedEditor(GlobalVariables.liveTabIndex));
        }

        /// <summary>
        /// Manage live hub disconection event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void liveStatusPb_Paint(object sender, PaintEventArgs e)
        {
            if (GlobalVariables.connected && GlobalVariables.liveDisconnected)
            {
                if (GlobalVariables.apiRemoteConnected || GlobalVariables.apiConnected)
                    ApiConnectionEvents.ManageHubDisconnection(hubConnection, new Button());
            }
        }
    }
}
