using CIARE.GUI;
using CIARE.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE.Model
{
    public partial class RefManager : Form
    {
        public RefManager()
        {
            InitializeComponent();
        }

        private void RefManager_Load(object sender, EventArgs e)
        {
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
        }

        private void addRefFileBtn_Click(object sender, EventArgs e)
        {

        }
    }
}
