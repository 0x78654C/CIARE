using CIARE.GUI;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace CIARE;

internal static class ThemeRegression
{
    internal static void Run(Action<bool, string> assert, string artifacts)
    {
        ThemeManager.LoadExternalThemes();
        var names = new[] { "C#-Light", "C#-Dark", "C#-DarkVS" }
            .Concat(ThemeManager.ExternalThemeNames).ToArray();
        assert(names.Length == 11, "All bundled C# themes are available");
        foreach (string name in names)
        {
            var strategy = (DefaultHighlightingStrategy)HighlightingManager.Manager.FindHighlighter(name);
            assert(strategy.Name == name, name + " loads without falling back to plain text");
            var background = strategy.GetColorFor("Default").BackgroundColor;
            var selection = strategy.GetColorFor("Selection");
            assert(Contrast(strategy.GetColorFor("Default").Color, background) >= 4.5, name + " readable default text");
            assert(Contrast(selection.Color, selection.BackgroundColor) >= 4.5, name + " readable selection");
            assert(Contrast(strategy.GetColorFor("LineNumbers").Color, background) >= 4.5, name + " readable line numbers");

            // Inspect the actual shipped definition, including colors not exercised by the sample.
            string filename = name == "C#-Light" ? "CSharp-Mode.xshd" : "CSharp-Mode-" + name.Substring(3) + ".xshd";
            using var stream = File.Exists(Path.Combine(ThemeManager.ThemesFolder, filename))
                ? File.OpenRead(Path.Combine(ThemeManager.ThemesFolder, filename))
                : typeof(HighlightingManager).Assembly.GetManifestResourceStream("ICSharpCode.TextEditor.Resources." + filename);
            var xml = new XmlDocument();
            xml.Load(stream);
            foreach (XmlElement element in xml.SelectNodes("//Digits | //Span | //KeyWords | //MarkPrevious | //Begin[@color] | //End[@color]"))
                assert(Contrast(ColorTranslator.FromHtml(element.GetAttribute("color")), background) >= 4.5,
                    name + " readable syntax: " + element.GetAttribute("name"));
            foreach (XmlElement element in xml.SelectNodes("//Begin | //End | //MarkPrevious"))
                assert(element.InnerText == element.InnerText.Trim(), name + " has exact match delimiters");

            var document = new DocumentFactory().CreateDocument();
            document.HighlightingStrategy = strategy;
            Color RuleColor(string xpath) => ColorTranslator.FromHtml(((XmlElement)xml.SelectSingleNode(xpath)).GetAttribute("color"));
            Color SpanColor(string span) => RuleColor("//RuleSet[not(@name)]/Span[@name='" + span + "']");
            void Check(string source, string token, Color expected)
            {
                document.TextContent = source;
                strategy.MarkTokens(document);
                int offset = source.IndexOf(token, StringComparison.Ordinal);
                assert(offset >= 0, "Test token exists: " + token);
                var line = document.GetLineSegmentForOffset(offset);
                var word = line.Words.First(w => !w.IsWhiteSpace && w.Offset <= offset - line.Offset
                    && w.Offset + w.Length > offset - line.Offset);
                assert(word.Color.ToArgb() == expected.ToArgb(), name + " highlights " + token);
            }

            foreach (string keyword in "async await record init required file scoped field extension and or not nint nuint nameof with unmanaged notnull managed allows alias".Split(' '))
            {
                document.TextContent = keyword;
                strategy.MarkTokens(document);
                assert(!document.GetLineSegment(0).Words.Single().HasDefaultColor, name + " recognizes " + keyword);
            }
            foreach (string type in "Task ValueTask CancellationToken IAsyncEnumerable Span ReadOnlySpan DateOnly TimeOnly JsonSerializer PriorityQueue".Split(' '))
                Check(type + " value;", type, RuleColor("//KeyWords[@name='DotNetClassNames']"));

            Check("// record is a comment", "record", SpanColor("LineComment"));
            Check("/* first line\nrecord */ int value;", "record", SpanColor("BlockComment"));
            Check("/* first line\nrecord */ int value;", "int", RuleColor("//KeyWords[@name='ValueTypes']"));
            Check("string text = \"record \\\"quoted\\\" text\";", "record", SpanColor("String"));
            Check("string path = @\"first\nrecord \"\"quoted\"\" line\";", "record", SpanColor("MultiLineString"));
            Check("char letter = 'x';", "x", SpanColor("Char"));
            Check("// TODO: update this", "TODO", RuleColor("//RuleSet[@name='CommentMarkerSet']/KeyWords[@name='ErrorWords']"));
            Check("/// <summary>Some documentation</summary>", "Some", SpanColor("DocLineComment"));
            Check("/// <typeparamref name=\"T\"/>", "typeparamref", RuleColor("//KeyWords[@name='SpecialComment']"));
            Check("/// <typeparamref name=\"T\"/>", "T", RuleColor("//RuleSet[@name='XmlDocSet']/Span[@name='String']"));
            Check("#nullable enable", "nullable", RuleColor("//RuleSet[@name='PreprocessorSet']/KeyWords"));
            Check("#pragma warning restore", "restore", RuleColor("//RuleSet[@name='PreprocessorSet']/KeyWords"));
            Check("value is null ? 0 : 42;", ":", RuleColor("//RuleSet[not(@name)]/KeyWords[@name='Punctuation']"));
            Check("int number = 42;", "42", strategy.DigitColor.Color);

            var completion = ThemeManager.GetCompletionThemeColors(name);
            assert(completion.BackColor.ToArgb() == background.ToArgb(), name + " completion background matches editor");
            assert(completion.SelectionStartColor.ToArgb() == selection.BackgroundColor.ToArgb(), name + " completion selection matches editor");
            assert(Contrast(completion.ForeColor, completion.BackColor) >= 4.5, name + " readable completion text");
            assert(Contrast(completion.SelectionForeColor, completion.SelectionStartColor) >= 4.5
                && Contrast(completion.SelectionForeColor, completion.SelectionEndColor) >= 4.5, name + " readable completion selection gradient");
            assert(completion.AccentColor.ToArgb() == strategy.GetColorFor("Accent").Color.ToArgb(), name + " explicit completion accent");
            Render(name, artifacts);
        }
    }

