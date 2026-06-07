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
        private const int OutputWindowHeight = 240;

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
                ShowOutputWindow(splitContainer, outLogRtb);
                return;
            }

            if (GlobalVariables.outPutDisplay)
            {
                ShowOutputWindow(splitContainer, outLogRtb);
            }
        }

        public static void ShowOutputWindow(SplitContainer splitContainer, RichTextBox outLogRtb)
        {
            SplitContainerHideShow.ShowSplitContainer(splitContainer);
            SetOutputWindowHeight(splitContainer, OutputWindowHeight);
            MainForm.Instance.visibleSplitContainer = false;
            GlobalVariables.outPutDisplay = false;
            outLogRtb.Focus();
            outLogRtb.ScrollToEnd(true);
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
                SetOutputWindowHeight(splitContainer, OutputWindowHeight);
                MainForm.Instance.visibleSplitContainer = false;
                MainForm.Instance.visibleSplitContainerAutoHide = true;
                outputRtb.Focus();
                outputRtb.ScrollToEnd(true);
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

        private static void SetOutputWindowHeight(SplitContainer splitContainer, int height)
        {
            if (splitContainer == null || splitContainer.IsDisposed || splitContainer.Orientation != Orientation.Horizontal)
                return;

            int availableHeight = splitContainer.ClientSize.Height - splitContainer.SplitterWidth;
            if (availableHeight <= 0)
                return;

            if (availableHeight <= splitContainer.Panel1MinSize + splitContainer.Panel2MinSize)
                return;

            int outputHeight = System.Math.Max(splitContainer.Panel2MinSize,
                System.Math.Min(height, availableHeight - splitContainer.Panel1MinSize));
            splitContainer.SplitterDistance = System.Math.Max(0, availableHeight - outputHeight);
        }
    }
}
