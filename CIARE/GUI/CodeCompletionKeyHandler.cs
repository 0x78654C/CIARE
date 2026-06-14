using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    class CodeCompletionKeyHandler
	{
		MainForm mainForm;
		TextEditorControl editor;
		CodeCompletionWindow codeCompletionWindow;
		System.Windows.Forms.Timer automaticCompletionTimer;
		CancellationTokenSource completionRequestCancellation;
		int completionRequestVersion;
		char pendingAutomaticCompletionKey;
		string pendingDefinitionWord;
		int pendingDefinitionOffset = -1;
		Point pendingDefinitionMouseLocation;
		const int AutomaticCompletionDelayMs = 1; // delay time for autocompletion window.
		static readonly SemaphoreSlim CompletionGenerationLock = new SemaphoreSlim(1, 1);
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
				editor.Disposed += h.EditorDisposed;

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
				if (key == '"' || key == '\'' || StartsSuppressedContext(textArea, key))
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
				CancelPendingCompletionRequest();
				if (IsDotAfterNumericToken(textArea))
				{
					return false;
				}

				BeginCompletionRequest(new CodeCompletionProvider(mainForm), key, false, true);
			}
			else if (IsAutomaticCompletionTrigger(key))
			{
				ScheduleAutomaticCompletionWindow(key);
			}
			else
			{
				CancelPendingCompletionRequest();
			}
			return false;
		}

		void ScheduleAutomaticCompletionWindow(char key)
		{
			pendingAutomaticCompletionKey = key;
			CancelCompletionWorker();
			if (automaticCompletionTimer == null)
			{
				automaticCompletionTimer = new System.Windows.Forms.Timer
				{
					Interval = AutomaticCompletionDelayMs
				};
				automaticCompletionTimer.Tick += AutomaticCompletionTimerTick;
			}

			automaticCompletionTimer.Stop();
			automaticCompletionTimer.Start();
		}

		void AutomaticCompletionTimerTick(object sender, EventArgs e)
		{
			automaticCompletionTimer.Stop();
			ShowAutomaticCompletionWindow(pendingAutomaticCompletionKey);
		}

		void ShowAutomaticCompletionWindow(char key)
		{
			if (editor.IsDisposed || codeCompletionWindow != null ||
				!editor.ActiveTextAreaControl.TextArea.Focused)
			{
				return;
			}

			TextArea textArea = editor.ActiveTextAreaControl.TextArea;
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

			BeginCompletionRequest(new CodeCompletionProvider(mainForm, preSelection), key, true, false);
		}

		void BeginCompletionRequest(CodeCompletionProvider completionDataProvider, char key,
			bool closeWhenCaretAtBeginning, bool dotTypedAfterRequest)
		{
			if (editor.IsDisposed || codeCompletionWindow != null)
				return;

			CodeCompletionProvider.CompletionRequest request =
				completionDataProvider.CaptureCompletionRequest(editor.ActiveTextAreaControl.TextArea, key);
			CancelCompletionWorker();
			var cancellation = new CancellationTokenSource();
			CancellationToken cancellationToken = cancellation.Token;
			completionRequestCancellation = cancellation;
			int requestVersion = ++completionRequestVersion;

			Task.Run(() =>
				{
					if (IsCompletionSuppressedContext(request.RawCode, request.CaretOffset))
						return Array.Empty<ICompletionData>();

					CompletionGenerationLock.Wait(cancellationToken);
					try
					{
						cancellationToken.ThrowIfCancellationRequested();
						ICompletionData[] result = completionDataProvider.GenerateCompletionData(request);
						cancellationToken.ThrowIfCancellationRequested();
						return result;
					}
					finally
					{
						CompletionGenerationLock.Release();
					}
				}, cancellationToken)
				.ContinueWith(task =>
				{
					if (task.IsCanceled || task.IsFaulted || cancellationToken.IsCancellationRequested ||
						editor.IsDisposed || !editor.IsHandleCreated)
					{
						return;
					}

					try
					{
						editor.BeginInvoke(new MethodInvoker(delegate
						{
							CompleteCompletionRequest(completionDataProvider, request, task.Result,
								closeWhenCaretAtBeginning, dotTypedAfterRequest, cancellation,
								cancellationToken, requestVersion);
						}));
					}
					catch
					{
					}
				}, TaskScheduler.Default);
		}

		void CompleteCompletionRequest(CodeCompletionProvider completionDataProvider,
			CodeCompletionProvider.CompletionRequest request, ICompletionData[] completionData,
			bool closeWhenCaretAtBeginning, bool dotTypedAfterRequest,
			CancellationTokenSource cancellation, CancellationToken cancellationToken, int requestVersion)
		{
			try
			{
				if (editor.IsDisposed || cancellationToken.IsCancellationRequested ||
					completionRequestCancellation != cancellation ||
					completionRequestVersion != requestVersion ||
					codeCompletionWindow != null)
				{
					return;
				}

				TextArea textArea = editor.ActiveTextAreaControl.TextArea;
				if (!textArea.Focused)
					return;

				int caretOffset = textArea.Caret.Offset;
				int startOffset;
				if (dotTypedAfterRequest)
				{
					if (caretOffset != request.CaretOffset + 1 ||
						request.CaretOffset >= textArea.Document.TextLength ||
						textArea.Document.GetCharAt(request.CaretOffset) != '.')
					{
						return;
					}

					startOffset = caretOffset;
				}
				else
				{
					string currentWord = GetCurrentWord(textArea);
					if (caretOffset != request.CaretOffset ||
						!string.Equals(currentWord, completionDataProvider.PreSelection, StringComparison.Ordinal))
					{
						return;
					}

					startOffset = Math.Max(0, caretOffset - currentWord.Length);
				}

				codeCompletionWindow = CodeCompletionWindow.ShowCompletionWindow(
					mainForm, editor, completionDataProvider, completionData, startOffset, caretOffset,
					true, true);
				if (codeCompletionWindow != null)
				{
					codeCompletionWindow.CloseWhenCaretAtBeginning = closeWhenCaretAtBeginning;
					codeCompletionWindow.Closed += new EventHandler(CloseCodeCompletionWindow);
				}
			}
			finally
			{
				if (completionRequestCancellation == cancellation)
				{
					completionRequestCancellation = null;
					cancellation.Dispose();
				}
			}
		}

		void CancelPendingCompletionRequest()
		{
			automaticCompletionTimer?.Stop();
			CancelCompletionWorker();
		}

		void CancelCompletionWorker()
		{
			var cancellation = completionRequestCancellation;
			completionRequestCancellation = null;
			if (cancellation != null)
			{
				cancellation.Cancel();
				cancellation.Dispose();
			}
			completionRequestVersion++;
		}

		static bool IsAutomaticCompletionTrigger(char key)
		{
			return char.IsLetterOrDigit(key) || key == '_';
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

		static bool StartsSuppressedContext(TextArea textArea, char key)
		{
			if (key == '#')
				return true;

			int offset = textArea.Caret.Offset;
			return offset > 0 && (key == '/' || key == '*') &&
				textArea.Document.GetCharAt(offset - 1) == '/';
		}

		static bool IsCompletionSuppressedContext(string text, int offset)
		{
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}

			offset = Math.Max(0, Math.Min(offset, text.Length));
			if (IsPreprocessorDirectiveLine(text, offset))
			{
				return true;
			}

            if (TryGetInterpolationSuppression(text, offset, out bool interpolationSuppressed))
            {
                return interpolationSuppressed;
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
				char ch = text[i];
				char next = i + 1 < offset ? text[i + 1] : '\0';

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
					if (ch == '"' && CountConsecutiveQuotes(text, i, offset) >= rawStringQuoteCount)
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
					int quoteCount = CountConsecutiveQuotes(text, i, offset);
					if (quoteCount >= 3)
					{
						inRawString = true;
						rawStringQuoteCount = quoteCount;
						i += quoteCount - 1;
					}
					else
					{
						inVerbatimString = IsVerbatimStringStart(text, i);
						inString = !inVerbatimString;
					}
				}
			}

			return inString || inVerbatimString || inRawString || inChar || inLineComment || inBlockComment;
		}

        static bool TryGetInterpolationSuppression(string text, int offset, out bool suppressed)
        {
            suppressed = false;
            try
            {
                SyntaxNode root = CSharpSyntaxTree.ParseText(text).GetRoot();
                if (root.FullSpan.IsEmpty)
                    return false;

                int position = Math.Max(0, Math.Min(offset - 1, root.FullSpan.End - 1));
                SyntaxToken token = root.FindToken(position, findInsideTrivia: true);
                InterpolationSyntax interpolation = token.Parent?.AncestorsAndSelf()
                    .OfType<InterpolationSyntax>()
                    .FirstOrDefault();
                if (interpolation == null)
                    return false;

                int expressionStart = interpolation.OpenBraceToken.Span.End;
                int expressionEnd = interpolation.CloseBraceToken.IsMissing
                    ? interpolation.FullSpan.End
                    : interpolation.CloseBraceToken.SpanStart;
                if (offset < expressionStart || offset > expressionEnd)
                    return false;

                SyntaxTrivia trivia = root.FindTrivia(position, findInsideTrivia: true);
                suppressed =
                    trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                    token.IsKind(SyntaxKind.StringLiteralToken) ||
                    token.IsKind(SyntaxKind.CharacterLiteralToken) ||
                    token.IsKind(SyntaxKind.InterpolatedStringTextToken);
                return true;
            }
            catch
            {
                return false;
            }
        }
        static bool IsPreprocessorDirectiveLine(string text, int offset)
		{
			if (offset <= 0)
			{
				return false;
			}

			int lineOffset = text.LastIndexOf('\n', offset - 1);
			lineOffset = lineOffset < 0 ? 0 : lineOffset + 1;
			for (int i = lineOffset; i < offset; i++)
			{
				char ch = text[i];
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

		static bool IsVerbatimStringStart(string text, int quoteOffset)
		{
			if (quoteOffset <= 0)
			{
				return false;
			}

			char previous = text[quoteOffset - 1];
			if (previous == '@')
			{
				return true;
			}

			return quoteOffset > 1
				&& previous == '$'
				&& text[quoteOffset - 2] == '@';
		}

		static int CountConsecutiveQuotes(string text, int offset, int maxOffset)
		{
			int count = 0;
			while (offset + count < maxOffset && text[offset + count] == '"')
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

			Keys modifiers = Control.ModifierKeys;
			if (e.Button != MouseButtons.Left ||
				((modifiers & Keys.Control) == 0 && (modifiers & Keys.Shift) == 0))
				return;

			TextArea textArea = editor.ActiveTextAreaControl.TextArea;
			if (!textArea.TextView.DrawingPosition.Contains(e.Location))
				return;

			TextLocation clickPos = textArea.TextView.GetLogicalPosition(
				e.X - textArea.TextView.DrawingPosition.X,
				e.Y - textArea.TextView.DrawingPosition.Y);
			int offset = textArea.Document.PositionToOffset(clickPos);
			int lookupOffset = Math.Max(0, Math.Min(offset, Math.Max(0, textArea.Document.TextLength - 1)));

			int wordStart = ICSharpCode.TextEditor.Document.TextUtilities.FindWordStart(textArea.Document, lookupOffset);
			int wordEnd   = ICSharpCode.TextEditor.Document.TextUtilities.FindWordEnd(textArea.Document, lookupOffset);
			if (wordEnd <= wordStart && lookupOffset > 0)
			{
				lookupOffset--;
				wordStart = ICSharpCode.TextEditor.Document.TextUtilities.FindWordStart(textArea.Document, lookupOffset);
				wordEnd = ICSharpCode.TextEditor.Document.TextUtilities.FindWordEnd(textArea.Document, lookupOffset);
			}
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

		void EditorDisposed(object sender, EventArgs e)
		{
			CancelPendingCompletionRequest();
			if (automaticCompletionTimer != null)
			{
				automaticCompletionTimer.Tick -= AutomaticCompletionTimerTick;
				automaticCompletionTimer.Dispose();
				automaticCompletionTimer = null;
			}

			CloseCodeCompletionWindow(codeCompletionWindow, EventArgs.Empty);
		}
	}
}
