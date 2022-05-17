using System.Windows.Forms;
namespace CIARE.Utils
{
    /*
     Show/Hide split container second Panel.
     */
    public class SplitContainerHideShow
    {
        /// <summary>
        /// Hide split container second panel.
        /// </summary>
        /// <param name="splitContainer"></param>
        public static void HideSplitContainer(SplitContainer splitContainer)
        {
            splitContainer.Panel2Collapsed = true;
            splitContainer.Panel2.Hide();
        }

        /// <summary>
        /// Show split container second panel.
        /// </summary>
        /// <param name="splitContainer"></param>
        public static void ShowSplitContainer(SplitContainer splitContainer)
        {
            splitContainer.Panel2Collapsed = false;
            splitContainer.Panel2.Show();
        }
    }
}
