using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sotype.Cli.Tests.Rendering.Charts;

/// <summary>
/// Renders a widget to plain text, without colours, at a fixed width.
/// </summary>
internal static class RenderedText
{
    public static string Of(IRenderable renderable, int width = 60)
    {
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(writer),
        });
        console.Profile.Width = width;

        console.Write(renderable);
        return writer.ToString();
    }
}
