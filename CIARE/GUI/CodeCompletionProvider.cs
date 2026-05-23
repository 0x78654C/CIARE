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
			NRefactoryResolver resolver = new NRefactoryResolver(MainForm.myProjectContent.Language);
			List<ICompletionData> resultList = new List<ICompletionData>();

			if (charTyped == '.')
			{
				Dom.ResolveResult rr = resolver.Resolve(FindExpression(textArea),
														MainForm.parseInformation,
														textArea.MotherTextEditorControl.Text);
				if (rr != null)
				{
					ArrayList completionData = rr.GetCompletionData(MainForm.myProjectContent);
					if (completionData != null)
					{
						AddCompletionData(resultList, completionData);
					}
				}
			}
			else
			{
				Dom.ExpressionResult expression = FindExpression(textArea);
				ArrayList completionData = resolver.CtrlSpace(textArea.Caret.Line + 1,
															  textArea.Caret.Column + 1,
															  MainForm.parseInformation,
															  textArea.MotherTextEditorControl.Text,
															  expression.Context);
				if (completionData != null)
				{
					AddCompletionData(resultList, completionData);
				}
				AddCompletionData(resultList, mainForm.GetWorkspaceMethodCompletionData(preSelection));
				AddDirectTypingKeywords(resultList);
			}
			return resultList.ToArray();
		}

		/// <summary>
		/// Find the expression the cursor is at.
		/// Also determines the context (using statement, "new"-expression etc.) the
		/// cursor is at.
		/// </summary>
		Dom.ExpressionResult FindExpression(TextArea textArea)
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
			Dom.ExpressionResult expression = finder.FindExpression(textArea.Document.TextContent, textArea.Caret.Offset);
			if (expression.Region.IsEmpty)
			{
				expression.Region = new Dom.DomRegion(textArea.Caret.Line + 1, textArea.Caret.Column + 1);
			}
			return expression;
		}

		void AddCompletionData(List<ICompletionData> resultList, ArrayList completionData)
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
					if (ContainsCompletionText(resultList, text))
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

					CodeCompletionData data;
					if (nameDictionary.TryGetValue(m.Name, out data))
					{
						data.AddOverload();
					}
					else
					{
						nameDictionary[m.Name] = data = new CodeCompletionData(m);
						resultList.Add(data);
					}
				}
				else
				{
					// Current ICSharpCode.SharpDevelop.Dom should never return anything else
					throw new NotSupportedException();
				}
			}
		}

		void AddDirectTypingKeywords(List<ICompletionData> resultList)
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
				if (ContainsCompletionText(resultList, keyword))
				{
					continue;
				}
				resultList.Add(new DefaultCompletionData(keyword, "keyword " + keyword, 10));
			}
		}

		static bool ContainsCompletionText(List<ICompletionData> resultList, string text)
		{
			foreach (ICompletionData data in resultList)
			{
				if (string.Equals(data.Text, text, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
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
