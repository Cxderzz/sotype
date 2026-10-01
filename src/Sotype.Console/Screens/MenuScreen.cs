using Sotype.Domain;
using Sotype.Domain.Configuration;
using Sotype.Domain.Constants;
using Sotype.Domain.History;
using Sotype.Domain.Themes;
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

    public static (TestConfiguration Configuration, ThemeRecord Theme) Show(IAnsiConsole console, UserPreferences preferences, IThemeRepository themeRepository, IHistoryRepository historyRepository, TimeProvider timeProvider)
    {
        console.Clear();
        console.Write(new FigletText(StringLookups.AppName).Color(Color.Yellow));

        var screen = console.Prompt(
            new SelectionPrompt<Navigation>()
                .Title("Where would you like to go?")
                .AddChoices(Navigation.Test, Navigation.Stats)
                .UseConverter(nav => nav switch
                {
                    Navigation.Test => "Start a [yellow]test[/]",
                    Navigation.Stats => "View [yellow]stats[/]",
                    _ => throw new ArgumentOutOfRangeException(nameof(nav), nav, null)
                })
            );

        if (screen == Navigation.Test)
        {
            var mode = console.Prompt(
                new SelectionPrompt<TestMode>()
                    .Title("Select a [yellow]mode[/]")
                    .AddChoices(TestMode.Time, TestMode.Words)
                    .DefaultValue(preferences.Mode));

            var configuration = mode == TestMode.Time
                ? TestConfiguration.ForDuration(TimeSpan.FromSeconds(PromptDuration(console, preferences)))
                : TestConfiguration.ForWordCount(PromptWordCount(console, preferences));

            var themeName = console.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [yellow]theme[/]")
                    .AddChoices(themeRepository.GetAll().Select(theme => theme.Name))
                    .DefaultValue(preferences.ThemeName));

            return (configuration, themeRepository.GetByName(themeName));
        }

        if (screen == Navigation.Stats)
        {
            StatsScreen.Show(console, historyRepository.GetAll(), themeRepository.GetByName(preferences.ThemeName), timeProvider.GetUtcNow());
            return Show(console, preferences, themeRepository, historyRepository, timeProvider);
        }

        throw new Exception("Unexpected route.");
    }

    private static int PromptDuration(IAnsiConsole console, UserPreferences preferences) => console.Prompt(
        new SelectionPrompt<int>()
            .Title("Select a [yellow]duration[/]")
            .AddChoices(DurationChoicesSeconds)
            .DefaultValue((int)(preferences.Duration ?? TimeSpan.Zero).TotalSeconds)
            .UseConverter(seconds => $"{seconds}s"));

    private static int PromptWordCount(IAnsiConsole console, UserPreferences preferences) => console.Prompt(
        new SelectionPrompt<int>()
            .Title("Select a [yellow]word count[/]")
            .AddChoices(WordCountChoices)
            .DefaultValue(preferences.WordCount ?? 0)
            .UseConverter(count => $"{count} words"));
}
