using ICSharpCode.TextEditor.Actions;
using Microsoft.CodeAnalysis.Differencing;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Numerics;
using System.Threading.Channels;
using System.Windows.Forms;
using System.Windows.Shapes;
using System.Xml.Linq;
using MessageBox = System.Windows.Forms.MessageBox;

namespace CIARE.Utils
{
    public class HotKeys
    {
        public static void ShowHotKeys()
        {
            string hotKeys = @"
-----------File management ----------------
CTRL + N         : Empty the current tab file and sets to new page.
CTRL + O         : Open file.
CTRL + S         : Save to data to current file if changed.
CTRL + Shift + S : Save data to a new file name or existing one.
CTRL + T         : Load C# Main template.

----------- Edit file management ----------
CTRL + Z         : Undo last modifications.
CTRL + X         : Cut selection.
CTRL + C         : Copy selection.
CTRL + V         : Paste selection.
Del              : Delete Selection.
CTRL + F         : Find text in current tab.
CTRL + H         : Replace text in current tab.
CTRL + G         : Go to line number in current tab.
CTRL + A         : Select all text in current tab.
CTRL + Shift + P : Get data from chatGPT by your provided text pattern.

---------------- Compile ------------------
CTRL + B         : Compile code from current tab to executable file. (.exe)
CTRL + Shift + B : Compile code from current tab to dynamic - link library. (.dll)
CTRL + L         : Add command line arguments.
CTRL + R         : Add external reference or download from NuGet.

------------------ View -------------------
CTRL + W         : Split window vertically.
CTRL + Shift + W : Split window horizontally.
CTRL + K         : Show / Hide output window.

-----------Live share management ----------
CTRL + Q         : Start live share management window.
";

            MessageBox.Show(hotKeys);
        } 
    }
}
