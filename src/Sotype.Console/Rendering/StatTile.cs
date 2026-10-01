using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sotype.Cli.Rendering;

/// <summary>
/// A small box with one headline number: a label, the value, and optionally something under it,
/// such as a <see cref="Charts.Sparkline"/>.
/// </summary>
/// <remarks>
/// The label goes inside the box rather than in its border, because a border title that is wider
/// than the box's content gets cut short.
/// </remarks>
/// <param name="label">Plain text, such as "best wpm".</param>
/// <param name="value">Spectre markup, so the value can be styled, such as "[yellow bold]93.4[/]".</param>
public sealed class StatTile(string label, string value) : Renderable
{
    public IRenderable? Detail { get; init; }

    protected override Measurement Measure(RenderOptions options, int maxWidth) =>
        ((IRenderable)BuildPanel()).Measure(options, maxWidth);

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
        ((IRenderable)BuildPanel()).Render(options, maxWidth);

    private Panel BuildPanel()
    {
        List<IRenderable> lines = [new Markup($"[grey58]{Markup.Escape(label)}[/]"), new Markup(value)];

        if (Detail is not null)
            lines.Add(Detail);

        return new Panel(new Rows(lines))
            .Border(BoxBorder.Rounded)
            .BorderColor(Card.BorderColour);
    }
}
