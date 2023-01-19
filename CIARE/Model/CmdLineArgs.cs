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
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
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
