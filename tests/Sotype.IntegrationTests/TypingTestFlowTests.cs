using Sotype.Domain;
using Sotype.Domain.Configuration;
using Sotype.Domain.History;
using Sotype.IntegrationTests.Harness;

namespace Sotype.IntegrationTests;

/// <summary>
/// End-to-end runs of the app: menu, typing test, results, persistence. Each test scripts the
/// keys a user would press, so a change can be checked without sitting through a real test.
/// </summary>
public class TypingTestFlowTests
{
    private static readonly string[] TenWords =
        ["alpha", "beta", "gamma", "delta", "echo", "fox", "golf", "hotel", "india", "joker"];

    private static readonly string TenWordsTyped = string.Join(' ', TenWords);

    private static readonly UserPreferences TenWordTest = new(TestMode.Words, null, 10, "SerikaDark");

    private static readonly UserPreferences FifteenSecondTest = new(TestMode.Time, TimeSpan.FromSeconds(15), null, "SerikaDark");

    private SotypeHarness _harness = null!;

    [SetUp]
    public void SetUp() => _harness = new SotypeHarness(TenWords);

    [TearDown]
    public void TearDown() => _harness.Dispose();

    /// <summary>Accepts the mode, length, and theme the menu opens on.</summary>
    private ScriptedInput AcceptMenuDefaults() => _harness.Input.Enter().Enter().Enter();

    [Test]
    public async Task WordsTest_ShouldRecordARunWithExactStats_WhenTypedPerfectly()
    {
        _harness.WithPreferences(TenWordTest);
        AcceptMenuDefaults()
            .TypeOver(TenWordsTyped, TimeSpan.FromMinutes(1))
            .Escape();

        await _harness.RunAsync();

        var correctCharacters = TenWords.Sum(word => word.Length);
        var run = _harness.History.GetAll().ShouldHaveSingleItem();
        run.Mode.ShouldBe(TestMode.Words);
        run.WordCount.ShouldBe(10);
        run.CorrectCharacters.ShouldBe(correctCharacters);
        run.IncorrectCharacters.ShouldBe(0);
        run.Accuracy.ShouldBe(100);
        run.Wpm.ShouldBe(correctCharacters / 5.0, tolerance: 0.001);
    }

    [Test]
    public async Task WordsTest_ShouldCountMistakes_WhenTypedWithTyposAndExtras()
    {
        _harness.WithPreferences(TenWordTest);
        AcceptMenuDefaults()
            .Type("alphx betaa ")                         // one incorrect, one extra
            .Type(string.Join(' ', TenWords[2..]))
            .Escape();

        await _harness.RunAsync();

        var run = _harness.History.GetAll().ShouldHaveSingleItem();
        run.IncorrectCharacters.ShouldBe(1);
        run.ExtraCharacters.ShouldBe(1);
        run.Accuracy.ShouldBeLessThan(100);
    }

    [Test]
    public async Task WordsTest_ShouldLetBackspaceFixAMistake()
    {
        _harness.WithPreferences(TenWordTest);
        AcceptMenuDefaults()
            .Type("alx").Backspace().Type("pha ")
            .Type(string.Join(' ', TenWords[1..]))
            .Escape();

        await _harness.RunAsync();

        var run = _harness.History.GetAll().ShouldHaveSingleItem();
        run.IncorrectCharacters.ShouldBe(0);
        run.Accuracy.ShouldBe(100);
    }

    [Test]
    public async Task TimeTest_ShouldFinishWhenTheClockRunsOut()
    {
        _harness.WithPreferences(FifteenSecondTest);
        AcceptMenuDefaults()
            .Type("alpha beta")
            .Wait(TimeSpan.FromSeconds(15))
            .Escape();

        await _harness.RunAsync();

        var run = _harness.History.GetAll().ShouldHaveSingleItem();
        run.Mode.ShouldBe(TestMode.Time);
        run.Duration.ShouldBe(TimeSpan.FromSeconds(15));
        run.CorrectCharacters.ShouldBe("alphabeta".Length);
        run.Wpm.ShouldBe("alphabeta".Length / 5.0 / 0.25, tolerance: 0.001);
    }

