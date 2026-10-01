using Sotype.Cli.App;
using Sotype.Domain.Configuration;
using Sotype.Domain.Constants;
using Sotype.Domain.Themes;
using Sotype.Infrastructure.Configuration;
using Sotype.Infrastructure.History;
using Sotype.Infrastructure.Theme;

namespace Sotype.IntegrationTests.Harness;

public sealed class SotypeHarness : IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"{StringLookups.AppName}-integration-{Guid.NewGuid()}");
    private readonly FixedWordListProvider _words;

    public SotypeHarness(params string[] words)
    {
        _words = new FixedWordListProvider(words);
        Input = new ScriptedInput(Clock);
        Terminal = new FakeTerminal(Input);
        History = new JsonHistoryRepository(Path.Combine(_directory, "history.json"));
        Preferences = new JsonPreferencesRepository(Path.Combine(_directory, "preferences.json"));
        Themes = new JsonThemeRepository(Path.Combine(_directory, "themes.json"));
    }

    public ManualClock Clock { get; } = new();

    public ScriptedInput Input { get; }

    public FakeTerminal Terminal { get; }

    public JsonHistoryRepository History { get; }

    public JsonPreferencesRepository Preferences { get; }

    public JsonThemeRepository Themes { get; }

    /// <summary>
    /// Saves preferences before the run, so the menu opens on known defaults.
    /// </summary>
    public SotypeHarness WithPreferences(UserPreferences preferences)
    {
        Preferences.Save(preferences);
        return this;
    }

    public SotypeHarness WithThemes(IEnumerable<ThemeRecord> themes)
    {
        Themes.SaveMultiple(themes);
        return this;
    }

    /// <summary>
    /// Runs the app until the script quits it. Fails, rather than hanging, if the script runs
    /// out while a test is still in progress.
    /// </summary>
    public async Task RunAsync(TimeSpan? timeout = null)
    {
        var app = new SotypeApp(Terminal, Clock, _words, History, Preferences, Themes);

        try
        {
            await app.RunAsync().WaitAsync(timeout ?? DefaultTimeout);
        }
        catch (TimeoutException)
        {
            Input.Abort();
            Assert.Fail(
                "The app did not quit before the timeout. The input script probably ran out " +
                $"mid-test. Last output:{Environment.NewLine}{Tail(Terminal.Output)}");
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private static string Tail(string text) => text.Length <= 2000 ? text : text[^2000..];
}
