using System;
using System.Collections.Generic;
using System.Drawing;
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
		string pendingDefinitionWord;
		int pendingDefinitionOffset = -1;
		Point pendingDefinitionMouseLocation;
		static readonly HashSet<string> DeclarationTypeKeywords = new HashSet<string>(StringComparer.Ordinal)
		{
			"var", "bool", "byte", "sbyte", "char", "decimal", "double", "float",
			"int", "uint", "long", "ulong", "short", "ushort", "object", "string",
			"dynamic", "nint", "nuint",
		};
		static readonly HashSet<string> DeclarationModifiers = new HashSet<string>(StringComparer.Ordinal)
		{
			"public", "private", "protected", "internal", "static", "readonly",
			"volatile", "const", "async", "unsafe", "using", "ref", "out", "in"
		};
		static readonly HashSet<string> NonTypeKeywords = new HashSet<string>(StringComparer.Ordinal)
		{
			"return", "throw", "await", "yield", "new", "if", "else", "for",
			"foreach", "while", "switch", "case", "lock", "fixed", "checked",
			"unchecked", "typeof", "sizeof", "nameof", "default"
		};
		static readonly char[] StatementSeparators = { ';', '{', '}' };

		private CodeCompletionKeyHandler(MainForm mainForm, TextEditorControl editor)
		{
			this.mainForm = mainForm;
			this.editor = editor;
		}

		public static CodeCompletionKeyHandler Attach(MainForm mainForm, TextEditorControl editor)
		{
			CodeCompletionKeyHandler h = new CodeCompletionKeyHandler(mainForm, editor);

			editor.ActiveTextAreaControl.TextArea.KeyEventHandler += h.TextAreaKeyEventHandler;
				editor.ActiveTextAreaControl.TextArea.MouseDown += h.TextAreaMouseDown;
				editor.ActiveTextAreaControl.TextArea.MouseUp += h.TextAreaMouseUp;

				// When the editor is disposed, close the code completion window
				editor.Disposed += h.CloseCodeCompletionWindow;

				return h;
			}

		/// <summary>
		/// Return true to handle the keypress, return false to let the text area handle the keypress
		/// </summary>
		bool TextAreaKeyEventHandler(char key)
		{
			TextArea textArea = editor.ActiveTextAreaControl.TextArea;
			if (codeCompletionWindow != null)
			{
				if (key == '"' || key == '\'' || IsCompletionSuppressedContext(textArea, textArea.Caret.Offset))
				{
					CloseCodeCompletionWindow(codeCompletionWindow, EventArgs.Empty);
					return false;
				}

				// If completion window is open and wants to handle the key, don't let the text area
				// handle it
				if (codeCompletionWindow.ProcessKeyEvent(key))
					return true;
			}
			if (key == '.')
			{
				if (IsCompletionSuppressedContext(textArea, textArea.Caret.Offset) || IsDotAfterNumericToken(textArea))
				{
					return false;
				}

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
			if (IsCompletionSuppressedContext(textArea, textArea.Caret.Offset))
			{
				return;
			}

			string preSelection = GetCurrentWord(textArea);
			if (preSelection.Length == 0)
			{
				return;
			}
			if (IsNumericLikeWord(preSelection))
			{
				return;
			}

			int wordStartOffset = textArea.Caret.Offset - preSelection.Length;
			if (wordStartOffset > 0 && textArea.Document.GetCharAt(wordStartOffset - 1) == '.')
			{
				return;
			}
			if (IsTypingVariableNameBeforeAssignment(textArea, wordStartOffset))
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

		static bool IsCompletionSuppressedContext(TextArea textArea, int offset)
		{
			if (textArea == null || textArea.Document == null)
			{
				return false;
			}

			IDocument document = textArea.Document;
			offset = Math.Max(0, Math.Min(offset, document.TextLength));
			if (IsPreprocessorDirectiveLine(document, offset))
			{
				return true;
			}

			bool inLineComment = false;
			bool inBlockComment = false;
			bool inString = false;
			bool inVerbatimString = false;
			bool inRawString = false;
			int rawStringQuoteCount = 0;
			bool inChar = false;

			for (int i = 0; i < offset; i++)
			{
				char ch = document.GetCharAt(i);
				char next = i + 1 < offset ? document.GetCharAt(i + 1) : '\0';

				if (inLineComment)
				{
					if (ch == '\r' || ch == '\n')
					{
						inLineComment = false;
					}
					continue;
				}

				if (inBlockComment)
				{
					if (ch == '*' && next == '/')
					{
						inBlockComment = false;
						i++;
					}
					continue;
				}

				if (inRawString)
				{
					if (ch == '"' && CountConsecutiveQuotes(document, i, offset) >= rawStringQuoteCount)
					{
						inRawString = false;
						i += rawStringQuoteCount - 1;
					}
					continue;
				}

				if (inVerbatimString)
				{
					if (ch == '"')
					{
						if (next == '"')
						{
							i++;
						}
						else
						{
							inVerbatimString = false;
						}
					}
					continue;
				}

				if (inString)
				{
					if (ch == '\\')
					{
						i++;
						continue;
					}
					if (ch == '"')
					{
						inString = false;
					}
					else if (ch == '\r' || ch == '\n')
					{
						inString = false;
					}
					continue;
				}

				if (inChar)
				{
					if (ch == '\\')
					{
						i++;
						continue;
					}
					if (ch == '\'')
					{
						inChar = false;
					}
					else if (ch == '\r' || ch == '\n')
					{
						inChar = false;
					}
					continue;
				}

				if (ch == '/' && next == '/')
				{
					inLineComment = true;
					i++;
				}
				else if (ch == '/' && next == '*')
				{
					inBlockComment = true;
					i++;
				}
				else if (ch == '\'')
				{
					inChar = true;
				}
				else if (ch == '"')
				{
					int quoteCount = CountConsecutiveQuotes(document, i, offset);
					if (quoteCount >= 3)
					{
						inRawString = true;
						rawStringQuoteCount = quoteCount;
						i += quoteCount - 1;
					}
					else
					{
						inVerbatimString = IsVerbatimStringStart(document, i);
						inString = !inVerbatimString;
					}
				}
			}

			return inString || inVerbatimString || inRawString || inChar || inLineComment || inBlockComment;
		}

		static bool IsPreprocessorDirectiveLine(IDocument document, int offset)
		{
			if (offset <= 0)
			{
				return false;
			}

			LineSegment line = document.GetLineSegmentForOffset(offset);
			int length = Math.Max(0, offset - line.Offset);
			for (int i = 0; i < length; i++)
			{
				char ch = document.GetCharAt(line.Offset + i);
				if (char.IsWhiteSpace(ch))
				{
					continue;
				}

				return ch == '#';
			}

			return false;
		}

		static bool IsDotAfterNumericToken(TextArea textArea)
		{
			int offset = textArea.Caret.Offset;
			if (offset <= 0 || offset > textArea.Document.TextLength)
			{
				return false;
			}
			if (!char.IsDigit(textArea.Document.GetCharAt(offset - 1)))
			{
				return false;
			}

			int startOffset = TextUtilities.FindWordStart(textArea.Document, offset);
			if (startOffset >= offset)
			{
				return false;
			}

			string token = textArea.Document.GetText(startOffset, offset - startOffset);
			return IsNumericLikeWord(token);
		}

		static bool IsNumericLikeWord(string word)
		{
			return word.Length > 0 && char.IsDigit(word[0]);
		}

		static bool IsVerbatimStringStart(IDocument document, int quoteOffset)
		{
			if (quoteOffset <= 0)
			{
				return false;
			}

			char previous = document.GetCharAt(quoteOffset - 1);
			if (previous == '@')
			{
				return true;
			}

			return quoteOffset > 1
				&& previous == '$'
				&& document.GetCharAt(quoteOffset - 2) == '@';
		}

		static int CountConsecutiveQuotes(IDocument document, int offset, int maxOffset)
		{
			int count = 0;
			while (offset + count < maxOffset && document.GetCharAt(offset + count) == '"')
			{
				count++;
			}

			return count;
		}

		static bool IsTypingVariableNameBeforeAssignment(TextArea textArea, int wordStartOffset)
		{
			if (MainForm.IsVisualBasic || wordStartOffset <= 0)
			{
				return false;
			}

			LineSegment line = textArea.Document.GetLineSegmentForOffset(wordStartOffset);
			int prefixLength = wordStartOffset - line.Offset;
			if (prefixLength <= 0)
			{
				return false;
			}

			string beforeWord = textArea.Document.GetText(line.Offset, prefixLength);
			int statementStart = beforeWord.LastIndexOfAny(StatementSeparators);
			string statement = beforeWord.Substring(statementStart + 1).Trim();
			if (statement.Length == 0 || statement.EndsWith(".", StringComparison.Ordinal))
			{
				return false;
			}

			statement = RemoveLeadingDeclarationModifiers(statement);
			if (statement.Length == 0)
			{
				return false;
			}

			int lastComma = LastTopLevelIndexOf(statement, ',');
			int lastEquals = LastTopLevelIndexOf(statement, '=');
			if (lastEquals > lastComma)
			{
				return false;
			}
			if (lastComma >= 0)
			{
				return StartsWithLikelyDeclaration(statement.Substring(0, lastComma));
			}

			return IsLikelyDeclarationType(statement);
		}

		static bool StartsWithLikelyDeclaration(string statement)
		{
			statement = statement.Trim();
			int equals = LastTopLevelIndexOf(statement, '=');
			if (equals >= 0)
			{
				statement = statement.Substring(0, equals).TrimEnd();
			}

			string typePart = RemoveLastIdentifier(statement);
			return typePart.Length > 0 && IsLikelyDeclarationType(typePart);
		}

		static string RemoveLeadingDeclarationModifiers(string statement)
		{
			while (true)
			{
				string firstToken = ReadFirstIdentifier(statement, 0);
				if (firstToken.Length == 0 || !DeclarationModifiers.Contains(firstToken))
				{
					return statement.Trim();
				}

				statement = statement.Substring(firstToken.Length).TrimStart();
			}
		}

		static bool IsLikelyDeclarationType(string typeText)
		{
			typeText = typeText.Trim();
			while (typeText.EndsWith("[]", StringComparison.Ordinal))
			{
				typeText = typeText.Substring(0, typeText.Length - 2).TrimEnd();
			}
			if (typeText.EndsWith("?", StringComparison.Ordinal))
			{
				typeText = typeText.Substring(0, typeText.Length - 1).TrimEnd();
			}

			string lastIdentifier = ReadLastIdentifier(typeText);
			if (lastIdentifier.Length == 0 || NonTypeKeywords.Contains(lastIdentifier))
			{
				return false;
			}
			if (DeclarationTypeKeywords.Contains(lastIdentifier))
			{
				return true;
			}

			return IsSingleIdentifier(typeText)
				|| (typeText.IndexOf('<') >= 0 && typeText.IndexOf('>') > typeText.IndexOf('<'))
				|| char.IsUpper(lastIdentifier[0]);
		}

		static string ReadFirstIdentifier(string text, int offset)
		{
			int index = offset;
			while (index < text.Length && char.IsWhiteSpace(text[index]))
			{
				index++;
			}

			int start = index;
			while (index < text.Length && (char.IsLetterOrDigit(text[index]) || text[index] == '_'))
			{
				index++;
			}

			return index > start ? text.Substring(start, index - start) : string.Empty;
		}

		static string ReadLastIdentifier(string text)
		{
			int index = text.Length - 1;
			while (index >= 0 && !(char.IsLetterOrDigit(text[index]) || text[index] == '_'))
			{
				index--;
			}

			int end = index + 1;
			while (index >= 0 && (char.IsLetterOrDigit(text[index]) || text[index] == '_'))
			{
				index--;
			}

			return end > index + 1 ? text.Substring(index + 1, end - index - 1) : string.Empty;
		}

		static string RemoveLastIdentifier(string text)
		{
			int index = text.Length - 1;
			while (index >= 0 && char.IsWhiteSpace(text[index]))
			{
				index--;
			}
			while (index >= 0 && (char.IsLetterOrDigit(text[index]) || text[index] == '_'))
			{
				index--;
			}

			return index >= 0 ? text.Substring(0, index + 1).Trim() : string.Empty;
		}

		static int LastTopLevelIndexOf(string text, char value)
		{
			int angleDepth = 0;
			int parenDepth = 0;
			int bracketDepth = 0;
			for (int i = text.Length - 1; i >= 0; i--)
			{
				char ch = text[i];
				if (ch == '>')
				{
					angleDepth++;
				}
				else if (ch == '<' && angleDepth > 0)
				{
					angleDepth--;
				}
				else if (ch == ')')
				{
					parenDepth++;
				}
				else if (ch == '(' && parenDepth > 0)
				{
					parenDepth--;
				}
				else if (ch == ']')
				{
					bracketDepth++;
				}
				else if (ch == '[' && bracketDepth > 0)
				{
					bracketDepth--;
				}
				else if (ch == value && angleDepth == 0 && parenDepth == 0 && bracketDepth == 0)
				{
					return i;
				}
			}

			return -1;
		}

		static bool IsSingleIdentifier(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}

			text = text.Trim();
			for (int i = 0; i < text.Length; i++)
			{
				if (!(char.IsLetterOrDigit(text[i]) || text[i] == '_'))
				{
					return false;
				}
			}

			return true;
		}

		void TextAreaMouseDown(object sender, MouseEventArgs e)
		{
			pendingDefinitionWord = null;
			pendingDefinitionOffset = -1;

			if (e.Button != MouseButtons.Left || (Control.ModifierKeys & Keys.Control) == 0)
				return;

			TextArea textArea = editor.ActiveTextAreaControl.TextArea;
			if (!textArea.TextView.DrawingPosition.Contains(e.Location))
				return;

			TextLocation clickPos = textArea.TextView.GetLogicalPosition(
				e.X - textArea.TextView.DrawingPosition.X,
				e.Y - textArea.TextView.DrawingPosition.Y);
			int offset = textArea.Document.PositionToOffset(clickPos);

			int wordStart = ICSharpCode.TextEditor.Document.TextUtilities.FindWordStart(textArea.Document, offset);
			int wordEnd   = ICSharpCode.TextEditor.Document.TextUtilities.FindWordEnd(textArea.Document, offset);
			if (wordEnd <= wordStart) return;

			string word = textArea.Document.GetText(wordStart, wordEnd - wordStart);
			if (string.IsNullOrEmpty(word) || (!char.IsLetter(word[0]) && word[0] != '_'))
				return;

			pendingDefinitionWord = word;
			pendingDefinitionOffset = wordStart;
			pendingDefinitionMouseLocation = e.Location;
		}

		void TextAreaMouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left || pendingDefinitionWord == null || pendingDefinitionOffset < 0)
				return;

			if (Math.Abs(e.X - pendingDefinitionMouseLocation.X) > SystemInformation.DragSize.Width ||
				Math.Abs(e.Y - pendingDefinitionMouseLocation.Y) > SystemInformation.DragSize.Height)
			{
				pendingDefinitionWord = null;
				pendingDefinitionOffset = -1;
				return;
			}

			string word = pendingDefinitionWord;
			int wordStart = pendingDefinitionOffset;
			pendingDefinitionWord = null;
			pendingDefinitionOffset = -1;

			editor.BeginInvoke(new MethodInvoker(delegate
			{
				var (filePath, line) = mainForm.FindDefinition(word, wordStart);
				if (filePath != null)
					mainForm.NavigateToDefinition(filePath, line);
			}));
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
