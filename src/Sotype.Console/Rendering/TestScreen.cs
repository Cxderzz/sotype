using Sotype.Cli.Input;
using Sotype.Cli.Theming;
using Sotype.Domain;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sotype.Cli.Rendering;

/// <summary>
/// Drives one running <see cref="TypingSession"/>: an <c>AnsiConsole.Live</c> display refreshed
/// by our own loop, since Spectre has no live-typing input primitive of its own. Polls for a
/// keypress, forwards it through <see cref="InputReader"/>, ticks the session's clock, and
/// watches for a terminal resize — redrawing whenever any of those actually changed something.
/// </summary>
public static class TestScreen
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan IdlePollDelay = TimeSpan.FromMilliseconds(15);

    /// <returns>
    /// <see cref="InputEvent.Restart"/> or <see cref="InputEvent.BackToMenu"/> if the user asked
    /// for one mid-test, otherwise <see cref="InputEvent.None"/> once the session finished on its own.
    /// </returns>
    public static async Task<InputEvent> RunAsync(TypingSession session, Theme theme)
    {
        // Each call starts a brand new Live display with no knowledge of whatever the previous
        // test screen last rendered — without this, a fresh Tab-restart would render the new
        // panel below/alongside the old one instead of replacing it.
        AnsiConsole.Clear();

        var outcome = InputEvent.None;
        var lastTickAt = DateTime.UtcNow;
        var lastWidth = Console.WindowWidth;
        var lastHeight = Console.WindowHeight;

        await AnsiConsole.Live(BuildRenderable(session, theme))
            .AutoClear(false)
            .Overflow(VerticalOverflow.Crop)
            .StartAsync(async ctx =>
            {
                while (!session.IsFinished)
                {
                    var needsRedraw = false;

                    if (Console.KeyAvailable)
                    {
                        var signal = InputReader.Dispatch(Console.ReadKey(intercept: true), session);
                        if (signal != InputEvent.None)
                        {
                            outcome = signal;
                            return;
                        }

                        needsRedraw = true;
                    }

                    session.Tick();

                    if (Console.WindowWidth != lastWidth || Console.WindowHeight != lastHeight)
                    {
                        lastWidth = Console.WindowWidth;
                        lastHeight = Console.WindowHeight;
                        needsRedraw = true;
                    }

                    if (DateTime.UtcNow - lastTickAt > TickInterval)
                    {
                        lastTickAt = DateTime.UtcNow;
                        needsRedraw = true;
                    }

                    if (needsRedraw)
                    {
                        ctx.UpdateTarget(BuildRenderable(session, theme));
                        ctx.Refresh();
                    }
                    else
                    {
                        await Task.Delay(IdlePollDelay);
                    }
                }

                ctx.UpdateTarget(BuildRenderable(session, theme));
                ctx.Refresh();
            });

        return outcome;
    }

    private static IRenderable BuildRenderable(TypingSession session, Theme theme)
    {
        var availableWidth = Math.Max(20, Console.WindowWidth - 8);

        var content = new Rows(
            BuildHeader(session, theme),
            new Text(string.Empty),
            BuildWordsDisplay(session, theme, availableWidth),
            new Text(string.Empty),
            new Markup("[grey58]tab[/] restart    [grey58]esc[/] menu"));

        return new Panel(content)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey35)
            .Expand();
    }

    private static IRenderable BuildHeader(TypingSession session, Theme theme)
    {
        var label = session.Mode == TestMode.Time
            ? $"{Math.Max(0, (int)Math.Ceiling((session.Configuration.Duration!.Value - session.Elapsed).TotalSeconds))}s"
            : $"{Math.Min(session.CurrentWordIndex + 1, session.Words.Count)}/{session.Words.Count}";

        return new Markup($"[{theme.Accent} bold]{label}[/]");
    }

    private static IRenderable BuildWordsDisplay(TypingSession session, Theme theme, int availableWidth)
    {
        // Wrapped from the full word list every time (index 0 onward), not a slice around the
        // current word: a slice's boundaries shift by one word on every commit, and re-wrapping
        // from a different starting point can reassign words to different lines even though
        // nothing about them changed — visible as words jumping around while typing. Wrapping
        // is monotonic (appending words to the end never changes how earlier ones were grouped),
        // so this stays stable, and at a few hundred words it's computationally trivial anyway.
        var wrapped = LineWrapper.WrapIntoLines(session.Words, availableWidth);
        var visible = LineWrapper.SelectVisibleWindow(wrapped, session.CurrentWord);

        var lines = visible
            .Select(line => (IRenderable)new Markup(MarkupBuilder.RenderLine(line, session.CurrentWord, theme)))
            .ToList();

        return new Rows(lines);
    }
}
