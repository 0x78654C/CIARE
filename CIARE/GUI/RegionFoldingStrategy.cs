using System.Collections.Generic;
using ICSharpCode.TextEditor.Document;

namespace CIARE.GUI
{
    /// <summary>
    /// Fold editor manager.
    /// </summary>
    public class FoldingStrategy : IFoldingStrategy
    {
        public List<FoldMarker> GenerateFoldMarkers(IDocument document, string fileName, object parseInformation)
        {
            List<FoldMarker> list = new List<FoldMarker>();
            var startLines = new Stack<int>();
            for (int i = 0; i < document.TotalNumberOfLines; i++)
            {
                string text = document.GetText(document.GetLineSegment(i));

                if (text.Trim().StartsWith("{"))
                {
                    startLines.Push(i);
                }
                if (text.Trim().StartsWith("}"))
                {
                    if (startLines.Count > 0)
                    {
                        int start = startLines.Pop();
                        list.Add(new FoldMarker(document, start, document.GetLineSegment(start).Length, i, 57, FoldType.TypeBody, "...}"));
                    }
                }
                if (text.Trim().StartsWith("#region"))
                {
                    startLines.Push(i);
                }
                if (text.Trim().StartsWith("#endregion"))
                {
                    if (startLines.Count > 0)
                    {
                        int start = startLines.Pop();
                        list.Add(new FoldMarker(document, start, document.GetLineSegment(start).Length, i, 57, FoldType.Region, "..."));
                    }
                }
                if (text.Trim().StartsWith("///<summary>"))
                {
                    startLines.Push(i);
                }
                if (text.Trim().StartsWith("///<returns>"))
                {
                    if (startLines.Count > 0)
                    {
                        int start = startLines.Pop();
                        string display = document.GetText(document.GetLineSegment(start + 1).Offset, document.GetLineSegment(start + 1).Length);
                        display = display.Trim().TrimStart('/');
                        list.Add(new FoldMarker(document, start, document.GetLineSegment(start).Length, i, 57, FoldType.TypeBody, display));
                    }
                }
            }
            return list;
        }
    }
}
