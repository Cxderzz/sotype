using Sotype.Cli.Input;
using Sotype.Cli.Screens;
using Sotype.Domain;
using Sotype.Domain.Configuration;
using Sotype.Domain.History;
using Sotype.Domain.Themes;
using Sotype.Domain.Words;
using Spectre.Console;

namespace Sotype.Cli.App;

/// <summary>
/// Drives the screen flow: menu, test, results, then restart, menu, or quit.
/// </summary>
public sealed class SotypeApp(
    IAnsiConsole console,
    TimeProvider timeProvider,
    IWordListProvider wordListProvider,
    IHistoryRepository historyRepository,
    IPreferencesRepository preferencesRepository,
    IThemeRepository themeRepository)
{
    public async Task RunAsync()
    {
        var preferences = preferencesRepository.Load();

        while (true)
        {
            var (configuration, theme) = MenuScreen.Show(console, preferences, themeRepository);

            while (true)
            {
                var session = CreateSession(configuration);
                var testOutcome = await new TestScreen(console, session, theme, timeProvider).RunAsync();

                if (testOutcome == InputEvent.Restart)
                    continue;

                if (testOutcome == InputEvent.BackToMenu)
                    break;

                var previousBest = PreviousBestWpm(configuration);
                preferences = Record(session.Result, configuration, theme);

                var resultsOutcome = ResultsScreen.Show(console, session.Result, previousBest, theme);

                if (resultsOutcome == InputEvent.Quit)
                    return;

                if (resultsOutcome == InputEvent.BackToMenu)
                    break;
            }
        }
    }

    private TypingSession CreateSession(TestConfiguration configuration) => configuration.Mode == TestMode.Time
        ? new TypingSession(configuration, wordListProvider, timeProvider)
        : new TypingSession(configuration, wordListProvider.TakeRandomWords(configuration.WordCount!.Value), timeProvider);

    private double? PreviousBestWpm(TestConfiguration configuration) => historyRepository.GetAll()
        .Where(record => record.Mode == configuration.Mode
            && record.Duration == configuration.Duration
            && record.WordCount == configuration.WordCount)
        .Select(record => (double?)record.Wpm)
        .Max();

    private UserPreferences Record(TestResult result, TestConfiguration configuration, ThemeRecord themeRecord)
    {
        historyRepository.Add(new RunRecord(
            Timestamp: timeProvider.GetLocalNow(),
            Mode: configuration.Mode,
            Duration: configuration.Duration,
            WordCount: configuration.WordCount,
            Wpm: result.Wpm,
            RawWpm: result.RawWpm,
            Accuracy: result.Accuracy,
            CorrectCharacters: result.CorrectCharacters,
            IncorrectCharacters: result.IncorrectCharacters,
            ExtraCharacters: result.ExtraCharacters,
            MissedCharacters: result.MissedCharacters,
            ThemeName: themeRecord.Name));

        var preferences = new UserPreferences(
            configuration.Mode, configuration.Duration, configuration.WordCount, themeRecord.Name);

        preferencesRepository.Save(preferences);

        return preferences;
    }
}
