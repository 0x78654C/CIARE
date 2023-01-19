using CIARE.Utils;
using CIARE.GUI;
using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CIARE
{
    /*
     Go to line class.
     */
    public partial class GoToLine : Form
    {
        public GoToLine()
        {
            InitializeComponent();
        }

        private void GoToLine_Load(object sender, EventArgs e)
        {
            WaterMark.TextBoxWaterMark(goToLineNumberTxt, "Enter line number...");
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
        }

        /// <summary>
        /// Exit form button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Go go line button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void goToLineBtn_Click(object sender, EventArgs e)
        {
            try
            {
                int lineNumber = Int32.Parse(goToLineNumberTxt.Text);
                GoToLineNumber.GoToLine(Form1.Instance.textEditorControl1, lineNumber);
                this.Close();
            }
            catch { }
        }

        /// <summary>
        /// Check if letters are added.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void goToLineNumberTxt_TextChanged(object sender, EventArgs e)
        {
            if (Regex.IsMatch(goToLineNumberTxt.Text, "[^0-9]"))
            {
                goToLineNumberTxt.Text = goToLineNumberTxt.Text.Remove(goToLineNumberTxt.Text.Length - 1);
                MessageBox.Show("Only numbers are accepted!", "CIARE", MessageBoxButtons.OK,
MessageBoxIcon.Warning);
                goToLineNumberTxt.SelectionStart = goToLineNumberTxt.Text.Length;
                goToLineNumberTxt.ScrollToCaret();
            }
        }
    }
}
