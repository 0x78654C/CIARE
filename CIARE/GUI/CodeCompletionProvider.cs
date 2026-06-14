using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

using Dom = ICSharpCode.SharpDevelop.Dom;
using NRefactoryResolver = ICSharpCode.SharpDevelop.Dom.NRefactoryResolver.NRefactoryResolver;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
	class CodeCompletionProvider : ICompletionDataProvider
	{
		MainForm mainForm;
		readonly string preSelection;
		internal sealed class CompletionRequest
		{
			public string RawCode;
			public string CurrentFilePath;
			public int CaretOffset;
			public int CaretLine;
			public int CaretColumn;
			public char CharacterTyped;
			public bool UsingDirectiveDotCompletion;
		}

		static readonly string[] DirectTypingKeywords = {
			"abstract", "as", "async", "await", "base", "bool", "break", "byte",
			"case", "catch", "char", "checked", "class", "const", "continue",
			"decimal", "default", "delegate", "do", "double", "else", "enum",
			"event", "explicit", "extern", "false", "finally", "fixed", "float",
			"for", "foreach", "get", "goto", "if", "implicit", "in", "init",
			"int", "interface", "internal", "is", "lock", "long", "namespace",
			"new", "null", "object", "operator", "out", "override", "params",
			"partial", "private", "protected", "public", "readonly", "record",
			"ref", "return", "sbyte", "sealed", "set", "short", "sizeof",
			"stackalloc", "static", "string", "struct", "switch", "this",
			"throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
			"unsafe", "ushort", "using", "var", "virtual", "void", "volatile",
			"while", "where", "yield"
		};

		public CodeCompletionProvider(MainForm mainForm) : this(mainForm, null)
		{
		}

		public CodeCompletionProvider(MainForm mainForm, string preSelection)
		{
			this.mainForm = mainForm;
			this.preSelection = preSelection;
		}

		public ImageList ImageList
		{
			get
			{
				return MainForm.Instance.imageList1;
			}
		}

		public string PreSelection
		{
			get
			{
				return preSelection;
			}
		}

		public int DefaultIndex
		{
			get
			{
				return -1;
			}
		}

		public CompletionDataProviderKeyResult ProcessKey(char key)
		{
			if (char.IsLetterOrDigit(key) || key == '_')
			{
				return CompletionDataProviderKeyResult.NormalKey;
			}
			else
			{
				// key triggers insertion of selected items
				return CompletionDataProviderKeyResult.InsertionKey;
			}
		}

		/// <summary>
		/// Called when entry should be inserted. Forward to the insertion action of the completion data.
		/// </summary>
		public bool InsertAction(ICompletionData data, TextArea textArea, int insertionOffset, char key)
		{
			textArea.Caret.Position = textArea.Document.OffsetToPosition(insertionOffset);
			return data.InsertAction(textArea, key);
		}

		public ICompletionData[] GenerateCompletionData(string fileName, TextArea textArea, char charTyped)
		{
			return GenerateCompletionData(CaptureCompletionRequest(textArea, charTyped));
		}

		internal CompletionRequest CaptureCompletionRequest(TextArea textArea, char charTyped)
		{
			return new CompletionRequest
			{
				RawCode = textArea.MotherTextEditorControl.Text,
				CurrentFilePath = mainForm.GetActiveEditorFilePathForCompletion(),
				CaretOffset = textArea.Caret.Offset,
				CaretLine = textArea.Caret.Line,
				CaretColumn = textArea.Caret.Column,
				CharacterTyped = charTyped,
				UsingDirectiveDotCompletion = charTyped == '.' && IsUsingDirectiveDotCompletion(textArea)
			};
		}

		internal ICompletionData[] GenerateCompletionData(CompletionRequest request)
		{
			string rawCode = request.RawCode;
			Dom.ExpressionResult expression = FindExpression(rawCode, request.CaretOffset,
				request.CaretLine, request.CaretColumn);
			if (!request.UsingDirectiveDotCompletion)
			{
				mainForm.RefreshActiveCompletionUnit(rawCode, request.CurrentFilePath);
			}
			mainForm.ResetCompletionWorkspaceIfInactive(request.CurrentFilePath);
			NRefactoryResolver resolver = new NRefactoryResolver(MainForm.myProjectContent.Language);
			List<ICompletionData> resultList = new List<ICompletionData>();
			HashSet<string> resultTexts = new HashSet<string>(StringComparer.Ordinal);

				// For top-level statement files the code has no class/method context, so the
				// NRefactory resolver cannot find a scope.  Wrap the code before passing it to
				// the resolver and adjust the caret line accordingly.
				string resolverCode = mainForm.PrepareCodeForNRefactoryCompletion(rawCode, request.CurrentFilePath,
					out int prefixLineOffset, out int wrapLineOffset, out int bodyStartLine);
				int caretLine = request.CaretLine + 1 + prefixLineOffset;
				int caretCol  = request.CaretColumn + 1;
				if (wrapLineOffset > 0 && caretLine >= bodyStartLine)
					caretLine += wrapLineOffset;

				if (request.CharacterTyped == '.')
				{
					int expressionBeginLine = expression.Region.IsEmpty
						? 0
						: expression.Region.BeginLine + prefixLineOffset;
					if (wrapLineOffset > 0 && !expression.Region.IsEmpty && expressionBeginLine >= bodyStartLine)
					{
						int adjustedBegin = expressionBeginLine + wrapLineOffset;
						expression.Region = expression.Region.EndLine == -1
							? new Dom.DomRegion(adjustedBegin, expression.Region.BeginColumn)
							: new Dom.DomRegion(adjustedBegin, expression.Region.BeginColumn,
								expression.Region.EndLine + prefixLineOffset + wrapLineOffset, expression.Region.EndColumn);
					}
					else if (prefixLineOffset > 0 && !expression.Region.IsEmpty)
					{
						int adjustedBegin = expressionBeginLine;
						expression.Region = expression.Region.EndLine == -1
							? new Dom.DomRegion(adjustedBegin, expression.Region.BeginColumn)
							: new Dom.DomRegion(adjustedBegin, expression.Region.BeginColumn,
								expression.Region.EndLine + prefixLineOffset, expression.Region.EndColumn);
					}
					Dom.ResolveResult rr = resolver.Resolve(expression,
															MainForm.parseInformation,
															resolverCode);
					ArrayList completionData = null;
					if (rr != null)
					{
						completionData = rr.GetCompletionData(MainForm.myProjectContent);
					}

					completionData = mainForm.FilterCompletionDataForActiveProject(completionData,
						request.CurrentFilePath);
					completionData = MergeCompletionData(completionData, mainForm.GetWorkspaceMemberCompletionData(expression.Expression));
					if (ShouldUseRoslynMemberFallback(completionData, request.UsingDirectiveDotCompletion))
					{
						completionData = MergeCompletionData(completionData,
							mainForm.GetRoslynMemberCompletionData(rawCode, request.CaretOffset,
								expression.Expression, request.CurrentFilePath));
					}
					if (completionData != null)
						AddCompletionData(resultList, resultTexts, completionData);
				}
				else
				{
					ArrayList completionData = resolver.CtrlSpace(caretLine,
																  caretCol,
																  MainForm.parseInformation,
																  resolverCode,
																  expression.Context);
					if (completionData != null)
						completionData = mainForm.FilterCompletionDataForActiveProject(completionData,
							request.CurrentFilePath);
					completionData = MergeCompletionData(completionData,
						mainForm.GetRoslynCtrlSpaceCompletionData(rawCode, request.CaretOffset, preSelection,
							request.CurrentFilePath));
					if (completionData != null)
						AddCompletionData(resultList, resultTexts, completionData);
					AddCompletionData(resultList, resultTexts,
						mainForm.GetWorkspaceMethodCompletionData(preSelection));
					AddDirectTypingKeywords(resultList, resultTexts);
				}
				return resultList.ToArray();
			}

		static bool IsUsingDirectiveDotCompletion(TextArea textArea)
		{
			if (textArea == null || textArea.Document == null)
				return false;

			try
			{
				var line = textArea.Document.GetLineSegment(textArea.Caret.Line);
				int column = Math.Max(0, Math.Min(textArea.Caret.Column, line.Length));
				string textBeforeCaret = textArea.Document.GetText(line.Offset, column).TrimStart();
				if (textBeforeCaret.StartsWith("global ", StringComparison.Ordinal))
					textBeforeCaret = textBeforeCaret.Substring("global ".Length).TrimStart();

				return (textBeforeCaret.StartsWith("using ", StringComparison.Ordinal) ||
					textBeforeCaret.StartsWith("using static ", StringComparison.Ordinal)) &&
					!textBeforeCaret.StartsWith("using (", StringComparison.Ordinal) &&
					textBeforeCaret.IndexOf('=') < 0;
			}
			catch
			{
				return false;
			}
		}

		static bool ShouldUseRoslynMemberFallback(ArrayList completionData, bool usingDirectiveDotCompletion)
		{
			if (usingDirectiveDotCompletion)
				return false;

			if (completionData == null || completionData.Count == 0)
				return true;

			return !ContainsNamespaceCompletionData(completionData);
		}

		static bool ContainsNamespaceCompletionData(ArrayList completionData)
		{
			foreach (object item in completionData)
			{
				if (item is string)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Find the expression the cursor is at.
		/// Also determines the context (using statement, "new"-expression etc.) the
		/// cursor is at.
		/// </summary>
		Dom.ExpressionResult FindExpression(string code, int caretOffset, int caretLine, int caretColumn)
		{
			Dom.IExpressionFinder finder;
			if (MainForm.IsVisualBasic)
			{
				finder = new Dom.VBNet.VBExpressionFinder();
			}
			else
			{
				finder = new Dom.CSharp.CSharpExpressionFinder(MainForm.parseInformation);
			}
			Dom.ExpressionResult expression = finder.FindExpression(code, caretOffset);
			if (expression.Region.IsEmpty)
			{
				expression.Region = new Dom.DomRegion(caretLine + 1, caretColumn + 1);
			}
			return expression;
		}

		void AddCompletionData(List<ICompletionData> resultList, HashSet<string> resultTexts,
			ArrayList completionData)
		{
			// used to store the method names for grouping overloads
			Dictionary<string, CodeCompletionData> nameDictionary = new Dictionary<string, CodeCompletionData>();

			// Add the completion data as returned by SharpDevelop.Dom to the
			// list for the text editor
			foreach (object obj in completionData)
			{
				if (obj is string)
				{
					string text = (string)obj;
					if (!resultTexts.Add(text))
					{
						continue;
					}

					// namespace names and keyword-code-completion entries are both returned as string
					string description = IsCSharpKeyword(text) ? "keyword " + text : "namespace " + text;
					resultList.Add(new DefaultCompletionData(text, description, 5));
				}
				else if (obj is Dom.IClass)
				{
					Dom.IClass c = (Dom.IClass)obj;
						if (resultTexts.Add(c.Name))
							resultList.Add(new CodeCompletionData(c));
					}
					else if (obj is Dom.IMember)
					{
						Dom.IMember m = (Dom.IMember)obj;
						if (m is Dom.IMethod && ((m as Dom.IMethod).IsConstructor))
						{
							// Skip constructors
							continue;
						}
						// Group results by name and add "(x Overloads)" to the
						// description if there are multiple results with the same name.
						// Also guard against duplicates introduced by a previous AddCompletionData call.

						CodeCompletionData data;
						if (nameDictionary.TryGetValue(m.Name, out data))
						{
							data.AddOverload();
						}
						else if (resultTexts.Add(m.Name))
						{
							nameDictionary[m.Name] = data = new CodeCompletionData(m);
							resultList.Add(data);
						}
					}
				else if (obj is ICompletionData)
				{
					ICompletionData data = (ICompletionData)obj;
					if (resultTexts.Add(data.Text))
						resultList.Add(data);
				}
				else
				{
					// Current ICSharpCode.SharpDevelop.Dom should never return anything else
					throw new NotSupportedException();
				}
			}
		}

		static ArrayList MergeCompletionData(ArrayList primary, ArrayList fallback)
		{
			if (fallback == null || fallback.Count == 0)
				return primary;
			if (primary == null || primary.Count == 0)
				return fallback;

			ArrayList merged = new ArrayList(primary);
			HashSet<string> seenTexts = new HashSet<string>(StringComparer.Ordinal);
			foreach (object item in primary)
			{
				string text = GetCompletionObjectText(item);
				if (text != null)
					seenTexts.Add(text);
			}

			foreach (object item in fallback)
			{
				string text = GetCompletionObjectText(item);
				if ((text == null && !merged.Contains(item)) || (text != null && seenTexts.Add(text)))
					merged.Add(item);
			}
			return merged;
		}

		static string GetCompletionObjectText(object item)
		{
			if (item is string)
				return (string)item;
			if (item is ICompletionData)
				return ((ICompletionData)item).Text;
			if (item is Dom.IClass)
				return ((Dom.IClass)item).Name;
			if (item is Dom.IMember)
				return ((Dom.IMember)item).Name;
			return null;
		}

		void AddDirectTypingKeywords(List<ICompletionData> resultList, HashSet<string> resultTexts)
		{
			if (MainForm.IsVisualBasic)
			{
				return;
			}

			string prefix = preSelection ?? string.Empty;
			foreach (string keyword in DirectTypingKeywords)
			{
				if (prefix.Length > 0 && !keyword.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (!resultTexts.Add(keyword))
				{
					continue;
				}
				resultList.Add(new DefaultCompletionData(keyword, "keyword " + keyword, 10));
			}
		}

		static bool IsCSharpKeyword(string text)
		{
			foreach (string keyword in DirectTypingKeywords)
			{
				if (string.Equals(keyword, text, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}
	}
}
