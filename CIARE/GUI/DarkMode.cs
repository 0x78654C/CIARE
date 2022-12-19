using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    /// <summary>
    /// Dark mode theme class.
    /// </summary>
    public class DarkMode
    {
        /// <summary>
        /// Set dark mode for main form.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="richTextBox"></param>
        /// <param name="groupBox"></param>
        /// <param name="separator1"></param>
        /// <param name="separator2"></param>
        /// <param name="separator3"></param>
        /// <param name="highlight"></param>
        /// <param name="comboBox"></param>
        /// <param name="menuStrip"></param>
        /// <param name="find"></param>
        /// <param name="toolStripMenuList"></param>
        /// <param name="toolStripSeparatorList"></param>
        /// <param name="findButton"></param>
        public static void SetDarkModeMain(Form form, RichTextBox richTextBox, GroupBox groupBox,
            Label separator2, Label separator3, MenuStrip menuStrip,
            List<ToolStripMenuItem> toolStripMenuList, List<ToolStripSeparator> toolStripSeparatorList)
        {
            form.BackColor = Color.FromArgb(51, 51, 51);
            form.ForeColor = Color.FromArgb(192, 215, 207);
            richTextBox.BackColor = Color.FromArgb(30, 30, 30);
            richTextBox.ForeColor = Color.FromArgb(192, 215, 207);
            groupBox.ForeColor = Color.FromArgb(192, 215, 207);
            separator2.ForeColor = Color.FromArgb(192, 215, 207);
            separator3.ForeColor = Color.FromArgb(192, 215, 207);
            menuStrip.BackColor = Color.FromArgb(51, 51, 51);
            menuStrip.ForeColor = Color.FromArgb(192, 215, 207);
            menuStrip.Renderer = new ColorTableSet();
            foreach (var toolStripMenu in toolStripMenuList)
            {
                toolStripMenu.BackColor = Color.FromArgb(51, 51, 51);
                toolStripMenu.ForeColor = Color.FromArgb(192, 215, 207);
            }
            foreach (var toolStripSeparator in toolStripSeparatorList)
            {
                toolStripSeparator.Paint += RenderToolStripSeparator.RenderToolStripSeparator_PaintDark;
            }
        }

        /// <summary>
        /// About form dark mode.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="okButton"></param>
        public static void AboutFormDarkMode(Form form, Button okButton, TextBox aboutMessage, PictureBox pictureBox)
        {
            form.BackColor = Color.FromArgb(51, 51, 51);
            form.ForeColor = Color.FromArgb(192, 215, 207);
            okButton.BackColor = Color.FromArgb(30, 30, 30);
            okButton.ForeColor = Color.FromArgb(192, 215, 207);
            aboutMessage.BackColor = Color.FromArgb(30, 30, 30);
            aboutMessage.ForeColor = Color.FromArgb(192, 215, 207);
            pictureBox.BackColor = Color.FromArgb(51, 51, 51);
        }
        /// <summary>
        /// CMD Line Arguments and Set Binary Name form dark mode.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="okButton"></param>
        /// <param name="cancelButton"></param>
        /// <param name="aboutMessage"></param>
        /// <param name="groupBox"></param>
        public static void CMDLineArgsDarkMode(Form form, Button okButton, Button cancelButton, TextBox aboutMessage, GroupBox groupBox)
        {
            form.BackColor = Color.FromArgb(51, 51, 51);
            form.ForeColor = Color.FromArgb(192, 215, 207);
            okButton.BackColor = Color.FromArgb(30, 30, 30);
            okButton.ForeColor = Color.FromArgb(192, 215, 207);
            cancelButton.BackColor = Color.FromArgb(30, 30, 30);
            cancelButton.ForeColor = Color.FromArgb(192, 215, 207);
            aboutMessage.BackColor = Color.FromArgb(30, 30, 30);
            aboutMessage.ForeColor = Color.FromArgb(192, 215, 207);
            groupBox.ForeColor = Color.FromArgb(192, 215, 207);
        }

        /// <summary>
        /// Find and replace form dark mode.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="replaceBtn"></param>
        /// <param name="replaceAllBtn"></param>
        /// <param name="findWhatTxt"></param>
        /// <param name="replaceWithTxt"></param>
        /// <param name="groupBox"></param>
        /// <param name="ignoreCaseCkb"></param>
        public static void FinAndReplaceDarkMode(Form form, Button replaceBtn, Button replaceAllBtn, TextBox findWhatTxt,
            TextBox replaceWithTxt, GroupBox groupBox, CheckBox ignoreCaseCkb, TabPage findTab, TabPage replaceTab, Button find,TextBox findTxt)
        {
            form.BackColor = Color.FromArgb(51, 51, 51);
            form.ForeColor = Color.FromArgb(192, 215, 207);
            replaceBtn.BackColor = Color.FromArgb(30, 30, 30);
            replaceBtn.ForeColor = Color.FromArgb(192, 215, 207);
            replaceAllBtn.BackColor = Color.FromArgb(30, 30, 30);
            replaceAllBtn.ForeColor = Color.FromArgb(192, 215, 207);
            findWhatTxt.BackColor = Color.FromArgb(30, 30, 30);
            findWhatTxt.ForeColor = Color.FromArgb(192, 215, 207);
            replaceWithTxt.BackColor = Color.FromArgb(30, 30, 30);
            replaceWithTxt.ForeColor = Color.FromArgb(192, 215, 207);
            ignoreCaseCkb.ForeColor = Color.FromArgb(192, 215, 207);
            groupBox.ForeColor = Color.FromArgb(192, 215, 207);
            findTab.BackColor = Color.FromArgb(51, 51, 51);
            findTab.ForeColor = Color.FromArgb(192, 215, 207);
            replaceTab.BackColor = Color.FromArgb(51, 51, 51);
            replaceTab.ForeColor = Color.FromArgb(192, 215, 207);
            findTxt.BackColor = Color.FromArgb(30, 30, 30);
            findTxt.ForeColor = Color.FromArgb(192, 215, 207);
            find.BackColor = Color.FromArgb(30, 30, 30);
            find.ForeColor = Color.FromArgb(192, 215, 207);
        }

        /// <summary>
        /// Options menu dark mode.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="cancelBtn"></param>
        /// <param name="highlightLbl"></param>
        /// <param name="highLightName"></param>
        /// <param name="codeCompletion"></param>
        /// <param name="lineNumber"></param>
        /// <param name="codeFolding"></param>
        /// <param name="displayBox"></param>
        /// <param name="buildBox"></param>
        /// <param name="displaySetting"></param>
        /// <param name="behaveSetting"></param>
        /// <param name="startFile"></param>
        /// <param name="apiLabel"></param>
        /// <param name="apiUrlTxt"></param>
        /// <param name="saveApiBtn"></param>
        /// <param name="liveShare"></param>
        public static void OptionsDarkMode(Form form, Button cancelBtn, Label highlightLbl, ComboBox highLightName,
            CheckBox codeCompletion, CheckBox lineNumber, CheckBox codeFolding, GroupBox displayBox, GroupBox buildBox, Label displaySetting ,Label behaveSetting, CheckBox startFile,
            Label apiLabel, TextBox apiUrlTxt, Button saveApiBtn, GroupBox liveShare, GroupBox openAIgrp,Label apiAiLbl,Label tokenLbl, TextBox apiAIKey, TextBox maxTokens, Button saveAibtn)
        {
            form.BackColor = Color.FromArgb(51, 51, 51);
            form.ForeColor = Color.FromArgb(192, 215, 207);
            cancelBtn.BackColor = Color.FromArgb(30, 30, 30);
            cancelBtn.ForeColor = Color.FromArgb(192, 215, 207);
            highlightLbl.ForeColor = Color.FromArgb(192, 215, 207);
            highLightName.BackColor = Color.FromArgb(30, 30, 30);
            highLightName.ForeColor = Color.FromArgb(192, 215, 207);
            codeCompletion.ForeColor = Color.FromArgb(192, 215, 207);
            lineNumber.ForeColor = Color.FromArgb(192, 215, 207);
            codeFolding.ForeColor = Color.FromArgb(192, 215, 207);
            displayBox.ForeColor = Color.FromArgb(192, 215, 207);
            buildBox.ForeColor = Color.FromArgb(192, 215, 207);
            displaySetting.ForeColor = Color.FromArgb(192, 215, 207);
            behaveSetting.ForeColor = Color.FromArgb(192, 215, 207);
            startFile.ForeColor = Color.FromArgb(192, 215, 207);
            saveApiBtn.BackColor = Color.FromArgb(30, 30, 30);
            saveApiBtn.ForeColor = Color.FromArgb(192, 215, 207);
            apiUrlTxt.ForeColor = Color.FromArgb(192, 215, 207);
            apiUrlTxt.BackColor = Color.FromArgb(30, 30, 30);
            apiLabel.ForeColor = Color.FromArgb(192, 215, 207);
            liveShare.ForeColor = Color.FromArgb(192, 215, 207);
            openAIgrp.ForeColor = Color.FromArgb(192, 215, 207);
            apiAiLbl.ForeColor = Color.FromArgb(192, 215, 207);
            tokenLbl.ForeColor = Color.FromArgb(192, 215, 207);
            apiAIKey.ForeColor = Color.FromArgb(192, 215, 207);
            apiAIKey.BackColor = Color.FromArgb(30, 30, 30);
            maxTokens.ForeColor = Color.FromArgb(192, 215, 207);
            maxTokens.BackColor = Color.FromArgb(30, 30, 30);
            saveAibtn.BackColor = Color.FromArgb(30, 30, 30);
            saveAibtn.ForeColor = Color.FromArgb(192, 215, 207);
        }
      
        /// <summary>
        /// Live Share dark mode.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="hostGrp"></param>
        /// <param name="sessionLbl"></param>
        /// <param name="sessionId"></param>
        /// <param name="passwordLbl"></param>
        /// <param name="password"></param>
        /// <param name="startHost"></param>
        /// <param name="remoteGrp"></param>
        /// <param name="remoteSessionLbl"></param>
        /// <param name="remoteSessionId"></param>
        /// <param name="remotePasswordLbl"></param>
        /// <param name="remotePasswordTxt"></param>
        /// <param name="remoteConnect"></param>
        public static void LiveShareDarkMode(Form form, GroupBox hostGrp, Label sessionLbl, TextBox sessionId, Label passwordLbl, TextBox password, Button startHost,
            GroupBox remoteGrp, Label remoteSessionLbl, TextBox remoteSessionId, Label remotePasswordLbl, TextBox remotePasswordTxt, Button remoteConnect)
        {
            form.BackColor = Color.FromArgb(51, 51, 51);
            form.ForeColor = Color.FromArgb(192, 215, 207);
            hostGrp.ForeColor = Color.FromArgb(192, 215, 207);
            sessionLbl.ForeColor = Color.FromArgb(192, 215, 207);
            sessionId.ForeColor = Color.FromArgb(192, 215, 207);
            sessionId.BackColor = Color.FromArgb(30, 30, 30);
            passwordLbl.ForeColor = Color.FromArgb(192, 215, 207);
            password.ForeColor = Color.FromArgb(192, 215, 207);
            password.BackColor = Color.FromArgb(30, 30, 30);
            startHost.BackColor = Color.FromArgb(30, 30, 30);
            startHost.ForeColor = Color.FromArgb(192, 215, 207);
            remoteGrp.ForeColor = Color.FromArgb(192, 215, 207);
            remoteSessionLbl.ForeColor = Color.FromArgb(192, 215, 207);
            remoteSessionId.ForeColor = Color.FromArgb(192, 215, 207);
            remoteSessionId.BackColor = Color.FromArgb(30, 30, 30);
            remotePasswordLbl.ForeColor = Color.FromArgb(192, 215, 207);
            remotePasswordTxt.ForeColor = Color.FromArgb(192, 215, 207);
            remotePasswordTxt.BackColor = Color.FromArgb(30, 30, 30);
            remoteConnect.BackColor = Color.FromArgb(30, 30, 30);
            remoteConnect.ForeColor = Color.FromArgb(192, 215, 207);
        }

        /// <summary>
        /// Api url check form dark mode.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="groupBox"></param>
        /// <param name="label"></param>
        /// <param name="textBox"></param>
        /// <param name="button"></param>
        public static void ApiUrlCheckDarkMode(Form form,GroupBox groupBox, Label label,TextBox textBox, Button button)
        {
            form.BackColor = Color.FromArgb(51, 51, 51);
            form.ForeColor = Color.FromArgb(192, 215, 207);
            groupBox.ForeColor = Color.FromArgb(192, 215, 207);
            label.ForeColor = Color.FromArgb(192, 215, 207);
            textBox.ForeColor = Color.FromArgb(192, 215, 207);
            textBox.BackColor = Color.FromArgb(30, 30, 30);
            button.BackColor = Color.FromArgb(30, 30, 30);
            button.ForeColor = Color.FromArgb(192, 215, 207);
        }
    }
}