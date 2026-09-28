using Spectre.Console;

namespace Sotype.Cli.Terminal;

/// <summary>
/// Wraps a repaint in the terminal's synchronized update mode, so a frame is never shown
/// half drawn. Terminals without support ignore the sequences.
/// </summary>
public static class SynchronizedOutput
{
    private const string Begin = "\x1b[?2026h";
    private const string End = "\x1b[?2026l";

    public static void Draw(Action repaint)
    {
        if (!AnsiConsole.Profile.Capabilities.Ansi)
        {
            repaint();
            return;
        }

        var writer = AnsiConsole.Profile.Out.Writer;

        writer.Write(Begin);
        repaint();
        writer.Write(End);
        writer.Flush();
    }
}
