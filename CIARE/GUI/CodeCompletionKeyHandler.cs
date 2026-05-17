using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    class CodeCompletionKeyHandler
	{
        MainForm mainForm;
		TextEditorControl editor;
		CodeCompletionWindow codeCompletionWindow;

		private CodeCompletionKeyHandler(MainForm mainForm, TextEditorControl editor)
		{
			this.mainForm = mainForm;
			this.editor = editor;
		}

		public static CodeCompletionKeyHandler Attach(MainForm mainForm, TextEditorControl editor)
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
				ShowCodeCompletionWindow(new CodeCompletionProvider(mainForm), key, false);
			}
			else if (IsAutomaticCompletionTrigger(key))
			{
				editor.BeginInvoke(new MethodInvoker(delegate { ShowAutomaticCompletionWindow(key); }));
			}
			return false;
		}

		void ShowAutomaticCompletionWindow(char key)
		{
			if (editor.IsDisposed || codeCompletionWindow != null)
			{
				return;
			}

			TextArea textArea = editor.ActiveTextAreaControl.TextArea;
			string preSelection = GetCurrentWord(textArea);
			if (preSelection.Length == 0)
			{
				return;
			}

			int wordStartOffset = textArea.Caret.Offset - preSelection.Length;
			if (wordStartOffset > 0 && textArea.Document.GetCharAt(wordStartOffset - 1) == '.')
			{
				return;
			}

			ShowCodeCompletionWindow(new CodeCompletionProvider(mainForm, preSelection), key, true);
		}

		void ShowCodeCompletionWindow(ICompletionDataProvider completionDataProvider, char key, bool closeWhenCaretAtBeginning)
		{
			codeCompletionWindow = CodeCompletionWindow.ShowCompletionWindow(
				mainForm,                   // The parent window for the completion window
				editor,                     // The text editor to show the window for
				MainForm.DummyFileName,     // Filename - will be passed back to the provider
				completionDataProvider,     // Provider to get the list of possible completions
				key                         // Key pressed - will be passed to the provider
			);
			if (codeCompletionWindow != null)
			{
				// ShowCompletionWindow can return null when the provider returns an empty list
				codeCompletionWindow.CloseWhenCaretAtBeginning = closeWhenCaretAtBeginning;
				codeCompletionWindow.Closed += new EventHandler(CloseCodeCompletionWindow);
			}
		}

		static bool IsAutomaticCompletionTrigger(char key)
		{
			return char.IsLetter(key) || key == '_';
		}

		static string GetCurrentWord(TextArea textArea)
		{
			int offset = textArea.Caret.Offset;
			int startOffset = TextUtilities.FindWordStart(textArea.Document, offset);
			if (startOffset >= offset)
			{
				return string.Empty;
			}
			return textArea.Document.GetText(startOffset, offset - startOffset);
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
