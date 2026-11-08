using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;
namespace NexusControlCenter.Controls;
public sealed class SparklineChart : FrameworkElement
{
    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(nameof(Values), typeof(IEnumerable), typeof(SparklineChart), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(SparklineChart), new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SecondaryValuesProperty = DependencyProperty.Register(nameof(SecondaryValues), typeof(IEnumerable), typeof(SparklineChart), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SecondaryStrokeProperty = DependencyProperty.Register(nameof(SecondaryStroke), typeof(Brush), typeof(SparklineChart), new FrameworkPropertyMetadata(Brushes.MediumPurple, FrameworkPropertyMetadataOptions.AffectsRender));
    public IEnumerable? Values { get => (IEnumerable?)GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }
    public IEnumerable? SecondaryValues { get => (IEnumerable?)GetValue(SecondaryValuesProperty); set => SetValue(SecondaryValuesProperty, value); }
    public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public Brush SecondaryStroke { get => (Brush)GetValue(SecondaryStrokeProperty); set => SetValue(SecondaryStrokeProperty, value); }
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == ValuesProperty || e.Property == SecondaryValuesProperty)
        {
            if (e.OldValue is INotifyCollectionChanged oldCollection) oldCollection.CollectionChanged -= Values_CollectionChanged;
            if (e.NewValue is INotifyCollectionChanged newCollection) newCollection.CollectionChanged += Values_CollectionChanged;
        }
    }
    private void Values_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Dispatcher.InvokeAsync(InvalidateVisual);
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc); var w = ActualWidth; var h = ActualHeight; if (w < 2 || h < 2) return;
        var primary = ToValues(Values); var secondary = ToValues(SecondaryValues);
        var grid = new Pen(new SolidColorBrush(Color.FromArgb(35, 160, 180, 220)), 1);
        for (int i = 1; i <= 3; i++) dc.DrawLine(grid, new Point(0, h * i / 4d), new Point(w, h * i / 4d));
        double max = Math.Max(100, primary.Concat(secondary).DefaultIfEmpty(100).Max());
        DrawSeries(dc, primary, Stroke, w, h, max, true);
        DrawSeries(dc, secondary, SecondaryStroke, w, h, max, false);
    }
    private static double[] ToValues(IEnumerable? values) => values?.Cast<object>().Select(v => Convert.ToDouble(v)).ToArray() ?? Array.Empty<double>();
    private static void DrawSeries(DrawingContext dc, double[] values, Brush stroke, double w, double h, double max, bool fill)
    {
        if (values.Length < 2) return;
        var points = values.Select((v, i) => new Point(i * w / (values.Length - 1d), h - 4 - Math.Clamp(v, 0, max) / max * (h - 8))).ToArray();
        if (fill)
        {
            var area = new StreamGeometry(); using (var ctx = area.Open()) { ctx.BeginFigure(new Point(0, h), true, true); foreach (var p in points) ctx.LineTo(p, true, false); ctx.LineTo(new Point(w, h), true, false); }
            var color = stroke is SolidColorBrush sc ? sc.Color : Colors.DodgerBlue;
            dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B)), null, area);
        }
        var line = new StreamGeometry(); using (var context = line.Open()) { context.BeginFigure(points[0], false, false); for (int i = 1; i < points.Length; i++) context.LineTo(points[i], true, false); }
        dc.DrawGeometry(null, new Pen(stroke, 2.25) { LineJoin = PenLineJoin.Round }, line);
        dc.DrawEllipse(stroke, null, points[^1], 3.2, 3.2);
    }
}
