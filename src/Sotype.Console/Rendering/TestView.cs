using Sotype.Domain;
using Sotype.Domain.Themes;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sotype.Cli.Rendering;

/// <summary>
/// Draws the test screen: progress header, words, and key hints.
/// </summary>
public sealed class TestView(Theme theme)
{
    /// <param name="caretColumn">
    /// Null hides the caret.
    /// </param>
    public IRenderable Render(TypingSession session, WordLayout layout, double? caretColumn)
    {
        var content = new Rows(
            new Markup($"[{theme.Accent} bold]{Header(session)}[/]"),
            new Text(string.Empty),
            new Rows(RenderLines(layout, caretColumn)),
            new Text(string.Empty),
            new Markup("[grey58]tab[/] restart    [grey58]esc[/] menu"));

        return new Panel(content)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey35)
            .Expand();
    }

    private IEnumerable<IRenderable> RenderLines(WordLayout layout, double? caretColumn) =>
        layout.Lines.Select((line, row) => (IRenderable)new Markup(
            LineMarkup.Render(line, theme, row == layout.CaretRow ? caretColumn : null)));

    /// <summary>
    /// Time remaining, or words committed.
    /// </summary>
    public static string Header(TypingSession session) => session.Mode == TestMode.Time
        ? $"{RemainingSeconds(session)}s"
        : $"{Math.Min(session.CurrentWordIndex + 1, session.Words.Count)}/{session.Words.Count}";

    private static int RemainingSeconds(TypingSession session) =>
        Math.Max(0, (int)Math.Ceiling((session.Configuration.Duration!.Value - session.Elapsed).TotalSeconds));
}
