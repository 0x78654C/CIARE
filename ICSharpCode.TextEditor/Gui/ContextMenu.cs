using ICSharpCode.TextEditor.Actions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor
{
    public partial class ContextMenu : ContextMenuStrip
    {
        TextAreaControl parent;

        /// <summary>
        /// When set, "Ask AI" is shown in the context menu and this action is invoked on click.
        /// </summary>
        public Action AskAIAction { get; set; }

        public ContextMenu(TextAreaControl parent)
        {
            this.parent = parent;
            InitializeComponent();

            undo.Click += OnClickUndo;
            cut.Click += OnClickCut;
            copy.Click += OnClickCopy;
            paste.Click += OnClickPaste;
            selectAll.Click += OnSelectAll;
            askAI.Click += OnClickAskAI;
        }

        void OnClickCut(object sender, EventArgs e)
        {
            new Cut().Execute(parent.TextArea);
            parent.TextArea.Focus();
        }

        void OnClickUndo(object sender, EventArgs e)
        {
            parent.Undo();
            parent.TextArea.Focus();
        }

        void OnClickCopy(object sender, EventArgs e)
        {
            new Copy().Execute(parent.TextArea);
            parent.TextArea.Focus();
        }

        void OnClickPaste(object sender, EventArgs e)
        {
            new Paste().Execute(parent.TextArea);
            parent.TextArea.Focus();
        }

        void OnSelectAll(object sender, EventArgs e)
        {
            new SelectWholeDocument().Execute(parent.TextArea);
            parent.TextArea.Focus();
        }

        void OnClickAskAI(object sender, EventArgs e)
        {
            AskAIAction?.Invoke();
        }

        void OnOpening(object sender, CancelEventArgs e)
        {
            undo.Enabled = parent.Document.UndoStack.CanUndo;
            cut.Enabled = copy.Enabled = delete.Enabled = parent.SelectionManager.HasSomethingSelected;
            paste.Enabled = parent.TextArea.ClipboardHandler.EnablePaste;
            selectAll.Enabled = !string.IsNullOrEmpty(parent.Document.TextContent);
            bool showAI = AskAIAction != null;
            askAI.Visible = showAI;
            aiSeparator.Visible = showAI;
        }

    }
}
