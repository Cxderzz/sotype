using Spectre.Console;

namespace Sotype.IntegrationTests.Harness;

/// <summary>
/// Keyboard input played back from a script of keypresses and pauses. Pauses advance the
/// <see cref="ManualClock"/> at the point the app reaches them, so timing is deterministic.
/// </summary>
public sealed class ScriptedInput(ManualClock clock) : IAnsiConsoleInput
{
    private readonly Queue<Step> _steps = new();
    private readonly Lock _lock = new();

    private bool _aborted;

    private abstract record Step;

    private sealed record KeyStep(ConsoleKeyInfo Key) : Step;

    private sealed record WaitStep(TimeSpan Duration) : Step;

    public ScriptedInput Press(ConsoleKey key, char keyChar = '\0') =>
        Enqueue(new KeyStep(new ConsoleKeyInfo(keyChar, key, shift: false, alt: false, control: false)));

    public ScriptedInput Enter() => Press(ConsoleKey.Enter, '\r');

    public ScriptedInput Escape() => Press(ConsoleKey.Escape, '\e');

    public ScriptedInput Tab() => Press(ConsoleKey.Tab, '\t');

    public ScriptedInput Backspace() => Press(ConsoleKey.Backspace, '\b');

    public ScriptedInput Down(int times = 1)
    {
        for (var i = 0; i < times; i++)
            Press(ConsoleKey.DownArrow);

        return this;
    }

    /// <summary>Types each character, with a space committing the current word.</summary>
    public ScriptedInput Type(string text)
    {
        foreach (var character in text)
            Press(KeyFor(character), character);

        return this;
    }

    /// <summary>
    /// Types <paramref name="text"/> with the clock advanced evenly between keystrokes, so the
    /// time from the first keystroke to the last is exactly <paramref name="duration"/>.
    /// </summary>
    public ScriptedInput TypeOver(string text, TimeSpan duration)
    {
        var gap = text.Length > 1 ? duration / (text.Length - 1) : TimeSpan.Zero;

        for (var i = 0; i < text.Length; i++)
        {
            if (i > 0)
                Wait(gap);

            Type(text[i].ToString());
        }

        return this;
    }

    public ScriptedInput Wait(TimeSpan duration) => Enqueue(new WaitStep(duration));

    /// <summary>Makes every later read throw, stopping an app that is stuck waiting for input.</summary>
    public void Abort()
    {
        lock (_lock)
            _aborted = true;
    }

    /// <remarks>
    /// Reports no key right after a pause, so the app gets a frame to notice the time passing
    /// (e.g. a timed test running out) before the next keypress arrives.
    /// </remarks>
    public bool IsKeyAvailable()
    {
        lock (_lock)
            return !ApplyWaits() && _steps.Count > 0;
    }

    public ConsoleKeyInfo? ReadKey(bool intercept)
    {
        lock (_lock)
        {
            ApplyWaits();

            if (_steps.TryDequeue(out var step) && step is KeyStep keyStep)
                return keyStep.Key;

            throw new InvalidOperationException(
                "The input script ran out while the app was waiting for a key. " +
                "End the script with the keys that quit the app (e.g. Escape on the results screen).");
        }
    }

    public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken) =>
        Task.FromResult(ReadKey(intercept));

    private ScriptedInput Enqueue(Step step)
    {
        lock (_lock)
            _steps.Enqueue(step);

        return this;
    }

    /// <returns>Whether any pause was applied.</returns>
    private bool ApplyWaits()
    {
        if (_aborted)
            throw new OperationCanceledException("The input script was aborted.");

        var waited = false;

        while (_steps.TryPeek(out var step) && step is WaitStep wait)
        {
            _steps.Dequeue();
            clock.Advance(wait.Duration);
            waited = true;
        }

        return waited;
    }

    private static ConsoleKey KeyFor(char character) => character switch
    {
        ' ' => ConsoleKey.Spacebar,
        >= 'a' and <= 'z' => ConsoleKey.A + (character - 'a'),
        >= 'A' and <= 'Z' => ConsoleKey.A + (character - 'A'),
        >= '0' and <= '9' => ConsoleKey.D0 + (character - '0'),
        _ => ConsoleKey.NoName
    };
}
