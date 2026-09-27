namespace Sotype.Cli.Terminal;

/// <summary>
/// Owns the full-screen takeover: switches the terminal to its alternate screen buffer and
/// hides the cursor on construction, and guarantees exactly one restore — on disposal, on
/// Ctrl+C, and on unhandled-exception process exit — so the user's shell is never left in a
/// broken state. Spectre.Console has no built-in alternate-screen mode, so this is done with
/// the raw ANSI codes directly.
/// </summary>
public sealed class TerminalSession : IDisposable
{
    private const string EnterAlternateScreen = "\x1b[?1049h";
    private const string ExitAlternateScreen = "\x1b[?1049l";

    private readonly object _restoreLock = new();
    private bool _restored;

    public TerminalSession()
    {
        Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        Console.Out.Write(EnterAlternateScreen);
        Console.Out.Flush();
        Console.CursorVisible = false;
    }

    public void Dispose()
    {
        Restore();
        Console.CancelKeyPress -= OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        GC.SuppressFinalize(this);
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        // Restore the terminal ourselves before the process exits so the shell prompt reappears
        // cleanly, rather than leaving it stuck on the alternate screen with the cursor hidden.
        e.Cancel = true;
        Restore();
        Environment.Exit(130); // 128 + SIGINT, the conventional exit code for Ctrl+C
    }

    private void OnProcessExit(object? sender, EventArgs e) => Restore();

    private void Restore()
    {
        lock (_restoreLock)
        {
            if (_restored)
                return;

            _restored = true;
            Console.CursorVisible = true;
            Console.Out.Write(ExitAlternateScreen);
            Console.Out.Flush();
        }
    }
}
