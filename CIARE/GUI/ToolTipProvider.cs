using System;
using System.Text;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Dom.CSharp;
using ICSharpCode.SharpDevelop.Dom.VBNet;
using TextEditor = ICSharpCode.TextEditor;
using NRefactoryResolver = ICSharpCode.SharpDevelop.Dom.NRefactoryResolver.NRefactoryResolver;
using ICSharpCode.NRefactory.Ast;

namespace CIARE.GUI
{
    sealed class ToolTipProvider
    {
        Form1 mainForm;
        TextEditor.TextEditorControl editor;

        private ToolTipProvider(Form1 mainForm, TextEditor.TextEditorControl editor)
        {
            this.mainForm = mainForm;
            this.editor = editor;
        }

        public static void Attach(Form1 mainForm, TextEditor.TextEditorControl editor)
        {
            ToolTipProvider tp = new ToolTipProvider(mainForm, editor);
            editor.ActiveTextAreaControl.TextArea.ToolTipRequest += tp.OnToolTipRequest;
        }

        void OnToolTipRequest(object sender, TextEditor.ToolTipRequestEventArgs e)
        {
            try
            {
                if (e.InDocument && !e.ToolTipShown)
                {
                    IExpressionFinder expressionFinder;
                    if (Form1.IsVisualBasic)
                    {
                        expressionFinder = new VBExpressionFinder();
                    }
                    else
                    {
                        expressionFinder = new CSharpExpressionFinder(Form1.parseInformation);
                    }
                    ExpressionResult expression = expressionFinder.FindFullExpression(
                        editor.Text,
                        editor.Document.PositionToOffset(e.LogicalPosition));
                    if (expression.Region.IsEmpty)
                    {
                        expression.Region = new DomRegion(e.LogicalPosition.Line + 1, e.LogicalPosition.Column + 1);
                    }

                    TextEditor.TextArea textArea = editor.ActiveTextAreaControl.TextArea;
                    NRefactoryResolver resolver = new NRefactoryResolver(Form1.myProjectContent.Language);
                    ResolveResult rr = resolver.Resolve(expression,
                                                        Form1.parseInformation,
                                                        textArea.MotherTextEditorControl.Text);
                    try
                    {
                        string toolTipText = GetText(rr);
                        if (toolTipText != null)
                        {
                            e.ShowToolTip(toolTipText);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        static string GetText(ResolveResult result)
        {
            if (result == null)
            {
                return null;
            }
            if (result is MixedResolveResult)
                return GetText(((MixedResolveResult)result).PrimaryResult);
            IAmbience ambience = Form1.IsVisualBasic ? (IAmbience)new VBNetAmbience() : new CSharpAmbience();
            ambience.ConversionFlags = ConversionFlags.StandardConversionFlags | ConversionFlags.ShowAccessibility;
            if (result is MemberResolveResult)
            {
                return GetMemberText(ambience, ((MemberResolveResult)result).ResolvedMember);
            }
            else if (result is LocalResolveResult)
            {
                LocalResolveResult rr = (LocalResolveResult)result;
                ambience.ConversionFlags = ConversionFlags.UseFullyQualifiedTypeNames
                    | ConversionFlags.ShowReturnType;
                StringBuilder b = new StringBuilder();
                if (rr.IsParameter)
                    b.Append("parameter ");
                else
                    b.Append("local variable ");
                b.Append(ambience.Convert(rr.Field));
                return b.ToString();
            }
            else if (result is NamespaceResolveResult)
            {
                return "namespace " + ((NamespaceResolveResult)result).Name;
            }
            else if (result is TypeResolveResult)
            {
                IClass c = ((TypeResolveResult)result).ResolvedClass;
                if (c != null)
                    return GetMemberText(ambience, c);
                else
                    return ambience.Convert(result.ResolvedType);
            }
            else if (result is MethodGroupResolveResult)
            {
                MethodGroupResolveResult mrr = result as MethodGroupResolveResult;
                IMethod m = mrr.GetMethodIfSingleOverload();
                if (m != null)
                    return GetMemberText(ambience, m);
                else
                    return "Overload of " + ambience.Convert(mrr.ContainingType) + "." + mrr.Name;
            }
            else
            {
                return null;
            }
        }

        static string GetMemberText(IAmbience ambience, IEntity member)
        {
            StringBuilder text = new StringBuilder();
            if (member is IField)
            {
                text.Append(ambience.Convert(member as IField));
            }
            else if (member is IProperty)
            {
                text.Append(ambience.Convert(member as IProperty));
            }
            else if (member is IEvent)
            {
                text.Append(ambience.Convert(member as IEvent));
            }
            else if (member is IMethod)
            {
                text.Append(ambience.Convert(member as IMethod));
            }
            else if (member is IClass)
            {
                text.Append(ambience.Convert(member as IClass));
            }
            else
            {
                text.Append("unknown member ");
                text.Append(member.ToString());
            }
            string documentation = member.Documentation;
            if (documentation != null && documentation.Length > 0)
            {
                text.Append('\n');
                text.Append(CodeCompletionData.XmlDocumentationToText(documentation));
            }
            return text.ToString();
        }
    }
}