    [Test]
    public async Task TimeTest_ShouldNotStartTheClockBeforeTheFirstKeystroke()
    {
        _harness.WithPreferences(FifteenSecondTest);
        AcceptMenuDefaults()
            .Wait(TimeSpan.FromMinutes(5))
            .Type("alpha")
            .Wait(TimeSpan.FromSeconds(15))
            .Escape();

        await _harness.RunAsync();

        _harness.History.GetAll().ShouldHaveSingleItem().CorrectCharacters.ShouldBe(5);
    }

    [Test]
    public async Task Tab_ShouldRestartTheTestWithoutRecordingARun()
    {
        _harness.WithPreferences(TenWordTest);
        AcceptMenuDefaults()
            .Type("xxxxx xxxx").Tab()
            .Type(TenWordsTyped)
            .Escape();

        await _harness.RunAsync();

        var run = _harness.History.GetAll().ShouldHaveSingleItem();
        run.IncorrectCharacters.ShouldBe(0);
    }

    [Test]
    public async Task Escape_ShouldReturnToTheMenuWithoutRecordingARun()
    {
        _harness.WithPreferences(TenWordTest);
        AcceptMenuDefaults()
            .Type("alpha").Escape();
        AcceptMenuDefaults()
            .Type(TenWordsTyped)
            .Escape();

        await _harness.RunAsync();

        _harness.History.GetAll().ShouldHaveSingleItem();
        _harness.Terminal.Output.Split("Select a mode").Length.ShouldBe(3);
    }

    [Test]
    public async Task ResultsScreen_ShouldAllowAnotherRunWithTab()
    {
        _harness.WithPreferences(TenWordTest);
        AcceptMenuDefaults()
            .Type(TenWordsTyped).Tab()
            .Type(TenWordsTyped).Escape();

        await _harness.RunAsync();

        _harness.History.GetAll().Count.ShouldBe(2);
    }

    [Test]
    public async Task Menu_ShouldSaveTheChosenSettingsAsPreferences()
    {
        // From the defaults (time, 30s, SerikaDark): pick words, 25 words, Dracula.
        _harness.Input
            .Down().Enter()
            .Down().Enter()
            .Down().Enter()
            .Type(string.Join(' ', Enumerable.Range(0, 25).Select(i => TenWords[i % TenWords.Length])))
            .Escape();

        await _harness.RunAsync();

        _harness.Preferences.Load().ShouldBe(new UserPreferences(TestMode.Words, null, 25, "Dracula"));
        _harness.History.GetAll().ShouldHaveSingleItem().ThemeName.ShouldBe("Dracula");
    }

    [Test]
    public async Task ResultsScreen_ShouldShowThePreviousBest_WhenNotBeaten()
    {
        _harness.WithPreferences(TenWordTest);
        _harness.History.Add(new RunRecord(
            DateTimeOffset.UnixEpoch, TestMode.Words, null, 10, Wpm: 999, RawWpm: 999, Accuracy: 100,
            CorrectCharacters: 0, IncorrectCharacters: 0, ExtraCharacters: 0, MissedCharacters: 0, ThemeName: "SerikaDark"));
        AcceptMenuDefaults()
            .TypeOver(TenWordsTyped, TimeSpan.FromMinutes(1))
            .Escape();

        await _harness.RunAsync();

        _harness.Terminal.Output.ShouldContain("personal best: 999 wpm");
    }

    [Test]
    public async Task TestScreen_ShouldRenderTheWordsAndProgress()
    {
        _harness.WithPreferences(TenWordTest);
        // A pause lets a frame be drawn mid-test, rather than the whole test typed in one frame.
        AcceptMenuDefaults()
            .Type("alpha ").Wait(TimeSpan.FromSeconds(1))
            .Type(string.Join(' ', TenWords[1..]))
            .Escape();

        await _harness.RunAsync();

        var output = _harness.Terminal.Output;
        output.ShouldContain("2/10");
        output.ShouldContain("alpha");
        output.ShouldContain("joker");
    }
}
