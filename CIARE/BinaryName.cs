using System;
using System.Windows.Forms;
using CIARE.Utils;

namespace CIARE
{
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
            if (GlobalVariables.exeName)
            {
                this.Text = "Set EXE binary name";
                WaterMark.TextBoxWaterMark(binaryNameTxt, "Enter name of output file..");
                return;
            }
            this.Text = "Set DLL binary name";
            WaterMark.TextBoxWaterMark(binaryNameTxt, "Enter name of output file..");
        }


        private void ConfirmButton_Click(object sender, EventArgs e)
        {
            if (binaryNameTxt.Text.Length > 0)
            {
                if(GlobalVariables.exeName)
                    GlobalVariables.binaryName = binaryNameTxt.Text+".exe";
                else
                    GlobalVariables.binaryName = binaryNameTxt.Text + ".dll";

                _checkConfirmationAction = true;
                GlobalVariables.outPutDisplay = true;
                GlobalVariables.binaryNameStore = binaryNameTxt.Text;
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
    }
}
