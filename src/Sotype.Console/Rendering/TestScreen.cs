using System.Diagnostics;
using Sotype.Cli.Input;
using Sotype.Cli.Theming;
using Sotype.Domain;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sotype.Cli.Rendering;

public static class TestScreen
{
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(16); // ~60fps
    private static readonly TimeSpan BlinkAfterIdle = TimeSpan.FromMilliseconds(900);
    private static readonly TimeSpan BlinkHalfPeriod = TimeSpan.FromMilliseconds(530);

    // Some hellish code to support repainting the entire terminal at once to reduce screen tearing
    private const string BeginSynchronizedUpdate = "\x1b[?2026h";
    private const string EndSynchronizedUpdate = "\x1b[?2026l";

    public static async Task<InputEvent> RunAsync(TypingSession session, Theme theme)
    {
        AnsiConsole.Clear();

        var outcome = InputEvent.None;
        var caret = new CaretAnimator();
        var clock = Stopwatch.StartNew();

        var lastFrameAt = TimeSpan.Zero;
        var lastInputAt = TimeSpan.Zero;
        var lastWidth = Console.WindowWidth;
        var lastHeight = Console.WindowHeight;
        var lastHeader = HeaderLabel(session);
        var lastCaretVisible = true;

        var (window, _) = AdvanceLayout(session, caret, TimeSpan.Zero);

        await AnsiConsole.Live(BuildRenderable(session, theme, window, caret, showCaret: true))
            .AutoClear(false)
            .Overflow(VerticalOverflow.Crop)
            .StartAsync(async ctx =>
            {
                while (!session.IsFinished)
                {
                    var frameStartedAt = clock.Elapsed;
                    var delta = frameStartedAt - lastFrameAt;
                    lastFrameAt = frameStartedAt;

                    var needsRedraw = false;

                    // a fast typist (or a held key) can deliver several keystrokes within one frame, and leaving the rest queued
                    // would make input lag further behind the longer the burst ran.
                    while (Console.KeyAvailable)
                    {
                        var signal = InputReader.Dispatch(Console.ReadKey(intercept: true), session);
                        if (signal != InputEvent.None)
                        {
                            outcome = signal;
                            return;
                        }

                        lastInputAt = frameStartedAt;
                        needsRedraw = true;
                    }

                    session.Tick();

                    if (Console.WindowWidth != lastWidth || Console.WindowHeight != lastHeight)
                    {
                        lastWidth = Console.WindowWidth;
                        lastHeight = Console.WindowHeight;
                        needsRedraw = true;
                    }

                    // Skipped entirely on a frame where nothing can have moved: the caret only
                    // ever retargets in response to input, so a test sitting idle re-wraps
                    // nothing and costs no more than the keypress poll itself.
                    if (needsRedraw || caret.IsAnimating)
                    {
                        bool caretMoved;
                        (window, caretMoved) = AdvanceLayout(session, caret, delta);
                        needsRedraw |= caretMoved;
                    }

                    var header = HeaderLabel(session);
                    if (header != lastHeader)
                    {
                        lastHeader = header;
                        needsRedraw = true;
                    }

                    var caretVisible = IsCaretVisible(frameStartedAt - lastInputAt, caret.IsAnimating);
                    if (caretVisible != lastCaretVisible)
                    {
                        lastCaretVisible = caretVisible;
                        needsRedraw = true;
                    }

                    if (needsRedraw)
                        Draw(ctx, BuildRenderable(session, theme, window, caret, caretVisible));

                    var frameCost = clock.Elapsed - frameStartedAt;
                    if (frameCost < FrameInterval)
                        await Task.Delay(FrameInterval - frameCost);
                }

                Draw(ctx, BuildRenderable(session, theme, window, caret, showCaret: true));
            });

        return outcome;
    }

    /// <summary>
    /// Re-wraps the word list, works out where the caret belongs, and gives the animator its
    /// chance to move toward it.
    /// </summary>
    /// <remarks>
    /// Wrapped from the full word list every time (index 0 onward), not a slice around the
    /// current word: a slice's boundaries shift by one word on every commit, and re-wrapping from
    /// a different starting point can reassign words to different lines even though nothing about
    /// them changed — visible as words jumping around while typing. Wrapping is monotonic
    /// (appending words to the end never changes how earlier ones were grouped), so this stays
    /// stable, and at a few hundred words it's computationally trivial even once a frame.
    /// </remarks>
    private static (VisibleWindow Window, bool CaretMoved) AdvanceLayout(
        TypingSession session, CaretAnimator caret, TimeSpan delta)
    {
        var availableWidth = Math.Max(20, Console.WindowWidth - 8);

        var lines = LineWrapper.WrapIntoLines(session.Words, availableWidth);
        var target = LineWrapper.LocateCaret(lines, session.CurrentWord);
        var caretMoved = caret.Advance(target, delta);

        return (LineWrapper.SelectVisibleWindow(lines, target.Line), caretMoved);
    }

    private static bool IsCaretVisible(TimeSpan idleFor, bool caretIsAnimating)
    {
        if (caretIsAnimating || idleFor < BlinkAfterIdle)
            return true;

        return (idleFor - BlinkAfterIdle).Ticks / BlinkHalfPeriod.Ticks % 2 == 0;
    }

    private static void Draw(LiveDisplayContext ctx, IRenderable renderable)
    {
        ctx.UpdateTarget(renderable);

        if (!AnsiConsole.Profile.Capabilities.Ansi)
        {
            ctx.Refresh();
            return;
        }

        var writer = AnsiConsole.Profile.Out.Writer;
        writer.Write(BeginSynchronizedUpdate);
        ctx.Refresh();
        writer.Write(EndSynchronizedUpdate);
        writer.Flush();
    }

    private static IRenderable BuildRenderable(
        TypingSession session, Theme theme, VisibleWindow window, CaretAnimator caret, bool showCaret)
    {
        var content = new Rows(
            new Markup($"[{theme.Accent} bold]{HeaderLabel(session)}[/]"),
            new Text(string.Empty),
            BuildWordsDisplay(theme, window, caret, showCaret),
            new Text(string.Empty),
            new Markup("[grey58]tab[/] restart    [grey58]esc[/] menu"));

        return new Panel(content)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey35)
            .Expand();
    }

    private static string HeaderLabel(TypingSession session) => session.Mode == TestMode.Time
        ? $"{Math.Max(0, (int)Math.Ceiling((session.Configuration.Duration!.Value - session.Elapsed).TotalSeconds))}s"
        : $"{Math.Min(session.CurrentWordIndex + 1, session.Words.Count)}/{session.Words.Count}";

    private static IRenderable BuildWordsDisplay(
        Theme theme, VisibleWindow window, CaretAnimator caret, bool showCaret)
    {
        var lines = new List<IRenderable>(window.Lines.Count);

        for (var i = 0; i < window.Lines.Count; i++)
        {
            var caretColumn = showCaret && window.StartLine + i == caret.CurrentLine
                ? caret.Column
                : (double?)null;

            lines.Add(new Markup(MarkupBuilder.RenderLine(window.Lines[i], theme, caretColumn)));
        }

        return new Rows(lines);
    }
}