    private static void Render(string name, string artifacts)
    {
        using var form = new Form { ShowInTaskbar = false, Opacity = 0, ClientSize = new Size(920, 460) };
        using var editor = new TextEditorControl { Dock = DockStyle.Fill, Font = new Font("Consolas", 11) };
        form.Controls.Add(editor);
        editor.SetHighlighting(name);
        editor.Text = """
            #nullable enable
            using System.Text.Json;

            /// <summary>Load a cached result asynchronously.</summary>
            /// <typeparam name="T">The result type.</typeparam>
            public sealed record CacheEntry<T>
            {
                // TODO: support cancellation during refresh.
                public required string Key { get; init; }
                public T? Value { get; set; }

                public async Task<string> ReadAsync(CancellationToken token)
                {
                    const int retries = 42;
                    var path = @"C:\cache\results.json";
                    if (Value is not null and not false)
                        return JsonSerializer.Serialize(Value);
                    await Task.Delay(retries, token);
                    return "No cached value";
                }
            }
            """;
        form.Show();
        Application.DoEvents();
        using var bitmap = new Bitmap(editor.Width, editor.Height);
        editor.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        bitmap.Save(Path.Combine(artifacts, "theme-" + name.Substring(3) + ".png"));
        form.Hide();
    }

    private static double Contrast(Color first, Color second)
    {
        static double Linear(byte channel)
        {
            double value = channel / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
        static double Luminance(Color color) => 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
        double a = Luminance(first), b = Luminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }
}
