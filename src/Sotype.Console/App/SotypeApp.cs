using Sotype.Cli.Input;
using Sotype.Cli.Rendering;
using Sotype.Domain;
using Sotype.Domain.Configuration;
using Sotype.Domain.History;
using Sotype.Domain.Words;

namespace Sotype.Cli.App;

/// <summary>
/// Orchestrates the screen flow: Menu → Test → Results → (restart | back to menu | quit).
/// The composition root (<c>Program.cs</c>) wires this up with the real infrastructure adapters.
/// </summary>
public sealed class SotypeApp(
    IWordListProvider wordListProvider,
    IHistoryRepository historyRepository,
    IPreferencesRepository preferencesRepository)
{
    public async Task RunAsync()
    {
        var preferences = preferencesRepository.Load();

        while (true)
        {
            var (configuration, theme) = MenuScreen.Show(preferences);

            var backToMenu = false;
            while (!backToMenu)
            {
                var session = CreateSession(configuration);
                var testOutcome = await TestScreen.RunAsync(session, theme);

                if (testOutcome == InputEvent.BackToMenu)
                {
                    backToMenu = true;
                    break;
                }

                if (testOutcome == InputEvent.Restart)
                    continue; // fresh session, same configuration and theme

                var result = session.Result;
                var previousBest = GetPreviousBestWpm(configuration);

                historyRepository.Add(ToRunRecord(result, configuration, theme.Name));
                preferences = new UserPreferences(configuration.Mode, configuration.Duration, configuration.WordCount, theme.Name);
                preferencesRepository.Save(preferences);

                switch (ResultsScreen.Show(result, previousBest, theme))
                {
                    case InputEvent.Restart:
                        continue;
                    case InputEvent.BackToMenu:
                        backToMenu = true;
                        break;
                    case InputEvent.Quit:
                        return;
                }
            }
        }
    }

    private TypingSession CreateSession(TestConfiguration configuration) => configuration.Mode == TestMode.Time
        ? new TypingSession(configuration, wordListProvider)
        : new TypingSession(configuration, wordListProvider.TakeRandomWords(configuration.WordCount!.Value));

    private double? GetPreviousBestWpm(TestConfiguration configuration) => historyRepository.GetAll()
        .Where(record => record.Mode == configuration.Mode
            && record.Duration == configuration.Duration
            && record.WordCount == configuration.WordCount)
        .Select(record => (double?)record.Wpm)
        .Max();

    private static RunRecord ToRunRecord(TestResult result, TestConfiguration configuration, string themeName) => new(
        Timestamp: DateTimeOffset.Now,
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
        ThemeName: themeName);
}
