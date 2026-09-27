using Sotype.Cli.Theming;
using Sotype.Domain;
using Sotype.Domain.Configuration;
using Spectre.Console;

namespace Sotype.Cli.Rendering;

/// <summary>
/// The pre-test configuration screen: mode, duration-or-word-count, and theme, via Spectre's
/// <see cref="SelectionPrompt{T}"/>. A custom inline top-bar config screen closer to monkeytype's
/// own look is a reasonable v2 polish item; this is fast to build and good enough for v1.
/// </summary>
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
                .AddChoices(PreferredFirst(new[] { TestMode.Time, TestMode.Words }, preferences.Mode)));

        var configuration = mode == TestMode.Time
            ? TestConfiguration.ForDuration(TimeSpan.FromSeconds(PromptDuration(preferences)))
            : TestConfiguration.ForWordCount(PromptWordCount(preferences));

        var themeName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [yellow]theme[/]")
                .AddChoices(PreferredFirst(ThemeCatalog.All.Select(t => t.Name), preferences.ThemeName)));

        return (configuration, ThemeCatalog.GetByName(themeName));
    }

    private static int PromptDuration(UserPreferences preferences)
    {
        var preferred = preferences.Mode == TestMode.Time ? (int?)preferences.Duration?.TotalSeconds : null;
        return AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title("Select a [yellow]duration[/]")
                .AddChoices(PreferredFirst(DurationChoicesSeconds, preferred))
                .UseConverter(seconds => $"{seconds}s"));
    }

    private static int PromptWordCount(UserPreferences preferences)
    {
        var preferred = preferences.Mode == TestMode.Words ? preferences.WordCount : null;
        return AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title("Select a [yellow]word count[/]")
                .AddChoices(PreferredFirst(WordCountChoices, preferred))
                .UseConverter(count => $"{count} words"));
    }

    /// <summary>
    /// Reorders <paramref name="choices"/> so the previously-used value comes first.
    /// <see cref="SelectionPrompt{T}"/> has no explicit "default selection" API, but it always
    /// highlights the first choice — so this is how the menu "remembers" the last run's settings.
    /// </summary>
    /// <remarks>
    /// <paramref name="preferred"/> is <see cref="object"/>, not the unconstrained <c>T?</c> it
    /// looks like it should be: without a <c>struct</c> constraint on <typeparamref name="T"/>,
    /// a nullable annotation on an unconstrained type parameter has no effect for value types, so
    /// callers could never actually pass an <c>int?</c> for a <c>PreferredFirst&lt;int&gt;</c> call.
    /// Comparing through <see cref="object"/> instead sidesteps that entirely — boxing a
    /// <see cref="Nullable{T}"/> with a value produces a plain boxed <typeparamref name="T"/>, so
    /// <see cref="Equals(object?, object?)"/> compares correctly regardless.
    /// </remarks>
    private static IEnumerable<T> PreferredFirst<T>(IEnumerable<T> choices, object? preferred) =>
        choices.OrderByDescending(choice => Equals(choice, preferred));
}
