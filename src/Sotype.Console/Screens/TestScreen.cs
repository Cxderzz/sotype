using Sotype.Cli.Input;
using Sotype.Cli.Rendering;
using Sotype.Cli.Terminal;
using Sotype.Domain;
using Sotype.Domain.Themes;
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

    private readonly IAnsiConsole _console;
    private readonly TypingSession _session;
    private readonly TestView _view;
    private readonly CaretAnimator _caret = new();
    private readonly TimeProvider _timeProvider;
    private readonly long _startedAt;

    private WordLayout _layout;
    private TimeSpan _lastFrameAt;
    private TimeSpan _lastInputAt;
    private InputEvent _outcome = InputEvent.None;

    public TestScreen(IAnsiConsole console, TypingSession session, ThemeRecord themeRecord, TimeProvider timeProvider)
    {
        _console = console;
        _session = session;
        _timeProvider = timeProvider;
        _startedAt = timeProvider.GetTimestamp();
        _view = new TestView(themeRecord);
        _layout = Layout();
    }

    /// <returns>
    /// <see cref="InputEvent.Restart"/> or <see cref="InputEvent.BackToMenu"/> if the user asked
    /// for one mid-test, otherwise <see cref="InputEvent.None"/> once the session finished.
    /// </returns>
    public async Task<InputEvent> RunAsync()
    {
        // A restart starts a new Live display, which would otherwise draw below the old panel.
        _console.Clear();

        await _console.Live(Render())
            .AutoClear(false)
            .Overflow(VerticalOverflow.Crop)
            .StartAsync(async ctx =>
            {
                var drawn = CurrentFrame();

                while (!_session.IsFinished)
                {
                    var startedAt = Now;

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
        _console.Profile.Width,
        _console.Profile.Height);

    private TimeSpan Now => _timeProvider.GetElapsedTime(_startedAt);

    /// <summary>
    /// Drains the queue so a burst of keystrokes is not spread over later frames. Stops once the
    /// session finishes, leaving any keys pressed after the final one for the results screen.
    /// </summary>
    private InputEvent? ReadInput()
    {
        while (!_session.IsFinished && _console.Input.IsKeyAvailable())
        {
            if (_console.Input.ReadKey(intercept: true) is not { } key)
                break;

            var signal = InputReader.Dispatch(key, _session);
            _lastInputAt = Now;

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

    ///<summary>
    /// The caret holds steady while typing and blinks once the typist pauses.
    /// </summary>
    private bool IsCaretVisible()
    {
        var idle = Now - _lastInputAt;

        if (_caret.IsMoving || idle < BlinkDelay)
            return true;

        return (idle - BlinkDelay).Ticks / BlinkInterval.Ticks % 2 == 0;
    }

    private async Task WaitForNextFrame(TimeSpan startedAt)
    {
        var remaining = FrameInterval - (Now - startedAt);

        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining, _timeProvider);
    }

    private void Draw(LiveDisplayContext ctx)
    {
        ctx.UpdateTarget(Render());
        SynchronizedOutput.Draw(_console, ctx.Refresh);
    }

    private IRenderable Render() => _view.Render(_session, _layout, IsCaretVisible() ? _caret.Column : null);

    private WordLayout Layout() =>
        WordLayout.Create(_session.Words, _session.CurrentWord, Math.Max(20, _console.Profile.Width - 8));
}
