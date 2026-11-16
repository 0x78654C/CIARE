using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CIARE.Designer;

// Reads declarations and literal designer assignments. User constructors and resources are never executed.
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class FormSource
{
    internal string Text { get; }
    internal DesignDocument Document { get; }
    internal ClassDeclarationSyntax Class { get; }
    internal MethodDeclarationSyntax Initialize { get; }
    internal HashSet<string> MemberNames { get; } = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DesignNode> _members = new(StringComparer.Ordinal);
    internal static string DesignerPath(string path) => Path.ChangeExtension(path, ".Designer.cs");
    internal static string CodePath(string path) => path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
        ? path[..^12] + ".cs" : Path.ChangeExtension(path, ".cs");

    internal FormSource(string text)
    {
        Text = text;
        var syntax = CSharpSyntaxTree.ParseText(text);
        if (syntax.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            throw new InvalidDataException("Fix the syntax errors in the designer source before opening Design view.");
        var candidates = syntax.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.ValueText == "InitializeComponent" && m.Body != null).ToArray();
        if (candidates.Length != 1) throw new InvalidDataException("Expected one InitializeComponent method in the form's designer file.");
        Initialize = candidates[0]; Class = (ClassDeclarationSyntax)Initialize.Parent;
        Document = new DesignDocument();
        var root = Document.Form;
        root.Id = "$this"; root.SourceMember = "this"; root.Name = Class.Identifier.ValueText; root.Text = root.Name;
        root.Namespace = string.Join(".", Class.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n => n.Name.ToString()));
        _members["this"] = root;
        var types = Class.Members.OfType<FieldDeclarationSyntax>().SelectMany(f => f.Declaration.Variables.Select(v => (v.Identifier.ValueText, Type: f.Declaration.Type.ToString())))
            .ToDictionary(x => x.ValueText, x => x.Type, StringComparer.Ordinal);
        foreach (var name in types.Keys) MemberNames.Add(name);
        var assignments = Initialize.Body.Statements.OfType<ExpressionStatementSyntax>().Select(s => s.Expression).OfType<AssignmentExpressionSyntax>().ToArray();
        foreach (var assignment in assignments)
        {
            string member = Member(assignment.Left);
            if (member.Contains('.') || member == "this") continue;
            string type = assignment.Right switch
            {
                ObjectCreationExpressionSyntax creation => creation.Type.ToString(),
                ImplicitObjectCreationExpressionSyntax when types.TryGetValue(member, out var declared) => declared,
                _ => null
            };
            if (type == null) continue;
            string kind = type.Replace("global::", "").Split('.').Last();
            bool supported = DesignCatalog.Controls.Any(c => c.Kind == kind);
            var node = DesignCatalog.Create(supported ? kind : "Panel", member, new Point(0, 0));
            node.Id = member; node.SourceMember = member; node.SourceType = supported ? "" : kind;
            node.Text = "";
            _members[member] = node;
            MemberNames.Add(member);
        }
        // Assignments may precede or follow parenting calls in generated code.
        foreach (var assignment in assignments)
        {
            if (!TryProperty(assignment.Left, out string member, out string property)) continue;
            if (!_members.TryGetValue(member, out var node)) continue;
            if (assignment.IsKind(SyntaxKind.AddAssignmentExpression))
            {
                string handler = assignment.Right is ObjectCreationExpressionSyntax handlerCreation
                    ? Member(handlerCreation.ArgumentList?.Arguments.FirstOrDefault()?.Expression) : Member(assignment.Right);
                if (DesignCatalog.EventsFor(node.Kind).Any(e => e.Name == property) && DesignDocument.IsIdentifier(handler)) node.Events[property] = handler;
                continue;
            }
            Apply(node, property, assignment.Right);
        }
        foreach (var call in Initialize.Body.Statements.OfType<ExpressionStatementSyntax>().Select(s => s.Expression).OfType<InvocationExpressionSyntax>())
        {
            if (call.Expression is not MemberAccessExpressionSyntax method) continue;
            string methodPath = Member(method.Expression);
            if (methodPath.EndsWith(".Controls", StringComparison.Ordinal) || methodPath == "Controls")
            {
                string owner = methodPath == "Controls" ? "this" : methodPath[..^9];
                var parent = ResolveContainer(owner);
                if (parent == null) continue;
                IEnumerable<ExpressionSyntax> children = method.Name.Identifier.ValueText switch
                {
                    "Add" => call.ArgumentList.Arguments.Select(a => a.Expression),
                    "AddRange" => call.ArgumentList.Arguments.SelectMany(a => ArrayValues(a.Expression)),
                    _ => Array.Empty<ExpressionSyntax>()
                };
                foreach (var childExpression in children)
                    if (_members.TryGetValue(Member(childExpression), out var child) && child != root && child != parent)
                    {
                        foreach (var n in _members.Values) n.Children.Remove(child);
                        if (child.Kind == "TabPage" && parent.SourceType.Length > 0) parent.Kind = "TabControl";
                        if (!DesignDocument.Enumerate(child).Contains(parent)) parent.Children.Add(child);
                    }
            }
            else if (methodPath.EndsWith(".Items", StringComparison.Ordinal) && method.Name.Identifier.ValueText == "AddRange" &&
                _members.TryGetValue(methodPath[..^6], out var owner) && DesignCatalog.HasItems(owner.Kind))
            {
                owner.Items = call.ArgumentList.Arguments.SelectMany(a => ArrayValues(a.Expression))
                    .OfType<LiteralExpressionSyntax>().Where(l => l.IsKind(SyntaxKind.StringLiteralExpression)).Select(l => l.Token.ValueText).ToArray();
            }
        }
        // Designer-created controls without a Controls.Add are nonvisual components; preserve their source unchanged.
        Document.Validate();
    }
    private DesignNode ResolveContainer(string member)
    {
        if (_members.TryGetValue(member, out var node)) return node.IsContainer ? node : null;
        int dot = member.LastIndexOf('.');
        if (dot < 0 || member[(dot + 1)..] is not ("Panel1" or "Panel2") || !_members.TryGetValue(member[..dot], out var split)) return null;
        node = DesignCatalog.Create("Panel", member.Replace('.', '_'), Point.Empty);
        node.Id = node.SourceMember = member;
        bool first = member.EndsWith("Panel1", StringComparison.Ordinal);
        bool horizontal = Initialize.Body.Statements.OfType<ExpressionStatementSyntax>().Any(s => s.Expression is AssignmentExpressionSyntax a &&
            Member(a.Left) == member[..dot] + ".Orientation" && a.Right.ToString().EndsWith("Horizontal", StringComparison.Ordinal));
        var distance = Initialize.Body.Statements.OfType<ExpressionStatementSyntax>().Select(s => s.Expression).OfType<AssignmentExpressionSyntax>()
            .LastOrDefault(a => Member(a.Left) == member[..dot] + ".SplitterDistance");
        int position = distance != null && Number(distance.Right, out double value) ? (int)value : (horizontal ? split.Height : split.Width) / 2;
        position = Math.Clamp(position, 1, Math.Max(1, (horizontal ? split.Height : split.Width) - 5));
        node.Size = horizontal ? new Size(split.Width, first ? position : Math.Max(1, split.Height - position - 4))
            : new Size(first ? position : Math.Max(1, split.Width - position - 4), split.Height);
        node.Location = first ? Point.Empty : horizontal ? new Point(0, position + 4) : new Point(position + 4, 0);
        split.Children.Add(node); _members[member] = node;
        return node;
    }
    internal static string Member(ExpressionSyntax expression)
    {
        string text = expression?.ToString().Replace("global::", "") ?? "";
        return text.StartsWith("this.", StringComparison.Ordinal) ? text[5..] : text;
    }
    internal static bool TryProperty(ExpressionSyntax expression, out string member, out string property)
    {
        if (expression is MemberAccessExpressionSyntax access)
        { member = Member(access.Expression); property = access.Name.Identifier.ValueText; return true; }
        member = "this"; property = Member(expression); return true;
    }
    internal static IEnumerable<ExpressionSyntax> ArrayValues(ExpressionSyntax expression) => expression switch
    {
        ArrayCreationExpressionSyntax a => a.Initializer?.Expressions ?? default,
        ImplicitArrayCreationExpressionSyntax a => a.Initializer.Expressions,
        CollectionExpressionSyntax a => a.Elements.OfType<ExpressionElementSyntax>().Select(e => e.Expression),
        _ => Array.Empty<ExpressionSyntax>()
    };
    private static bool Number(ExpressionSyntax expression, out double value)
    {
        if (expression is CastExpressionSyntax cast) return Number(cast.Expression, out value);
        if (expression is ParenthesizedExpressionSyntax parens) return Number(parens.Expression, out value);
        if (expression is PrefixUnaryExpressionSyntax unary && Number(unary.Operand, out value))
        { if (unary.IsKind(SyntaxKind.UnaryMinusExpression)) value = -value; return true; }
        if (expression is LiteralExpressionSyntax literal && literal.Token.Value is IConvertible number && literal.Token.Value is not string && literal.Token.Value is not bool)
        { value = number.ToDouble(CultureInfo.InvariantCulture); return true; }
        value = 0; return false;
    }
    private static IEnumerable<ExpressionSyntax> Arguments(ExpressionSyntax expression) => expression switch
    {
        ObjectCreationExpressionSyntax creation => creation.ArgumentList?.Arguments.Select(a => a.Expression) ?? Array.Empty<ExpressionSyntax>(),
        ImplicitObjectCreationExpressionSyntax creation => creation.ArgumentList.Arguments.Select(a => a.Expression),
        InvocationExpressionSyntax call => call.ArgumentList.Arguments.Select(a => a.Expression),
        _ => Array.Empty<ExpressionSyntax>()
    };
    private static void Apply(DesignNode node, string property, ExpressionSyntax value)
    {
        var args = Arguments(value).ToArray();
        bool Pair(out int x, out int y)
        {
            x = y = 0;
            if (args.Length < 2 || !Number(args[0], out var a) || !Number(args[1], out var b)) return false;
            x = (int)a; y = (int)b; return true;
        }
        switch (property)
        {
            case "Text" when value is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression): node.Text = literal.Token.ValueText; break;
            case "Location" when Pair(out int x, out int y): node.Location = new Point(x, y); break;
            case "Size" or "ClientSize" when Pair(out int w, out int h): node.Size = new Size(Math.Max(1, w), Math.Max(1, h)); break;
            case "Width" when Number(value, out var width): node.Width = Math.Max(1, (int)width); break;
            case "Height" when Number(value, out var height): node.Height = Math.Max(1, (int)height); break;
            case "TabIndex" when Number(value, out var index): node.TabIndex = (int)index; break;
            case "Enabled": node.Enabled = !value.IsKind(SyntaxKind.FalseLiteralExpression); break;
            case "Visible": node.Visible = !value.IsKind(SyntaxKind.FalseLiteralExpression); break;
            case "Checked": node.Checked = value.IsKind(SyntaxKind.TrueLiteralExpression); break;
            case "Multiline": node.Multiline = value.IsKind(SyntaxKind.TrueLiteralExpression); break;
            case "Dock": if (Enum.TryParse<DockStyle>(value.ToString().Split('.').Last(), out var dock)) node.Dock = dock; break;
            case "Anchor":
                if (Number(value, out var anchor)) node.Anchor = (AnchorStyles)(int)anchor;
                else
                {
                    var names = value.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().Select(a => a.Name.Identifier.ValueText);
                    var flags = names.Select(n => Enum.TryParse<AnchorStyles>(n, out var a) ? a : 0).Aggregate((AnchorStyles)0, (a, b) => a | b);
                    if (flags != 0) node.Anchor = flags;
                }
                break;
            case "BackColor" or "ForeColor":
                Color color = Color.Empty;
                if (args.Length == 1 && Number(args[0], out var argb)) color = Color.FromArgb((int)argb);
                else if (args.Length is 3 or 4 && args.All(a => Number(a, out _)))
                {
                    var rgb = args.Select(a => { Number(a, out var n); return Math.Clamp((int)n, 0, 255); }).ToArray();
                    color = args.Length == 3 ? Color.FromArgb(rgb[0], rgb[1], rgb[2]) : Color.FromArgb(rgb[0], rgb[1], rgb[2], rgb[3]);
                }
                else if (Enum.TryParse<KnownColor>(value.ToString().Split('.').Last(), out var known)) color = Color.FromKnownColor(known);
                if (!color.IsEmpty && color.A == 255) { if (property == "BackColor") node.BackColor = color; else node.ForeColor = color; }
                break;
            case "Font" when args.Length > 1 && Number(args[1], out var size):
                node.FontSize = (float)Math.Clamp(size, 6, 72);
                if (args[0] is LiteralExpressionSyntax family && family.IsKind(SyntaxKind.StringLiteralExpression)) node.FontFamily = family.Token.ValueText;
                if (args.Length > 2)
                {
                    if (Number(args[2], out var style)) node.FontStyle = (FontStyle)(int)style;
                    else node.FontStyle = args[2].DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>()
                        .Select(a => Enum.TryParse<FontStyle>(a.Name.Identifier.ValueText, out var flag) ? flag : 0)
                        .Aggregate(FontStyle.Regular, (a, b) => a | b);
                }
                break;
            case "Minimum" when Number(value, out var minimum): node.Minimum = Math.Max(0, (int)minimum); node.Value = Math.Max(node.Value, node.Minimum); node.Maximum = Math.Max(node.Maximum, node.Minimum); break;
            case "Maximum" when Number(value, out var maximum): node.Maximum = Math.Max(node.Minimum, (int)maximum); node.Value = Math.Min(node.Value, node.Maximum); break;
            case "Value" when node.Kind == "DateTimePicker" && args.Length > 0 && args.All(a => Number(a, out _)):
                var numbers = args.Select(a => { Number(a, out var n); return (long)n; }).ToArray();
                if (numbers.Length == 1) node.Date = new DateTime(numbers[0]);
                else if (numbers.Length >= 3) node.Date = new DateTime((int)numbers[0], (int)numbers[1], (int)numbers[2]);
                break;
            case "Value" when Number(value, out var number): node.Value = Math.Clamp((int)number, node.Minimum, node.Maximum); break;
            default: DesignOptions.TryRead(node, property, value); break;
        }
    }

    internal string Update(DesignDocument edited)
    {
        edited.Validate();
        var original = Document.AllNodes.ToDictionary(n => n.Id);
        var current = edited.AllNodes.ToDictionary(n => n.Id);
        foreach (var node in edited.AllNodes.Where(n => original.ContainsKey(n.Id)))
            if (node.Name != original[node.Id].Name || node.Namespace != original[node.Id].Namespace)
                throw new InvalidDataException("Rename existing form classes and fields in Code view, then reopen Design view.");
        var removed = original.Values.Where(n => !current.ContainsKey(n.Id)).Select(n => n.SourceMember).ToHashSet(StringComparer.Ordinal);
        var added = current.Values.Where(n => !original.ContainsKey(n.Id)).ToArray();
        if (added.Any(n => MemberNames.Contains(n.Name))) throw new InvalidDataException("A new control name conflicts with an existing designer field.");
        string Target(DesignNode n) => n.SourceMember.Length == 0 ? "this." + n.Name : n.SourceMember == "this" ? "this" : "this." + n.SourceMember;
        var changes = new List<TextChange>();
        var additions = new List<string>();
        var deletedStatements = new HashSet<StatementSyntax>();
        var statements = Initialize.Body.Statements.ToArray();
        var assignments = statements.OfType<ExpressionStatementSyntax>().Where(s => s.Expression is AssignmentExpressionSyntax)
            .ToDictionary(s => s, s => (AssignmentExpressionSyntax)s.Expression);
        void Remove(StatementSyntax statement)
        {
            if (deletedStatements.Add(statement)) changes.Add(new TextChange(statement.Span, ""));
        }
        foreach (var statement in statements)
            if (statement.DescendantNodes().OfType<IdentifierNameSyntax>().Any(i => removed.Contains(i.Identifier.ValueText))) Remove(statement);
        void Set(DesignNode node, string property, string expression, bool eventBinding = false)
        {
            string owner = node.SourceMember.Length == 0 ? node.Name : node.SourceMember;
            var matches = assignments.Where(p => TryProperty(p.Value.Left, out var m, out var prop) && m == owner && prop == property &&
                p.Value.IsKind(eventBinding ? SyntaxKind.AddAssignmentExpression : SyntaxKind.SimpleAssignmentExpression)).ToArray();
            if (eventBinding)
            {
                foreach (var match in matches) Remove(match.Key);
                if (expression != null) additions.Add($"{Target(node)}.{property} += this.{expression};");
            }
            else if (matches.Length > 0 && !deletedStatements.Contains(matches[^1].Key))
                changes.Add(new TextChange(matches[^1].Value.Right.Span, expression));
            else additions.Add($"{Target(node)}.{property} = {expression};");
        }
        foreach (var node in edited.AllNodes)
        {
            original.TryGetValue(node.Id, out var old);
            var props = Properties(node, node == edited.Form);
            var oldProps = old == null ? new Dictionary<string, string>() : Properties(old, old == Document.Form);
            if (old == null) additions.Add($"this.{node.Name} = new global::System.Windows.Forms.{node.Kind}();");
            foreach (var property in props)
                if (!oldProps.TryGetValue(property.Key, out string prior) || prior != property.Value) Set(node, property.Key, property.Value);
            if (DesignCatalog.HasItems(node.Kind) && (old == null || !node.Items.SequenceEqual(old.Items)))
            {
                string owner = node.SourceMember.Length == 0 ? node.Name : node.SourceMember;
                foreach (var statement in statements.OfType<ExpressionStatementSyntax>())
                    if (statement.Expression is InvocationExpressionSyntax call && call.Expression is MemberAccessExpressionSyntax method &&
                        Member(method.Expression) == owner + ".Items" && method.Name.Identifier.ValueText is "Add" or "AddRange" or "Clear") Remove(statement);
                additions.Add($"{Target(node)}.Items.Clear();");
                if (node.Items.Length > 0) additions.Add($"{Target(node)}.Items.AddRange(new object[] {{ {string.Join(", ", node.Items.Select(i => SymbolDisplay.FormatLiteral(i, true)))} }});");
            }
            var eventNames = node.Events.Keys.Concat(old?.Events.Keys ?? Enumerable.Empty<string>()).Distinct();
            foreach (string eventName in eventNames)
            {
                node.Events.TryGetValue(eventName, out string handler);
                string previous = old?.Events.GetValueOrDefault(eventName);
                if (handler != previous) Set(node, eventName, handler, true);
            }
        }
        foreach (var parent in edited.AllNodes.Where(n => n.IsContainer))
        {
            original.TryGetValue(parent.Id, out var old);
            // SplitContainer's internal panels already exist and cannot be added or removed.
            var children = parent.Children.Where(n => !n.SourceMember.Contains('.')).ToArray();
            var previousChildren = old?.Children.Where(n => !n.SourceMember.Contains('.')).ToArray() ?? Array.Empty<DesignNode>();
            if (children.Select(n => n.Id).SequenceEqual(previousChildren.Select(n => n.Id))) continue;
            string controls = (parent.SourceMember.Length == 0 ? parent.Name : parent.SourceMember) + ".Controls";
            foreach (var statement in statements.OfType<ExpressionStatementSyntax>())
                if (statement.Expression is InvocationExpressionSyntax call && call.Expression is MemberAccessExpressionSyntax method &&
                    (Member(method.Expression) == controls || controls == "this.Controls" && Member(method.Expression) == "Controls") &&
                    method.Name.Identifier.ValueText is "Add" or "AddRange") Remove(statement);
            foreach (var child in children) additions.Add($"{Target(parent)}.Controls.Add({Target(child)});");
        }
        foreach (var field in Class.Members.OfType<FieldDeclarationSyntax>())
        {
            var retained = field.Declaration.Variables.Where(v => !removed.Contains(v.Identifier.ValueText)).ToArray();
            if (retained.Length == field.Declaration.Variables.Count) continue;
            changes.Add(new TextChange(field.Span, retained.Length == 0 ? "" : field.WithDeclaration(field.Declaration.WithVariables(SyntaxFactory.SeparatedList(retained))).ToString()));
        }
        string newline = Text.Contains("\r\n") ? "\r\n" : "\n";
        if (additions.Count > 0)
        {
            // Existing layout/resource statements are kept; apply new values before final ResumeLayout.
            var resume = statements.FirstOrDefault(s => s is ExpressionStatementSyntax e && e.Expression is InvocationExpressionSyntax c &&
                c.Expression is MemberAccessExpressionSyntax m && m.Name.Identifier.ValueText == "ResumeLayout");
            int insertion = resume?.SpanStart ?? Initialize.Body.CloseBraceToken.SpanStart;
            changes.Add(new TextChange(new TextSpan(insertion, 0), string.Join(newline + "            ", additions) + newline + "            "));
        }
        if (added.Length > 0)
            changes.Add(new TextChange(new TextSpan(Class.CloseBraceToken.SpanStart, 0),
                string.Join(newline, added.Select(n => $"        private global::System.Windows.Forms.{n.Kind} {n.Name};")) + newline + "    "));
        return SourceText.From(Text).WithChanges(changes).ToString();
    }
    internal static Dictionary<string, string> Properties(DesignNode n, bool root)
    {
        string Literal(string text) => SymbolDisplay.FormatLiteral(text ?? "", true);
        var props = new Dictionary<string, string>
        {
            ["Name"] = Literal(n.Name), [root ? "ClientSize" : "Size"] = $"new global::System.Drawing.Size({n.Width}, {n.Height})",
            ["BackColor"] = $"global::System.Drawing.Color.FromArgb({n.BackColorArgb})", ["ForeColor"] = $"global::System.Drawing.Color.FromArgb({n.ForeColorArgb})",
            ["Font"] = DesignCodeGenerator.FontExpression(n),
            ["Enabled"] = n.Enabled ? "true" : "false"
        };
        if (n.Kind is not ("DateTimePicker" or "NumericUpDown")) props["Text"] = Literal(n.Text);
        if (!root)
        {
            props["Location"] = $"new global::System.Drawing.Point({n.X}, {n.Y})"; props["TabIndex"] = n.TabIndex.ToString(CultureInfo.InvariantCulture);
            props["Anchor"] = $"(global::System.Windows.Forms.AnchorStyles){(int)n.Anchor}";
            props["Dock"] = $"global::System.Windows.Forms.DockStyle.{n.Dock}"; props["Visible"] = n.Visible ? "true" : "false";
        }
        if (n.Kind is "CheckBox" or "RadioButton") props["Checked"] = n.Checked ? "true" : "false";
        if (n.Kind == "TextBox") props["Multiline"] = n.Multiline ? "true" : "false";
        if (n.Kind == "DateTimePicker") props["Value"] = $"new global::System.DateTime({n.Date.Ticks}L)";
        if (DesignCatalog.HasRange(n.Kind))
        { props["Maximum"] = n.Maximum.ToString(CultureInfo.InvariantCulture); props["Minimum"] = n.Minimum.ToString(CultureInfo.InvariantCulture); props["Value"] = n.Value.ToString(CultureInfo.InvariantCulture); }
        foreach (var option in DesignOptions.Expressions(n, true)) props[option.Key] = option.Value;
        return props;
    }
}
