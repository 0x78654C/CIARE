using CIARE.GUI;
using CIARE.Utils;
using ICSharpCode.TextEditor;
using System.Drawing;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Versioning;

namespace CIARE.LiveShareManage
{
    public class UserDisplay
    {
        [SupportedOSPlatform("windows")]

        /// <summary>
        /// Output Error message to richtextbox.
        /// </summary>
        /// <param name="richTextBox"></param>
        /// <param name="errorId"></param>
        /// <param name="errorMessage"></param>
        /// <param name="lineNumber"></param>
        public static void Show(string userName, int lineNumber)
        {
            var screenPosition = SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.Caret.ScreenPosition;
            var colPos = screenPosition.Y;
            screenPosition = SelectedEditor.GetSelectedEditor().ActiveTextAreaControl.TextArea.Caret.ScreenPosition;
            var X = screenPosition.X;
            var Y = screenPosition.Y + 23;
            var pos = new Point(X, Y);
            var contextMenuStrip = new ContextMenuStrip();
            var itemMenu = new ToolStripMenuItem();
            itemMenu.Text = $"\u2196\n{userName}";
            contextMenuStrip.Name = "Error Notification";
            itemMenu.BackColor = Color.FromArgb(30, 30, 31);
            itemMenu.ForeColor = Color.IndianRed;
            itemMenu.Font = new Font(new FontFamily(GenericFontFamilies.Monospace), 11.28f, FontStyle.Italic | FontStyle.Bold);
            contextMenuStrip.Items.Add(itemMenu);
            contextMenuStrip.Show(SelectedEditor.GetSelectedEditor().ActiveTextAreaControl, pos);
        }
    }
}
