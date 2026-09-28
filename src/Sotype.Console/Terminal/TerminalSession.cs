namespace Sotype.Cli.Terminal;

/// <summary>
/// Switches the terminal to its alternate screen buffer and hides the cursor, restoring both
/// exactly once: on disposal, on Ctrl+C, or on process exit.
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
        // Restore before exiting, rather than leaving the shell on the alternate screen.
        e.Cancel = true;
        Restore();
        Environment.Exit(130); // 128 + SIGINT
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
