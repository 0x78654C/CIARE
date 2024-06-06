using ICSharpCode.TextEditor;
using System.Runtime.Versioning;

namespace CIARE.Utils
{
    [SupportedOSPlatform("windows")]
    /*
     Split ICSharpCode.TextEditor window.
     */
    public class SwitchSplit
    {
        /// <summary>
        /// Split text editor window.
        /// </summary>
        /// <param name="textEditor"></param>
        public static void SwitchSplitWindow()
        {
            var firstArea = GlobalVariables.textAreaFirst;
            var secondArea = GlobalVariables.textAreaSecond;
            if ((firstArea != null) && firstArea.TextArea.Focused)
            {
                if (secondArea != null)
                    secondArea.TextArea.Focus();
            }
            else
            {
                if (firstArea != null)
                    firstArea.TextArea.Focus();
            }
        }
    }
}
