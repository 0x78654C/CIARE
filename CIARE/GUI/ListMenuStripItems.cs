using System.Collections.Generic;
using System.Windows.Forms;

namespace CIARE.GUI
{
    public class ListMenuStripItems
    {
        /// <summary>
        /// List of toolstripmenu from menu bar.
        /// </summary>
        /// <returns></returns>
        public static List<ToolStripMenuItem> ListToolStripMenu()
        {
            List<ToolStripMenuItem> listToosStripM = new List<ToolStripMenuItem>();
            listToosStripM.Add(Form1.Instance.fIleToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.openToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.saveToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.exitToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.saveAsStripMenuItem);
            listToosStripM.Add(Form1.Instance.toolStripMenuItem1);
            listToosStripM.Add(Form1.Instance.LoadCStripMenuItem);
            listToosStripM.Add(Form1.Instance.helpToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.aboutToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.compileToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.compileToexeCtrlShiftBToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.compileToDLLCtrlSfitBToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.editToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.undoToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.copyStripMenuItem);
            listToosStripM.Add(Form1.Instance.cutStripMenuItem);
            listToosStripM.Add(Form1.Instance.pasteStripMenuItem);
            listToosStripM.Add(Form1.Instance.deleteStripMenuItem);
            listToosStripM.Add(Form1.Instance.replaceStripMenuItem);
            listToosStripM.Add(Form1.Instance.selectAllStripMenuItem3);
            listToosStripM.Add(Form1.Instance.viewToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.splitEditorToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.showHideHSCToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.goToLineStripMenuItem);
            listToosStripM.Add(Form1.Instance.cmdLinesArgsStripMenuItem);
            listToosStripM.Add(Form1.Instance.splitVEditorToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.fIleToolStripMenuItem);
            listToosStripM.Add(Form1.Instance.finStripMenuItem);
            listToosStripM.Add(Form1.Instance.optionsToolStripMenuItem);
            return listToosStripM;
        }

        /// <summary>
        /// List of ToolStripSeparators from menu bar.
        /// </summary>
        /// <returns></returns>
        public static List<ToolStripSeparator> ListToolStripSeparator()
        {
            List<ToolStripSeparator> listToosStripS = new List<ToolStripSeparator>();
            listToosStripS.Add(Form1.Instance.toolStripSeparator1);
            listToosStripS.Add(Form1.Instance.toolStripSeparator2);
            listToosStripS.Add(Form1.Instance.toolStripSeparator3);
            listToosStripS.Add(Form1.Instance.toolStripSeparator4);
            listToosStripS.Add(Form1.Instance.toolStripSeparator5);
            listToosStripS.Add(Form1.Instance.compileStripSeparator1);
            return listToosStripS;
        }
    }
}
