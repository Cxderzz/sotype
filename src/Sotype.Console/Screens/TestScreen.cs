using System.Diagnostics;
using Sotype.Cli.Input;
using Sotype.Cli.Rendering;
using Sotype.Cli.Terminal;
using Sotype.Cli.Theming;
using Sotype.Domain;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sotype.Cli.Screens;

/// <summary>
/// Runs one typing test. The loop keeps a steady frame rate so the caret has frames to animate
/// in between keystrokes, and redraws only when the screen would look different.
/// </summary>
public sealed class TestScreen
{
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(16);
    private static readonly TimeSpan BlinkDelay = TimeSpan.FromMilliseconds(900);
    private static readonly TimeSpan BlinkInterval = TimeSpan.FromMilliseconds(530);

    private readonly TypingSession _session;
    private readonly TestView _view;
    private readonly CaretAnimator _caret = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    private WordLayout _layout;
    private TimeSpan _lastFrameAt;
    private TimeSpan _lastInputAt;
    private InputEvent _outcome = InputEvent.None;

    public TestScreen(TypingSession session, Theme theme)
    {
        _session = session;
        _view = new TestView(theme);
        _layout = Layout();
    }

    /// <returns>
    /// <see cref="InputEvent.Restart"/> or <see cref="InputEvent.BackToMenu"/> if the user asked
    /// for one mid-test, otherwise <see cref="InputEvent.None"/> once the session finished.
    /// </returns>
    public async Task<InputEvent> RunAsync()
    {
        // A restart starts a new Live display, which would otherwise draw below the old panel.
        AnsiConsole.Clear();

        await AnsiConsole.Live(Render())
            .AutoClear(false)
            .Overflow(VerticalOverflow.Crop)
            .StartAsync(async ctx =>
            {
                var drawn = CurrentFrame();

                while (!_session.IsFinished)
                {
                    var startedAt = _clock.Elapsed;

                    if (ReadInput() is { } signal)
                    {
                        _outcome = signal;
                        return;
                    }

                    _session.Tick();
                    Advance(startedAt);

                    var frame = CurrentFrame();
                    if (frame != drawn)
                    {
                        drawn = frame;
                        Draw(ctx);
                    }

                    await WaitForNextFrame(startedAt);
                }

                Draw(ctx);
            });

        return _outcome;
    }

    /// <summary>Everything that changes what the screen shows.</summary>
    private readonly record struct Frame(
        int CaretLine,
        double CaretColumn,
        bool CaretVisible,
        string Header,
        int Width,
        int Height);

    private Frame CurrentFrame() => new(
        _caret.Line,
        _caret.Column,
        IsCaretVisible(),
        TestView.Header(_session),
        Console.WindowWidth,
        Console.WindowHeight);

    /// <summary>Drains the queue so a burst of keystrokes is not spread over later frames.</summary>
    private InputEvent? ReadInput()
    {
        while (Console.KeyAvailable)
        {
            var signal = InputReader.Dispatch(Console.ReadKey(intercept: true), _session);
            _lastInputAt = _clock.Elapsed;

            if (signal != InputEvent.None)
                return signal;
        }

        return null;
    }

    private void Advance(TimeSpan now)
    {
        _layout = Layout();
        _caret.Advance(_layout.CaretLine, _layout.CaretColumn, now - _lastFrameAt);
        _lastFrameAt = now;
    }

    /// <summary>The caret holds steady while typing and blinks once the typist pauses.</summary>
    private bool IsCaretVisible()
    {
        var idle = _clock.Elapsed - _lastInputAt;

        if (_caret.IsMoving || idle < BlinkDelay)
            return true;

        return (idle - BlinkDelay).Ticks / BlinkInterval.Ticks % 2 == 0;
    }

    private async Task WaitForNextFrame(TimeSpan startedAt)
    {
        var remaining = FrameInterval - (_clock.Elapsed - startedAt);

        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining);
    }

    private void Draw(LiveDisplayContext ctx)
    {
        ctx.UpdateTarget(Render());
        SynchronizedOutput.Draw(ctx.Refresh);
    }

    private IRenderable Render() => _view.Render(_session, _layout, IsCaretVisible() ? _caret.Column : null);

    private WordLayout Layout() =>
        WordLayout.Create(_session.Words, _session.CurrentWord, Math.Max(20, Console.WindowWidth - 8));
}
