using System;
using System.Drawing;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace CIARE.Debugger
{
    /// <summary>
    /// A source breakpoint displayed in the editor icon margin.
    /// </summary>
    internal sealed class BreakpointBookmark : Bookmark
    {
        private bool _isHealthy = true;

        public BreakpointBookmark(IDocument document, TextLocation location)
            : base(document, location)
        {
        }

        /// <summary>
        /// Gets or sets whether this line maps to an executable debugger sequence point.
        /// </summary>
        public bool IsHealthy
        {
            get => _isHealthy;
            set
            {
                if (_isHealthy == value)
                    return;

                _isHealthy = value;
                if (Document == null)
                    return;

                Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, LineNumber));
                Document.CommitUpdate();
            }
        }

        public override void Draw(IconBarMargin margin, Graphics graphics, Point position)
        {
            margin.DrawBreakpoint(graphics, position.Y, IsEnabled, IsHealthy);
        }
    }

    internal sealed class BreakpointBookmarkFactory : IBookmarkFactory
    {
        public Bookmark CreateBookmark(IDocument document, TextLocation location)
        {
            return new BreakpointBookmark(document, location);
        }
    }

    /// <summary>
    /// Marks the statement at which the debuggee is currently paused.
    /// </summary>
    internal sealed class CurrentStatementBookmark : Bookmark
    {
        public CurrentStatementBookmark(IDocument document, TextLocation location)
            : base(document, location)
        {
        }

        public override bool CanToggle => false;

        public override void Draw(IconBarMargin margin, Graphics graphics, Point position)
        {
            margin.DrawArrow(graphics, position.Y);
        }
    }
}
