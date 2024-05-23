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
                MainForm.Instance.visibleSplitContainer = false;
                GlobalVariables.outPutDisplay = false;
                outLogRtb.Focus();
                outLogRtb.ScrollToEnd();
                return;
            }

            if (GlobalVariables.outPutDisplay)
            {
                SplitContainerHideShow.ShowSplitContainer(splitContainer);
                MainForm.Instance.visibleSplitContainer = false;
                GlobalVariables.outPutDisplay = false;
                outLogRtb.Focus();
                outLogRtb.ScrollToEnd();
            }
        }

        /// <summary>
        /// Set output window state.
        /// </summary>
        /// <param name="outputRtb"></param>
        /// <param name="splitContainer"></param>
        public static void SetOutputWindowState(RichTextBox outputRtb,SplitContainer splitContainer)
        {
            if (MainForm.Instance.visibleSplitContainer)
            {
                SplitContainerHideShow.ShowSplitContainer(splitContainer);
                MainForm.Instance.visibleSplitContainer = false;
                MainForm.Instance.visibleSplitContainerAutoHide = true;
                outputRtb.Focus();
                outputRtb.ScrollToEnd();
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, "OutWState", "False");
            }
            else
            {
                SplitContainerHideShow.HideSplitContainer(splitContainer);
                MainForm.Instance.visibleSplitContainer = true;
                MainForm.Instance.visibleSplitContainerAutoHide = false;
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, "OutWState", "True");
            }
        }
    }
}
