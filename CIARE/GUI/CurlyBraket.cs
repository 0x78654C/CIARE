using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]

    public class CurlyBraket
    {

        /// <summary>
        /// Insert ending curly braket when { is typed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TextArea_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '{')
            {
                var textArea = SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea;
                int caretOffset = SelectedEditor.GetSelectedEditor().Document.PositionToOffset(textArea.Caret.Position);

                // Insert the curly brace pair
                SelectedEditor.GetSelectedEditor().Document.Insert(caretOffset, "}");

                // Move the caret between the braces
                textArea.Caret.Position = SelectedEditor.GetSelectedEditor().Document.OffsetToPosition(caretOffset);
                
                e.Handled = true; // Prevent default behavior
            }
        }
    }
}
