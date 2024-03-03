using CIARE.Utils;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.GUI
{
    public partial class HotKeys : Form
    {
        [SupportedOSPlatform("windows")]
        public HotKeys()
        {
            InitializeComponent();
        
        }

        private void HotKeys_Load(object sender, System.EventArgs e)
        {
            InitializeEditor.ReadEditorHighlight(GlobalVariables.registryPath, textEditorControl1, new ComboBox { });
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
        }
    }
}
