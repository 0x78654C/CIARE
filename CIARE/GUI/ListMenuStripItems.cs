using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.Versioning;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    public class ListMenuStripItems
    {
        /// <summary>
        /// List of toolstripmenu from menu bar.
        /// </summary>
        /// <returns></returns>
        public static List<ToolStripMenuItem> ListToolStripMenu()
        {
            List<ToolStripMenuItem> listToosStripM = new List<ToolStripMenuItem>
            {
                MainForm.Instance.fIleToolStripMenuItem,
                MainForm.Instance.openToolStripMenuItem,
                MainForm.Instance.saveToolStripMenuItem,
                MainForm.Instance.exitToolStripMenuItem,
                MainForm.Instance.saveAsStripMenuItem,
                MainForm.Instance.newFileStripMenuItem,
                MainForm.Instance.LoadCStripMenuItem,
                MainForm.Instance.helpToolStripMenuItem,
                MainForm.Instance.aboutToolStripMenuItem,
                MainForm.Instance.compileToolStripMenuItem,
                MainForm.Instance.compileToexeCtrlShiftBToolStripMenuItem,
                MainForm.Instance.compileToDLLCtrlSfitBToolStripMenuItem,
                MainForm.Instance.editToolStripMenuItem,
                MainForm.Instance.undoToolStripMenuItem,
                MainForm.Instance.redoToolStripMenuItem,
                MainForm.Instance.copyStripMenuItem,
                MainForm.Instance.cutStripMenuItem,
                MainForm.Instance.pasteStripMenuItem,
                MainForm.Instance.deleteStripMenuItem,
                MainForm.Instance.replaceStripMenuItem,
                MainForm.Instance.selectAllStripMenuItem3,
                MainForm.Instance.viewToolStripMenuItem,
                MainForm.Instance.splitEditorToolStripMenuItem,
                MainForm.Instance.showHideHSCToolStripMenuItem,
                MainForm.Instance.goToLineStripMenuItem,
                MainForm.Instance.cmdLinesArgsStripMenuItem,
                MainForm.Instance.splitVEditorToolStripMenuItem,
                MainForm.Instance.fIleToolStripMenuItem,
                MainForm.Instance.finStripMenuItem,
                MainForm.Instance.optionsToolStripMenuItem,
                MainForm.Instance.liveShareToolStripMenuItem,
                MainForm.Instance.chatGPTCTRLShiftPToolStripMenuItem,
                MainForm.Instance.referenceAddToolStripMenuItem,
                MainForm.Instance.hotKeyToolStripMenuItem,
            };
            return listToosStripM;
        }

        /// <summary>
        /// List of ToolStripSeparators from menu bar.
        /// </summary>
        /// <returns></returns>
        public static List<ToolStripSeparator> ListToolStripSeparator()
        {
            List<ToolStripSeparator> listToosStripS = new List<ToolStripSeparator>
            {
                MainForm.Instance.toolStripSeparator1,
                MainForm.Instance.toolStripSeparator2,
                MainForm.Instance.toolStripSeparator3,
                MainForm.Instance.toolStripSeparator4,
                MainForm.Instance.toolStripSeparator5,
                MainForm.Instance.toolStripSeparator6,
                MainForm.Instance.toolStripSeparator7,
                MainForm.Instance.compileStripSeparator1
            };
            return listToosStripS;
        }
    }
}
