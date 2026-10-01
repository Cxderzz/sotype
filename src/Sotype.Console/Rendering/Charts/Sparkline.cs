using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sotype.Cli.Rendering.Charts;

/// <summary>
/// A one-row trend line: one block character per value, scaled from the lowest value shown to the highest.
/// </summary>
/// <remarks>When there are more values than columns, only the most recent values are shown.</remarks>
public sealed class Sparkline(IReadOnlyList<double> values, Color colour) : Renderable
{
    private static readonly char[] Blocks = ['▁', '▂', '▃', '▄', '▅', '▆', '▇', '█'];

    /// <summary>
    /// The width in columns, or <see langword="null"/> for one column per value.
    /// </summary>
    public int? Width { get; init; }

    protected override Measurement Measure(RenderOptions options, int maxWidth)
    {
        var width = VisibleWidth(maxWidth);
        return new Measurement(width, width);
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var shown = values.TakeLast(VisibleWidth(maxWidth)).ToList();
        if (shown.Count == 0)
            yield break;

        var min = shown.Min();
        var range = shown.Max() - min;

        var blocks = shown.Select(value => range == 0
            ? Blocks[0]
            : Blocks[(int)Math.Round((value - min) / range * (Blocks.Length - 1))]);

        yield return new Segment(new string(blocks.ToArray()), new Style(colour));
    }

    private int VisibleWidth(int maxWidth) => Math.Min(Width ?? values.Count, maxWidth);
}
