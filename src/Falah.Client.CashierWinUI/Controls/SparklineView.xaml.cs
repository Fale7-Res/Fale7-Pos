using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Falah.Client.CashierWinUI.Controls;

/// <summary>
/// A trend line small enough to sit inside a figure's own card.
///
/// The geometry is rebuilt whenever the points or the size change, because a path built against the
/// wrong width silently draws off the edge rather than failing. The last point is marked: a
/// sparkline's job is to say where the number is heading, and the end is where it is heading to.
///
/// A series that is entirely flat - every value equal, which for a restaurant usually means every
/// value zero - draws a dash instead of a straight line through the middle. A line implies movement
/// that did not happen.
/// </summary>
public sealed partial class SparklineView : UserControl
{
    public SparklineView()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Render();
        Loaded += (_, _) => Render();
    }

    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points), typeof(object), typeof(SparklineView),
        new PropertyMetadata(null, (d, _) => ((SparklineView)d).Render()));

    /// <summary>The raw series in piastres. Typed as object so a plain IReadOnlyList&lt;long&gt;
    /// binds without the caller having to wrap it.</summary>
    public object? Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register(
        nameof(LineBrush), typeof(Brush), typeof(SparklineView),
        new PropertyMetadata(null, (d, _) => ((SparklineView)d).Render()));

    public Brush? LineBrush
    {
        get => (Brush?)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    private IReadOnlyList<long> Series => Points as IReadOnlyList<long> ?? Array.Empty<long>();

    private void Render()
    {
        var width = ActualWidth;
        var height = ActualHeight;
        var series = Series;

        // Nothing to draw against yet - the next SizeChanged will bring a real box.
        if (width <= 1 || height <= 1) return;

        var brush = LineBrush ?? new SolidColorBrush(Colors.Gray);
        LinePath.Stroke = brush;
        AreaPath.Fill = brush;
        EndPoint.Fill = brush;

        var min = series.Count > 0 ? series.Min() : 0;
        var max = series.Count > 0 ? series.Max() : 0;

        if (series.Count < 2 || max == min)
        {
            // Flat or absent: say so rather than drawing a line that implies a steady trend.
            LinePath.Data = null;
            AreaPath.Data = null;
            EndPoint.Visibility = Visibility.Collapsed;
            FlatLabel.Visibility = Visibility.Visible;
            return;
        }

        FlatLabel.Visibility = Visibility.Collapsed;
        EndPoint.Visibility = Visibility.Visible;

        const double pad = 3;
        var usableW = width - pad * 2;
        var usableH = height - pad * 2;
        var stepX = usableW / (series.Count - 1);
        var span = (double)(max - min);

        var points = new List<Point>(series.Count);
        for (var i = 0; i < series.Count; i++)
        {
            var x = pad + i * stepX;
            var y = pad + usableH - (series[i] - min) / span * usableH;
            points.Add(new Point(x, y));
        }

        LinePath.Data = BuildLine(points);
        AreaPath.Data = BuildArea(points, height - pad);

        var last = points[^1];
        EndPoint.Margin = new Thickness(last.X - 2.5, last.Y - 2.5, 0, 0);
    }

    private static PathGeometry BuildLine(IReadOnlyList<Point> points)
    {
        var figure = new PathFigure { StartPoint = points[0], IsClosed = false, IsFilled = false };
        for (var i = 1; i < points.Count; i++) figure.Segments.Add(new LineSegment { Point = points[i] });
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    /// <summary>The same line closed down to the baseline. The fill gives the eye a shape to read
    /// at a glance, which a one-pixel line at this size cannot.</summary>
    private static PathGeometry BuildArea(IReadOnlyList<Point> points, double baseline)
    {
        var figure = new PathFigure { StartPoint = new Point(points[0].X, baseline), IsClosed = true, IsFilled = true };
        foreach (var point in points) figure.Segments.Add(new LineSegment { Point = point });
        figure.Segments.Add(new LineSegment { Point = new Point(points[^1].X, baseline) });
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }
}
