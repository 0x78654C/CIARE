using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace CIARE.Designer.Wpf;

// Construct only built-in controls. Project code, markup extensions, and resource providers are never executed.
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class WpfRenderer
{
    internal readonly Dictionary<XElement, FrameworkElement> Views = new();
    internal readonly Dictionary<XElement, System.Drawing.Rectangle> Bounds = new();
    internal int Approximations { get; private set; }
    internal FrameworkElement Build(WpfDocument document, XElement selected = null, bool designMode = true)
    {
        Views.Clear(); Bounds.Clear(); Approximations = 0;
        var root = BuildElement(document.Root, true, designMode);
        foreach (var ancestor in selected?.AncestorsAndSelf() ?? Enumerable.Empty<XElement>())
            if (Views.TryGetValue(ancestor, out var item) && item is TabItem tab) tab.IsSelected = true;
        root.Width = Math.Clamp(WpfDocument.Number(document.Root, "Width", 800), 100, 2000);
        root.Height = Math.Clamp(WpfDocument.Number(document.Root, "Height", 450), 100, 1500);
        if (root is Border border && border.Background == null) border.Background = Brushes.White;
        root.Measure(new Size(root.Width, root.Height)); root.Arrange(new Rect(0, 0, root.Width, root.Height)); root.UpdateLayout();
        foreach (var pair in Views)
        {
            try
            {
                var p = pair.Value == root ? new Point() : pair.Value.TransformToAncestor(root).Transform(new Point());
                Bounds[pair.Key] = new System.Drawing.Rectangle((int)Math.Round(p.X), (int)Math.Round(p.Y), Math.Max(1, (int)Math.Round(pair.Value.ActualWidth)), Math.Max(1, (int)Math.Round(pair.Value.ActualHeight)));
            }
            catch (InvalidOperationException) { } // Unselected tab content is not part of the visual tree.
        }
        return root;
    }
    internal System.Drawing.Bitmap Render(WpfDocument document, XElement selected = null)
    {
        var root = Build(document, selected);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.Width), (int)Math.Ceiling(root.Height), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream(); encoder.Save(stream); stream.Position = 0;
        using var decoded = new System.Drawing.Bitmap(stream);
        return new System.Drawing.Bitmap(decoded);
    }
    private FrameworkElement BuildElement(XElement element, bool root, bool designMode)
    {
        var type = WpfCatalog.TypeFor(element);
        FrameworkElement view;
        if (root) view = new Border();
        else if (type == null)
        {
            Approximations++;
            view = new Border { BorderBrush = Brushes.SlateGray, BorderThickness = new Thickness(1), Background = Brushes.Gainsboro,
                Child = new TextBlock { Text = element.Name.LocalName, Foreground = Brushes.DimGray, Margin = new Thickness(8) } };
        }
        else view = (FrameworkElement)Activator.CreateInstance(type);
        Views[element] = view;
        foreach (var spec in WpfCatalog.Properties(element))
        {
            if (element.Attribute(spec.Name) is not { } attribute) continue;
            string value = attribute.Value;
            if (value.StartsWith('{')) { Approximations++; continue; }
            // Tooltips/content must remain text, never executable XAML or a project object.
            try
            {
                var property = view.GetType().GetProperty(spec.Name);
                if (property?.CanWrite == true) property.SetValue(view, WpfCatalog.ConvertValue(property.PropertyType, value));
            }
            catch (Exception error) when (error is ArgumentException or FormatException or InvalidOperationException or NotSupportedException or System.Reflection.TargetInvocationException)
            { Approximations++; }
        }
        // Unknown controls still retain their dimensions and position in the preview.
        if (type == null)
        {
            view.Width = WpfDocument.Number(element, "Width", 160); view.Height = WpfDocument.Number(element, "Height", 80);
            if (element.Attribute("Margin") is { } margin && !margin.Value.StartsWith('{'))
                try { view.Margin = (Thickness)WpfCatalog.ConvertValue(typeof(Thickness), margin.Value); } catch { }
            view.HorizontalAlignment = HorizontalAlignment.Left; view.VerticalAlignment = VerticalAlignment.Top;
        }
        foreach (string property in new[] { "Canvas.Left", "Canvas.Top", "Canvas.Right", "Canvas.Bottom", "Grid.Row", "Grid.Column", "Grid.RowSpan", "Grid.ColumnSpan", "DockPanel.Dock" })
        {
            if (element.Attribute(property) is not { } attr || attr.Value.StartsWith('{')) continue;
            try
            {
                switch (property)
                {
                    case "Canvas.Left": Canvas.SetLeft(view, double.Parse(attr.Value, System.Globalization.CultureInfo.InvariantCulture)); break;
                    case "Canvas.Top": Canvas.SetTop(view, double.Parse(attr.Value, System.Globalization.CultureInfo.InvariantCulture)); break;
                    case "Canvas.Right": Canvas.SetRight(view, double.Parse(attr.Value, System.Globalization.CultureInfo.InvariantCulture)); break;
                    case "Canvas.Bottom": Canvas.SetBottom(view, double.Parse(attr.Value, System.Globalization.CultureInfo.InvariantCulture)); break;
                    case "Grid.Row": Grid.SetRow(view, int.Parse(attr.Value)); break;
                    case "Grid.Column": Grid.SetColumn(view, int.Parse(attr.Value)); break;
                    case "Grid.RowSpan": Grid.SetRowSpan(view, int.Parse(attr.Value)); break;
                    case "Grid.ColumnSpan": Grid.SetColumnSpan(view, int.Parse(attr.Value)); break;
                    case "DockPanel.Dock": DockPanel.SetDock(view, Enum.Parse<Dock>(attr.Value)); break;
                }
            }
            catch (Exception error) when (error is ArgumentException or FormatException or OverflowException) { Approximations++; }
        }
        // Invisible/disabled elements remain selectable in Design view.
        if (designMode) { view.Visibility = Visibility.Visible; view.IsEnabled = true; }
        if (view is Grid grid)
        {
            foreach (var row in element.Element(WpfDocument.Presentation + "Grid.RowDefinitions")?.Elements() ?? Enumerable.Empty<XElement>())
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLengthValue((string)row.Attribute("Height")) });
            foreach (var col in element.Element(WpfDocument.Presentation + "Grid.ColumnDefinitions")?.Elements() ?? Enumerable.Empty<XElement>())
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLengthValue((string)col.Attribute("Width")) });
        }
        if (type != null || root)
            foreach (var child in WpfDocument.Children(element))
            {
                var content = BuildElement(child, false, designMode);
                switch (view)
                {
                    case Panel panel: panel.Children.Add(content); break;
                    case ItemsControl items: items.Items.Add(content); break;
                    case Decorator decorator when decorator.Child == null: decorator.Child = content; break;
                    case ContentControl single: single.Content = content; break;
                    default: Approximations++; break;
                }
            }
        if (view is TextBlock text && element.Attribute("Text") == null && !element.HasElements) text.Text = element.Value;
        if (view is ContentControl c && c.Content == null && !element.HasElements && !string.IsNullOrWhiteSpace(element.Value)) c.Content = element.Value;
        if (view is Image image && image.Source == null)
        {
            var drawing = new GeometryDrawing(Brushes.WhiteSmoke, new Pen(Brushes.SlateGray, 1), new RectangleGeometry(new Rect(0, 0, 150, 100)));
            image.Source = new DrawingImage(drawing); if (element.Attribute("Source") != null) Approximations++;
        }
        if (view is Control control) control.ApplyTemplate();
        return view;
    }
    private static GridLength GridLengthValue(string text)
    {
        try { return (GridLength)new GridLengthConverter().ConvertFromInvariantString(text ?? "*"); }
        catch { return new GridLength(1, GridUnitType.Star); }
    }
}
