using Sotype.Cli.Input;
using Sotype.Cli.Theming;
using Sotype.Domain;
using Spectre.Console;

namespace Sotype.Cli.Rendering;

/// <summary>The post-test screen: final stats, a personal-best comparison, and the next action.</summary>
public static class ResultsScreen
{
    public static InputEvent Show(TestResult result, double? previousBestWpm, Theme theme)
    {
        AnsiConsole.Clear();

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey35);
        table.AddColumn("stat");
        table.AddColumn("value");
        table.AddRow("wpm", $"[{theme.Accent} bold]{result.Wpm:0.#}[/]");
        table.AddRow("raw wpm", $"{result.RawWpm:0.#}");
        table.AddRow("accuracy", $"{result.Accuracy:0.#}%");
        table.AddRow("correct", $"[{theme.Correct}]{result.CorrectCharacters}[/]");
        table.AddRow("incorrect", $"[{theme.Incorrect}]{result.IncorrectCharacters}[/]");
        table.AddRow("extra", $"[{theme.Extra}]{result.ExtraCharacters}[/]");
        table.AddRow("missed", $"{result.MissedCharacters}");

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine(BuildPersonalBestMessage(result, previousBestWpm));
        AnsiConsole.MarkupLine("[grey58]tab[/] restart    [grey58]enter/m[/] menu    [grey58]esc[/] quit");

        while (true)
        {
            switch (Console.ReadKey(intercept: true).Key)
            {
                case ConsoleKey.Tab:
                    return InputEvent.Restart;
                case ConsoleKey.Enter or ConsoleKey.M:
                    return InputEvent.BackToMenu;
                case ConsoleKey.Escape:
                    return InputEvent.Quit;
            }
        }
    }

    private static string BuildPersonalBestMessage(TestResult result, double? previousBestWpm)
    {
        if (previousBestWpm is not { } best)
            return "[green]New personal best![/]";

        return result.Wpm > best
            ? $"[green]New personal best! (+{result.Wpm - best:0.#} wpm)[/]"
            : $"[grey58]personal best: {best:0.#} wpm[/]";
    }
}
