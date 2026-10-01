using Sotype.Cli.Input;
using Sotype.Domain;
using Sotype.Domain.Themes;
using Spectre.Console;

namespace Sotype.Cli.Screens;

/// <summary>
/// Shows the finished test's stats and waits for the next action.
/// </summary>
public static class ResultsScreen
{
    public static InputEvent Show(IAnsiConsole console, TestResult result, double? previousBestWpm, ThemeRecord themeRecord)
    {
        console.Clear();

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey35);
        table.AddColumn("stat");
        table.AddColumn("value");
        table.AddRow("wpm", $"[{themeRecord.Accent} bold]{result.Wpm:0.#}[/]");
        table.AddRow("raw wpm", $"{result.RawWpm:0.#}");
        table.AddRow("accuracy", $"{result.Accuracy:0.#}%");
        table.AddRow("correct", $"[{themeRecord.Correct}]{result.CorrectCharacters}[/]");
        table.AddRow("incorrect", $"[{themeRecord.Incorrect}]{result.IncorrectCharacters}[/]");
        table.AddRow("extra", $"[{themeRecord.Extra}]{result.ExtraCharacters}[/]");
        table.AddRow("missed", $"{result.MissedCharacters}");

        console.Write(table);
        console.MarkupLine(PersonalBestMessage(result, previousBestWpm));
        console.MarkupLine("[grey58]tab[/] restart    [grey58]enter/m[/] menu    [grey58]esc[/] quit");

        while (true)
        {
            switch (console.Input.ReadKey(intercept: true)?.Key)
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

    private static string PersonalBestMessage(TestResult result, double? previousBestWpm)
    {
        if (previousBestWpm is not { } best)
            return "[green]New personal best![/]";

        return result.Wpm > best
            ? $"[green]New personal best! (+{result.Wpm - best:0.#} wpm)[/]"
            : $"[grey58]personal best: {best:0.#} wpm[/]";
    }
}
