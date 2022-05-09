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
            if (GlobalVariables.exeName)
            {
                this.Text = "Set EXE binary name";
                Utils.WaterMark.TextBoxWaterMark(binaryNameTxt, "binary_file_name.exe");
                return;
            }
            this.Text = "Set DLL binary name";
            WaterMark.TextBoxWaterMark(binaryNameTxt, "binary_file_name.dll");
        }

        private void ConfirmButton_Click(object sender, EventArgs e)
        {
            if (binaryNameTxt.Text.Length > 0)
            {
                if (GlobalVariables.exeName && !binaryNameTxt.Text.ToLower().EndsWith(".exe"))
                {
                    MessageBox.Show("File name needs to end with '.exe' extension!", "CIARE", MessageBoxButtons.OK,
 MessageBoxIcon.Warning);
                    return;
                }
                if (GlobalVariables.exeName == false && !binaryNameTxt.Text.ToLower().EndsWith(".dll"))
                {
                    MessageBox.Show("File name needs to end with '.dll' extension!", "CIARE", MessageBoxButtons.OK,
 MessageBoxIcon.Warning);
                    GlobalVariables.exeName = false;
                    return;
                }
                GlobalVariables.binaryName = binaryNameTxt.Text;
                _checkConfirmationAction = true;
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
