using CIARE.Utils;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    public partial class HotKeys : Form
    {
        private const string HotKeyDescription = @"----------- File Management ---------------
CTRL + N         : Empty the current tab file and set it to a new page.
CTRL + Shift + N : Create a new C# project or solution.
CTRL + O         : Open file.
CTRL + Shift + O : Open an existing .sln or .csproj project/solution.
CTRL + S         : Save data to current file if changed.
CTRL + Shift + S : Save data to a new file name or existing one.
CTRL + T         : Load C# Main template.

------------ Editor Management ------------
CTRL + Z         : Undo last modification.
CTRL + Y         : Redo last modification.
CTRL + Delete    : Delete word to the right.
CTRL + Backspace : Delete word to the left.
CTRL + D         : Duplicate current line.
CTRL + Shift + D : Delete from cursor to end of line.
CTRL + X         : Cut selection.
CTRL + C         : Copy selection.
CTRL + V         : Paste selection.
DEL              : Delete selection.
CTRL + F         : Find text in current tab.
CTRL + H         : Replace text in current tab.
CTRL + G         : Go to line number in current tab.
CTRL + A         : Select all text in current tab.
CTRL + Shift + P : Ask AI with your current text or selection.
CTRL + E         : Show / Hide file explorer.
CTRL + Left Click: Go to definition.
Shift + F12      : Find usages.

---------------- Debug --------------------
F5               : Start debugging / continue.
CTRL + F5        : Run current code without debugging.
SHIFT + F5       : Stop debugging.
F9               : Toggle breakpoint at the current line.
CTRL+SHIFT+F9    : Delete all breakpoints.
F10              : Step over.
F11              : Step into.
SHIFT + F11      : Step out.

---------------- Compile ------------------
CTRL + B         : Compile code from current tab to binary. (.dll/.exe)
CTRL + Shift + B : Publish code from current tab to binary. (.dll/.exe)
CTRL + L         : Add command line arguments.
CTRL + R         : Add external reference or download from NuGet.

------------------ View -------------------
CTRL + W         : Split window vertically.
CTRL + Shift + W : Split window horizontally.
CTRL + U         : Switch between split window areas.
CTRL + K         : Show / Hide output window.
CTRL + E         : Show / Hide file explorer.
SHIFT+ALT+ENTER  : Toggle full screen.

------------- Tabs Management -------------
CTRL + Tab       : Add new tab.
CTRL + Left      : Switch tabs to the left.
CTRL + Right     : Switch tabs to the right.

----------- Live Share Management ---------
CTRL + Q         : Start live share management window.

------------ NuGet Search Window ----------
SHIFT + F10      : Download selected NuGet package.";

        public HotKeys()
        {
            InitializeComponent();
        
        }

        /// <summary>
        /// Overwrite the key press.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape:
                    this.Close();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void HotKeys_Load(object sender, System.EventArgs e)
        {
            textEditorControl1.Text = HotKeyDescription;
            InitializeEditor.ReadEditorHighlight(GlobalVariables.registryPath, textEditorControl1, new ComboBox { });
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
        }
    }
}
