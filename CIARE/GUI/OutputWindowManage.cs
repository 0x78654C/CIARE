using CIARE.Utils;
using System.Windows.Forms;

namespace CIARE.GUI
{
    /*
     Class for manage output window (show/hide).
     */
    public class OutputWindowManage
    {
        /// <summary>
        /// Pop up the output pane on compile or code run.
        /// </summary>
        public static void ShowOutputOnCompileRun(bool runner, SplitContainer splitContainer, RichTextBox outLogRtb)
        {
            if (runner)
            {
                SplitContainerHideShow.ShowSplitContainer(splitContainer);
                Form1.Instance._visibleSplitContainer = false;
                GlobalVariables.outPutDisplay = false;
                outLogRtb.Focus();
                return;
            }

            if (GlobalVariables.outPutDisplay)
            {
                SplitContainerHideShow.ShowSplitContainer(splitContainer);
                Form1.Instance._visibleSplitContainer = false;
                GlobalVariables.outPutDisplay = false;
                outLogRtb.Focus();
            }
        }
    }
}
