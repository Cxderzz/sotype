using System.Globalization;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sotype.Cli.Rendering.Charts;

/// <summary>
/// A line chart of one or more series, with a labelled y-axis and a legend. It fills the available width.
/// </summary>
/// <example>
/// <code>
/// var chart = new LineChart { Height = 8 }
///     .AddSeries(new ChartSeries("wpm", wpmPerTest, Color.Green));
/// </code>
/// </example>
/// <remarks>
/// Each series is stretched across the full width, whatever its length. Series added later are
/// drawn on top of earlier ones: where two series pass through the same cell, only the later one
/// shows, since a cell can only have one colour.
/// </remarks>
public sealed class LineChart : Renderable
{
    private const string LabelledTick = "┤";
    private const string UnlabelledTick = "│";

    private readonly List<ChartSeries> _series = [];

    /// <summary>The plot's height in terminal rows, not counting the x-axis and legend.</summary>
    public int Height { get; init; } = 10;

    /// <summary>The bottom of the y-axis, or <see langword="null"/> to use the lowest value.</summary>
    public double? Min { get; init; }

    /// <summary>The top of the y-axis, or <see langword="null"/> to use the highest value.</summary>
    public double? Max { get; init; }

    /// <summary>The number format for the y-axis labels.</summary>
    public string LabelFormat { get; init; } = "0";

    public Color AxisColour { get; init; } = Color.Grey35;

    public bool ShowLegend { get; init; } = true;

    public LineChart AddSeries(ChartSeries series)
    {
        _series.Add(series);
        return this;
    }

    protected override Measurement Measure(RenderOptions options, int maxWidth) => new(maxWidth, maxWidth);

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var plotted = _series.Where(series => series.Values.Count > 0).ToList();
        if (plotted.Count == 0)
            return [];

        var (min, max) = AxisRange(plotted);
        var labels = AxisLabels(min, max);
        var labelWidth = labels.Max(label => label.Length);

        // Each row is the label, a space, the tick, then the plot.
        var canvas = new BrailleCanvas(columns: Math.Max(1, maxWidth - labelWidth - 2), rows: Height);
        foreach (var series in plotted)
            Plot(canvas, series, min, max);

        var axisStyle = new Style(AxisColour);
        var segments = new List<Segment>();

        for (var row = 0; row < Height; row++)
        {
            var label = labels[row];
            segments.Add(new Segment(label.PadLeft(labelWidth) + " ", axisStyle));
            segments.Add(new Segment(label.Length > 0 ? LabelledTick : UnlabelledTick, axisStyle));
            segments.AddRange(canvas.RenderRow(row));
            segments.Add(Segment.LineBreak);
        }

        segments.Add(new Segment(new string(' ', labelWidth + 1) + "└" + new string('─', canvas.Columns), axisStyle));
        segments.Add(Segment.LineBreak);

        if (ShowLegend)
            segments.AddRange(Legend(plotted));

        return segments;
    }

    private (double Min, double Max) AxisRange(List<ChartSeries> series)
    {
        var min = Min ?? series.Min(s => s.Values.Min());
        var max = Max ?? series.Max(s => s.Values.Max());

        // A flat line still needs a range to be drawn in.
        return max > min ? (min, max) : (min, min + 1);
    }

    /// <summary>One label per row: the top, middle, and bottom rows are labelled, the rest are blank.</summary>
    private List<string> AxisLabels(double min, double max)
    {
        var lastRow = Math.Max(1, Height - 1);

        return Enumerable.Range(0, Height)
            .Select(row => row == 0 || row == Height / 2 || row == Height - 1
                ? (max - (max - min) * row / lastRow).ToString(LabelFormat, CultureInfo.InvariantCulture)
                : string.Empty)
            .ToList();
    }

    /// <summary>
    /// Joins each value to the next with a straight line.
    /// </summary>
    private static void Plot(BrailleCanvas canvas, ChartSeries series, double min, double max)
    {
        var values = series.Values;

        (int X, int Y) DotFor(int index)
        {
            var across = values.Count == 1 ? 0 : (double)index / (values.Count - 1);
            var up = (Math.Clamp(values[index], min, max) - min) / (max - min);

            return ((int)Math.Round(across * (canvas.DotWidth - 1)), (int)Math.Round((1 - up) * (canvas.DotHeight - 1)));
        }

        var previous = DotFor(0);
        canvas.SetDot(previous.X, previous.Y, series.Colour);

        for (var index = 1; index < values.Count; index++)
        {
            var next = DotFor(index);
            canvas.DrawLine(previous, next, series.Colour);
            previous = next;
        }
    }

    private static IEnumerable<Segment> Legend(List<ChartSeries> series)
    {
        for (var index = 0; index < series.Count; index++)
        {
            if (index > 0)
                yield return new Segment("   ");

            yield return new Segment("⣿ " + series[index].Name, new Style(series[index].Colour));
        }

        yield return Segment.LineBreak;
    }
}
