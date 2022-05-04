using System;
using System.Windows.Forms;

namespace CIARE
{
    public partial class BinaryName : Form
    {
        /*
         Form for handle binary name.
         */
        public BinaryName()
        {
            InitializeComponent();
        }

        private void BinaryName_Load(object sender, EventArgs e)
        {
            if (Utils.GlobalVariables.exeName)
            {
                this.Text = "Set EXE binary name";
                Utils.WaterMark.TextBoxWaterMark(binaryNameTxt, "binary_file_name.exe");
                return;
            }
            this.Text = "Set DLL binary name";
            Utils.WaterMark.TextBoxWaterMark(binaryNameTxt, "binary_file_name.dll");
        }

        private void ConfirmButton_Click(object sender, EventArgs e)
        {
            if (binaryNameTxt.Text.Length > 0)
            {
                if (Utils.GlobalVariables.exeName && !binaryNameTxt.Text.ToLower().EndsWith(".exe"))
                {
                    MessageBox.Show("File name needs to end with '.exe' extension!", "CIARE", MessageBoxButtons.OK,
 MessageBoxIcon.Warning);
                    return;
                }
                if (Utils.GlobalVariables.exeName == false && !binaryNameTxt.Text.ToLower().EndsWith(".dll"))
                {
                    MessageBox.Show("File name needs to end with '.dll' extension!", "CIARE", MessageBoxButtons.OK,
 MessageBoxIcon.Warning);
                    return;
                }
                Utils.GlobalVariables.binaryName = binaryNameTxt.Text;
                this.Close();
            }
            else
            {
                MessageBox.Show("You need to provide a name for the binary file!", "CIARE", MessageBoxButtons.OK,
 MessageBoxIcon.Warning);
            }
            Utils.GlobalVariables.exeName = false;
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
