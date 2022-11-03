using CIARE.GUI;
using CIARE.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    public partial class LiveShareHost : Form
    {
        public LiveShareHost()
        {
            InitializeComponent();
        }

        private void LiveShareHost_Load(object sender, EventArgs e)
        {
            // Set dark mode if enabled.
            if (GlobalVariables.darkColor)
                DarkMode.LiveShareDarkMode(this, liveShareStartGrp, sessionIdLbl, sessionTxt, passwordLbl, passwordTxt, startLiveBtn,
                    remoteGrp, remoteSessionLbl, remoteSessioniDtxt, remotePassLbl, remotePasswordTxt, connectHostBtn);

            // Generate secure encryption password.
            GeneratePassword(passwordTxt);

            // Display current session id.
            // Generated only on first app run.
            SetSessionId(sessionTxt);

        }
        private void startLiveBtn_Click(object sender, EventArgs e)
        {

        }

        private void GeneratePassword(TextBox password)
        {
            password.Text = Utils.Encryption.KeyGenerator.GeneratePassword(15, true, true, false);
        }

        private void SetSessionId(TextBox sessionId)
        {
            sessionId.Text = GlobalVariables.sessionId;
        }
    }
}
