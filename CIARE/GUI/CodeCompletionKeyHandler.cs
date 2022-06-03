using System;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace CIARE.GUI
{
	class CodeCompletionKeyHandler
	{
		Form1 mainForm;
		TextEditorControl editor;
		CodeCompletionWindow codeCompletionWindow;

		private CodeCompletionKeyHandler(Form1 mainForm, TextEditorControl editor)
		{
			this.mainForm = mainForm;
			this.editor = editor;
		}

		public static CodeCompletionKeyHandler Attach(Form1 mainForm, TextEditorControl editor)
		{
			CodeCompletionKeyHandler h = new CodeCompletionKeyHandler(mainForm, editor);

			editor.ActiveTextAreaControl.TextArea.KeyEventHandler += h.TextAreaKeyEventHandler;

			// When the editor is disposed, close the code completion window
			editor.Disposed += h.CloseCodeCompletionWindow;

			return h;
		}

		/// <summary>
		/// Return true to handle the keypress, return false to let the text area handle the keypress
		/// </summary>
		bool TextAreaKeyEventHandler(char key)
		{
			if (codeCompletionWindow != null)
			{
				// If completion window is open and wants to handle the key, don't let the text area
				// handle it
				if (codeCompletionWindow.ProcessKeyEvent(key))
					return true;
			}
			if (key == '.')
			{
				ICompletionDataProvider completionDataProvider = new CodeCompletionProvider(mainForm);

				codeCompletionWindow = CodeCompletionWindow.ShowCompletionWindow(
					mainForm,                   // The parent window for the completion window
					editor,                     // The text editor to show the window for
					Form1.DummyFileName,     // Filename - will be passed back to the provider
					completionDataProvider,     // Provider to get the list of possible completions
					key                         // Key pressed - will be passed to the provider
				);
				if (codeCompletionWindow != null)
				{
					// ShowCompletionWindow can return null when the provider returns an empty list
					codeCompletionWindow.Closed += new EventHandler(CloseCodeCompletionWindow);
				}
			}
			return false;
		}

		void CloseCodeCompletionWindow(object sender, EventArgs e)
		{
			if (codeCompletionWindow != null)
			{
				codeCompletionWindow.Closed -= new EventHandler(CloseCodeCompletionWindow);
				codeCompletionWindow.Dispose();
				codeCompletionWindow = null;
			}
		}
	}
}
