using System;
using CIARE.Utils;
using System.Windows.Forms;
using CIARE.GUI;

namespace CIARE
{
    /*
     Form for set the command line arguments.
     */
    public partial class CmdLineArgs : Form
    {
        public CmdLineArgs()
        {
            InitializeComponent();
        }

        private void CmdLineArgs_Load(object sender, EventArgs e)
        {
            cmdLineArgTxtBox.Text= GlobalVariables.commandLineArguments;
            if (GlobalVariables.darkColor)
                DarkMode.CMDLineArgsDarkMode(this, confirmBtn, cancelBtn, cmdLineArgTxtBox, groupBox1);
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void confirmBtn_Click(object sender, EventArgs e)
        {
            GlobalVariables.commandLineArguments = cmdLineArgTxtBox.Text;
            this.Close();
        }
    }
}
