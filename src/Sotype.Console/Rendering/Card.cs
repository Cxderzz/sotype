using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sotype.Cli.Rendering;

/// <summary>
/// The bordered box that groups content, so every section looks the same.
/// </summary>
public static class Card
{
    public static readonly Color BorderColour = Color.Grey35;

    /// <summary>
    /// A box that fills the available width, with <paramref name="title"/> set into its top border.
    /// </summary>
    public static Panel Create(string title, IRenderable content) => new Panel(content)
        .Header($"[grey58]{Markup.Escape(title)}[/]")
        .Border(BoxBorder.Rounded)
        .BorderColor(BorderColour)
        .Expand();
}
