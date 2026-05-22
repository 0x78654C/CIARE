// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Gui.CompletionWindow
{
    /// <summary>
    /// Description of CodeCompletionListView.
    /// </summary>
    public class CodeCompletionListView : System.Windows.Forms.UserControl
    {
        ICompletionData[] completionData;
        int firstItem = 0;
        int selectedItem = -1;
        ImageList imageList;
        Font completionFont;
        public static CompletionThemeColors ActiveTheme = CompletionThemeColors.Light;
        const int HorizontalPadding = 8;
        const int TextGap = 8;
        const int RowInset = 3;

        public ImageList ImageList
        {
            get
            {
                return imageList;
            }
            set
            {
                imageList = value;
            }
        }

        public int FirstItem
        {
            get
            {
                return firstItem;
            }
            set
            {
                if (firstItem != value)
                {
                    firstItem = value;
                    OnFirstItemChanged(EventArgs.Empty);
                }
            }
        }

        public ICompletionData SelectedCompletionData
        {
            get
            {
                if (selectedItem < 0)
                {
                    return null;
                }
                return completionData[selectedItem];
            }
        }

        public int ItemHeight
        {
            get
            {
                int imageHeight = imageList != null ? imageList.ImageSize.Height : Font.Height;
                return Math.Max(imageHeight + 8, Font.Height + 8);
            }
        }

        public int MaxVisibleItem
        {
            get
            {
                return Height / ItemHeight;
            }
        }

        public CodeCompletionListView(ICompletionData[] completionData)
        {
            Array.Sort(completionData, DefaultCompletionData.Compare);
            this.completionData = completionData;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Selectable, false);
            completionFont = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Font = completionFont;
        }

        public void Close()
        {
            if (completionData != null)
            {
                Array.Clear(completionData, 0, completionData.Length);
            }
            base.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && completionFont != null)
            {
                completionFont.Dispose();
                completionFont = null;
            }
            base.Dispose(disposing);
        }

        public void SelectIndex(int index)
        {
            int oldSelectedItem = selectedItem;
            int oldFirstItem = firstItem;

            index = Math.Max(0, index);
            selectedItem = Math.Max(0, Math.Min(completionData.Length - 1, index));
            if (selectedItem < firstItem)
            {
                FirstItem = selectedItem;
            }
            if (firstItem + MaxVisibleItem <= selectedItem)
            {
                FirstItem = selectedItem - MaxVisibleItem + 1;
            }
            if (oldSelectedItem != selectedItem)
            {
                if (firstItem != oldFirstItem)
                {
                    Invalidate();
                }
                else
                {
                    int min = Math.Min(selectedItem, oldSelectedItem) - firstItem;
                    int max = Math.Max(selectedItem, oldSelectedItem) - firstItem;
                    Invalidate(new Rectangle(0, 1 + min * ItemHeight, Width, (max - min + 1) * ItemHeight));
                }
                OnSelectedItemChanged(EventArgs.Empty);
            }
        }

        public void CenterViewOn(int index)
        {
            int oldFirstItem = this.FirstItem;
            int firstItem = index - MaxVisibleItem / 2;
            if (firstItem < 0)
                this.FirstItem = 0;
            else if (firstItem >= completionData.Length - MaxVisibleItem)
                this.FirstItem = completionData.Length - MaxVisibleItem;
            else
                this.FirstItem = firstItem;
            if (this.FirstItem != oldFirstItem)
            {
                Invalidate();
            }
        }

        public void ClearSelection()
        {
            if (selectedItem < 0)
                return;
            int itemNum = selectedItem - firstItem;
            selectedItem = -1;
            Invalidate(new Rectangle(0, itemNum * ItemHeight, Width, (itemNum + 1) * ItemHeight + 1));
            Update();
            OnSelectedItemChanged(EventArgs.Empty);
        }

        public void PageDown()
        {
            SelectIndex(selectedItem + MaxVisibleItem);
        }

        public void PageUp()
        {
            SelectIndex(selectedItem - MaxVisibleItem);
        }

        public void SelectNextItem()
        {
            SelectIndex(selectedItem + 1);
        }

        public void SelectPrevItem()
        {
            SelectIndex(selectedItem - 1);
        }

        public void SelectItemWithStart(string startText)
        {
            if (startText == null || startText.Length == 0) return;
            string originalStartText = startText;
            startText = startText.ToLower();
            int bestIndex = -1;
            int bestQuality = -1;
            // Qualities: 0 = match start
            //            1 = match start case sensitive
            //            2 = full match
            //            3 = full match case sensitive
            double bestPriority = 0;
            for (int i = 0; i < completionData.Length; ++i)
            {
                string itemText = completionData[i].Text;
                string lowerText = itemText.ToLower();
                if (lowerText.StartsWith(startText))
                {
                    double priority = completionData[i].Priority;
                    int quality;
                    if (lowerText == startText)
                    {
                        if (itemText == originalStartText)
                            quality = 3;
                        else
                            quality = 2;
                    }
                    else if (itemText.StartsWith(originalStartText))
                    {
                        quality = 1;
                    }
                    else
                    {
                        quality = 0;
                    }
                    bool useThisItem;
                    if (bestQuality < quality)
                    {
                        useThisItem = true;
                    }
                    else
                    {
                        if (bestIndex == selectedItem)
                        {
                            useThisItem = false;
                        }
                        else if (i == selectedItem)
                        {
                            useThisItem = bestQuality == quality;
                        }
                        else
                        {
                            useThisItem = bestQuality == quality && bestPriority < priority;
                        }
                    }
                    if (useThisItem)
                    {
                        bestIndex = i;
                        bestPriority = priority;
                        bestQuality = quality;
                    }
                }
            }
            if (bestIndex < 0)
            {
                ClearSelection();
            }
            else
            {
                if (bestIndex < firstItem || firstItem + MaxVisibleItem <= bestIndex)
                {
                    SelectIndex(bestIndex);
                    CenterViewOn(bestIndex);
                }
                else
                {
                    SelectIndex(bestIndex);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            int yPos = 1;
            int itemHeight = ItemHeight;
            int imageWidth = 0;
            if (imageList != null && imageList.ImageSize.Height > 0)
            {
                imageWidth = Math.Min(imageList.ImageSize.Width, itemHeight - 8);
            }
            int curItem = firstItem;
            var t = ActiveTheme;
            Color backColor          = t.BackColor;
            Color rowHoverColor      = t.RowAlternateColor;
            Color foreColor          = t.ForeColor;
            Color selectedStartColor = t.SelectionStartColor;
            Color selectedEndColor   = t.SelectionEndColor;
            Color selectedForeColor  = t.SelectionForeColor;
            Color accentColor        = t.AccentColor;
            Color borderColor        = t.BorderColor;
            Graphics g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(backColor);

            using (SolidBrush rowHoverBrush = new SolidBrush(rowHoverColor))
            using (Pen borderPen = new Pen(borderColor))
            using (Pen accentPen = new Pen(accentColor, 2))
            {
                while (curItem < completionData.Length && yPos < Height)
                {
                    Rectangle drawingBackground = new Rectangle(1, yPos, Width - 2, itemHeight);
                    if (drawingBackground.IntersectsWith(pe.ClipRectangle))
                    {
                        Rectangle rowBounds = new Rectangle(HorizontalPadding / 2,
                                                            yPos + RowInset,
                                                            Math.Max(1, Width - HorizontalPadding),
                                                            Math.Max(1, itemHeight - RowInset * 2));
                        if (curItem == selectedItem)
                        {
                            using (GraphicsPath selectedPath = CreateRoundRectangle(rowBounds, 5))
                            using (LinearGradientBrush selectedBrush = new LinearGradientBrush(rowBounds,
                                                                                               selectedStartColor,
                                                                                               selectedEndColor,
                                                                                               LinearGradientMode.Vertical))
                            {
                                g.FillPath(selectedBrush, selectedPath);
                            }
                            g.DrawLine(accentPen, rowBounds.Left + 3, rowBounds.Top + 4, rowBounds.Left + 3, rowBounds.Bottom - 4);
                        }
                        else if (curItem % 2 == 1)
                        {
                            g.FillRectangle(rowHoverBrush, drawingBackground);
                        }

                        int xPos = HorizontalPadding;
                        if (imageList != null && completionData[curItem].ImageIndex >= 0 && completionData[curItem].ImageIndex < imageList.Images.Count)
                        {
                            Image image = imageList.Images[completionData[curItem].ImageIndex];
                            int imageHeight = Math.Min(image.Height, itemHeight - 8);
                            int imageY = yPos + (itemHeight - imageHeight) / 2;
                            g.DrawImage(image, new Rectangle(xPos, imageY, imageWidth, imageHeight));
                            xPos += imageWidth + TextGap;
                        }

                        Rectangle textBounds = new Rectangle(xPos,
                                                             yPos,
                                                             Math.Max(1, Width - xPos - HorizontalPadding),
                                                             itemHeight);
                        TextRenderer.DrawText(g,
                                              completionData[curItem].Text,
                                              Font,
                                              textBounds,
                                              curItem == selectedItem ? selectedForeColor : foreColor,
                                              TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
                    }

                    yPos += itemHeight;
                    ++curItem;
                }

                g.DrawRectangle(borderPen, new Rectangle(0, 0, Width - 1, Height - 1));
            }
        }

        static GraphicsPath CreateRoundRectangle(Rectangle rectangle, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            if (diameter <= 0)
            {
                path.AddRectangle(rectangle);
                path.CloseFigure();
                return path;
            }

            Rectangle arc = new Rectangle(rectangle.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = rectangle.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rectangle.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rectangle.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            float yPos = 1;
            int curItem = firstItem;
            float itemHeight = ItemHeight;

            while (curItem < completionData.Length && yPos < Height)
            {
                RectangleF drawingBackground = new RectangleF(1, yPos, Width - 2, itemHeight);
                if (drawingBackground.Contains(e.X, e.Y))
                {
                    SelectIndex(curItem);
                    break;
                }
                yPos += itemHeight;
                ++curItem;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pe)
        {
        }

        protected virtual void OnSelectedItemChanged(EventArgs e)
        {
            if (SelectedItemChanged != null)
            {
                SelectedItemChanged(this, e);
            }
        }

        protected virtual void OnFirstItemChanged(EventArgs e)
        {
            if (FirstItemChanged != null)
            {
                FirstItemChanged(this, e);
            }
        }

        public event EventHandler SelectedItemChanged;
        public event EventHandler FirstItemChanged;

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // CodeCompletionListView
            // 
            this.Name = "CodeCompletionListView";
            this.ResumeLayout(false);

        }
    }
}
