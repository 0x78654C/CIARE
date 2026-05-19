using System.Drawing;

namespace ICSharpCode.TextEditor.Gui.CompletionWindow
{
    /// <summary>
    /// Holds all colours needed to paint the code-completion pop-up,
    /// so it can match any editor theme.
    /// </summary>
    public struct CompletionThemeColors
    {
        public Color BackColor;
        public Color RowAlternateColor;
        public Color ForeColor;
        public Color SelectionStartColor;
        public Color SelectionEndColor;
        public Color SelectionForeColor;
        public Color AccentColor;
        public Color BorderColor;

        public static CompletionThemeColors Light => new CompletionThemeColors
        {
            BackColor           = Color.FromArgb(250, 251, 253),
            RowAlternateColor   = Color.FromArgb(244, 247, 252),
            ForeColor           = Color.FromArgb(37,  43,  54),
            SelectionStartColor = Color.FromArgb(218, 235, 255),
            SelectionEndColor   = Color.FromArgb(199, 224, 255),
            SelectionForeColor  = Color.FromArgb(20,  46,  86),
            AccentColor         = Color.FromArgb(24,  105, 210),
            BorderColor         = Color.FromArgb(188, 198, 214),
        };

        public static CompletionThemeColors Dark => new CompletionThemeColors
        {
            BackColor           = Color.FromArgb(31,  33,  38),
            RowAlternateColor   = Color.FromArgb(42,  45,  52),
            ForeColor           = Color.FromArgb(225, 230, 236),
            SelectionStartColor = Color.FromArgb(54,  96,  168),
            SelectionEndColor   = Color.FromArgb(43,  75,  135),
            SelectionForeColor  = Color.White,
            AccentColor         = Color.FromArgb(112, 172, 255),
            BorderColor         = Color.FromArgb(78,  86,  102),
        };
    }
}
