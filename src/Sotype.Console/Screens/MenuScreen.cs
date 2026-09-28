using Sotype.Cli.Theming;
using Sotype.Domain;
using Sotype.Domain.Configuration;
using Spectre.Console;

namespace Sotype.Cli.Screens;

/// <summary>
/// Prompts for the mode, length, and theme of the next test.
/// </summary>
/// <remarks>
/// Each prompt opens on the last used value. A default that is not one of the choices, such as
/// the duration after a words-mode test, leaves the first choice selected.
/// </remarks>
public static class MenuScreen
{
    private static readonly int[] DurationChoicesSeconds = [15, 30, 60, 120];
    private static readonly int[] WordCountChoices = [10, 25, 50, 100];

    public static (TestConfiguration Configuration, Theme Theme) Show(UserPreferences preferences)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("sotype").Color(Color.Yellow));

        var mode = AnsiConsole.Prompt(
            new SelectionPrompt<TestMode>()
                .Title("Select a [yellow]mode[/]")
                .AddChoices(TestMode.Time, TestMode.Words)
                .DefaultValue(preferences.Mode));

        var configuration = mode == TestMode.Time
            ? TestConfiguration.ForDuration(TimeSpan.FromSeconds(PromptDuration(preferences)))
            : TestConfiguration.ForWordCount(PromptWordCount(preferences));

        var themeName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [yellow]theme[/]")
                .AddChoices(ThemeCatalog.All.Select(theme => theme.Name))
                .DefaultValue(preferences.ThemeName));

        return (configuration, ThemeCatalog.GetByName(themeName));
    }

    private static int PromptDuration(UserPreferences preferences) => AnsiConsole.Prompt(
        new SelectionPrompt<int>()
            .Title("Select a [yellow]duration[/]")
            .AddChoices(DurationChoicesSeconds)
            .DefaultValue((int)(preferences.Duration ?? TimeSpan.Zero).TotalSeconds)
            .UseConverter(seconds => $"{seconds}s"));

    private static int PromptWordCount(UserPreferences preferences) => AnsiConsole.Prompt(
        new SelectionPrompt<int>()
            .Title("Select a [yellow]word count[/]")
            .AddChoices(WordCountChoices)
            .DefaultValue(preferences.WordCount ?? 0)
            .UseConverter(count => $"{count} words"));
}
