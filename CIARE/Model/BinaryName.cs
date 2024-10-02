using System;
using System.Windows.Forms;
using CIARE.Utils;
using CIARE.GUI;
using System.Runtime.Versioning;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    public partial class BinaryName : Form
    {
        /*
         Form for handle binary name.
         */

        private bool _checkConfirmationAction = false;
        public BinaryName()
        {
            InitializeComponent();
        }

        private void BinaryName_Load(object sender, EventArgs e)
        {
            GlobalVariables.checkFormOpen = true;
            binaryNameTxt.Text = GlobalVariables.binaryNameStore;
            binaryNameTxt.Text = binaryNameTxt.Text.Trim();
            typeApp.Text = GlobalVariables.binarytype;
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
            if (GlobalVariables.binaryPublish)
                this.Text = "Set binary name - Publish";
            else
                this.Text = "Set binary name - Compile";
            WaterMark.TextBoxWaterMark(binaryNameTxt, "Enter name of output file..");
            if (typeApp.Text == ".exe")
                typeCompileCkb.Visible = true;
            if (GlobalVariables.OutputKind == Microsoft.CodeAnalysis.OutputKind.WindowsApplication)
                typeCompileCkb.Checked = true;
        }


        private void ConfirmButton_Click(object sender, EventArgs e)
        {
            binaryNameTxt.Text = binaryNameTxt.Text.Trim();
            if (binaryNameTxt.Text.Length > 0)
            {
                GlobalVariables.binaryName = binaryNameTxt.Text + typeApp.Text;
                _checkConfirmationAction = true;
                GlobalVariables.outPutDisplay = true;
                GlobalVariables.binaryNameStore = binaryNameTxt.Text;
                GlobalVariables.binarytype = typeApp.Text;
                if (typeApp.Text == ".exe")
                    GlobalVariables.binarytypeTemplate = "Exe";
                else
                    GlobalVariables.binarytypeTemplate = "Library";
                this.Close();
            }
            else
            {
                MessageBox.Show("You need to provide a name for the binary file!", "CIARE", MessageBoxButtons.OK,
 MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Clear binary variables on cance button event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Clear binary variables on form close.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BinaryName_FormClosed(object sender, FormClosedEventArgs e)
        {
            GlobalVariables.checkFormOpen = false;
            GlobalVariables.exeName = false;
            if (!_checkConfirmationAction)
                GlobalVariables.binaryName = string.Empty;
        }

        private void typeCompileCkb_CheckedChanged(object sender, EventArgs e)
        {
            if (typeCompileCkb.Checked)
                GlobalVariables.OutputKind = Microsoft.CodeAnalysis.OutputKind.WindowsApplication;
            else
                GlobalVariables.OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication;
        }

        /// <summary>
        /// Display type of application you want to compile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void typeApp_SelectedIndexChanged(object sender, EventArgs e)
        {
            typeCompileCkb.Visible = (typeApp.Text == ".exe") ? true : false;
        }
    }
}
