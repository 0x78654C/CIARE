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
            List<ToolStripMenuItem> listToosStripM = new List<ToolStripMenuItem>();
            listToosStripM.Add(MainForm.Instance.fIleToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.openToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.saveToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.exitToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.saveAsStripMenuItem);
            listToosStripM.Add(MainForm.Instance.newFileStripMenuItem);
            listToosStripM.Add(MainForm.Instance.LoadCStripMenuItem);
            listToosStripM.Add(MainForm.Instance.helpToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.aboutToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.compileToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.compileToexeCtrlShiftBToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.compileToDLLCtrlSfitBToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.editToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.undoToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.copyStripMenuItem);
            listToosStripM.Add(MainForm.Instance.cutStripMenuItem);
            listToosStripM.Add(MainForm.Instance.pasteStripMenuItem);
            listToosStripM.Add(MainForm.Instance.deleteStripMenuItem);
            listToosStripM.Add(MainForm.Instance.replaceStripMenuItem);
            listToosStripM.Add(MainForm.Instance.selectAllStripMenuItem3);
            listToosStripM.Add(MainForm.Instance.viewToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.splitEditorToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.showHideHSCToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.goToLineStripMenuItem);
            listToosStripM.Add(MainForm.Instance.cmdLinesArgsStripMenuItem);
            listToosStripM.Add(MainForm.Instance.splitVEditorToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.fIleToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.finStripMenuItem);
            listToosStripM.Add(MainForm.Instance.optionsToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.liveShareToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.chatGPTCTRLShiftPToolStripMenuItem);
            listToosStripM.Add(MainForm.Instance.referenceAddToolStripMenuItem);
            return listToosStripM;
        }

        /// <summary>
        /// List of ToolStripSeparators from menu bar.
        /// </summary>
        /// <returns></returns>
        public static List<ToolStripSeparator> ListToolStripSeparator()
        {
            List<ToolStripSeparator> listToosStripS = new List<ToolStripSeparator>();
            listToosStripS.Add(MainForm.Instance.toolStripSeparator1);
            listToosStripS.Add(MainForm.Instance.toolStripSeparator2);
            listToosStripS.Add(MainForm.Instance.toolStripSeparator3);
            listToosStripS.Add(MainForm.Instance.toolStripSeparator4);
            listToosStripS.Add(MainForm.Instance.toolStripSeparator5);
            listToosStripS.Add(MainForm.Instance.toolStripSeparator6);
            listToosStripS.Add(MainForm.Instance.toolStripSeparator7);
            listToosStripS.Add(MainForm.Instance.compileStripSeparator1);
            return listToosStripS;
        }
    }
}
