using CIARE.Utils;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.GUI
{
    /*
     Class for manage output window (show/hide).
     */
    [SupportedOSPlatform("windows")]
    public class OutputWindowManage
    {
        /// <summary>
        /// Pop up the output pane on compile or code run.
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="splitContainer"></param>
        /// <param name="outLogRtb"></param>
        public static void ShowOutputOnCompileRun(bool runner, SplitContainer splitContainer, RichTextBox outLogRtb)
        {
            if (runner)
            {
                SplitContainerHideShow.ShowSplitContainer(splitContainer);
                Form1.Instance.visibleSplitContainer = false;
                GlobalVariables.outPutDisplay = false;
                outLogRtb.Focus();
                return;
            }

            if (GlobalVariables.outPutDisplay)
            {
                SplitContainerHideShow.ShowSplitContainer(splitContainer);
                Form1.Instance.visibleSplitContainer = false;
                GlobalVariables.outPutDisplay = false;
                outLogRtb.Focus();
            }
        }

        /// <summary>
        /// Set output window state.
        /// </summary>
        /// <param name="outputRtb"></param>
        /// <param name="splitContainer"></param>
        public static void SetOutputWindowState(RichTextBox outputRtb,SplitContainer splitContainer)
        {
            if (Form1.Instance.visibleSplitContainer)
            {
                SplitContainerHideShow.ShowSplitContainer(splitContainer);
                Form1.Instance.visibleSplitContainer = false;
                Form1.Instance.visibleSplitContainerAutoHide = true;
                outputRtb.Focus();
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, "OutWState", "False");
            }
            else
            {
                SplitContainerHideShow.HideSplitContainer(splitContainer);
                Form1.Instance.visibleSplitContainer = true;
                Form1.Instance.visibleSplitContainerAutoHide = false;
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, "OutWState", "True");
            }
        }
    }
}
