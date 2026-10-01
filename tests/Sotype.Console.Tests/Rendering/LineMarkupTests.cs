using Sotype.Cli.Rendering;
using Sotype.Domain;
using Sotype.Domain.Themes;

namespace Sotype.Cli.Tests.Rendering;

public class LineMarkupTests
{
    private static readonly ThemeRecord ThemeRecord = new(
        Name: "Test",
        Correct: "green",
        Incorrect: "red",
        Extra: "orange3",
        Pending: "grey",
        Accent: "yellow",
        CursorForeground: "black",
        CursorBackground: "yellow");

    private static IReadOnlyList<Word> Words(params string[] words) =>
        new TypingSession(TestConfiguration.ForWordCount(words.Length), words).Words;

    [Test]
    public void Render_ShouldColourTheTargetCharacters_ByWhatWasTypedOverThem()
    {
        var session = new TypingSession(TestConfiguration.ForWordCount(1), new[] { "cat" });
        session.TypeCharacter('c');
        session.TypeCharacter('x');

        LineMarkup.Render(session.Words, ThemeRecord).ShouldBe("[green]c[/][red]a[/][grey]t[/]");
    }

    [Test]
    public void Render_ShouldShowCharactersTypedPastAWordsEnd_AsTheyWereTyped()
    {
        var session = new TypingSession(TestConfiguration.ForWordCount(2), new[] { "hi", "yo" });
        session.TypeCharacter('h');
        session.TypeCharacter('i');
        session.TypeCharacter('z');

        LineMarkup.Render(session.Words, ThemeRecord).ShouldBe("[green]hi[/][orange3]z[/][grey] yo[/]");
    }

    [Test]
    public void Render_ShouldReverseVideoTheCharacterUnderTheCaret_WhenItSitsOnACellBoundary()
    {
        LineMarkup.Render(Words("cat"), ThemeRecord, caretColumn: 0)
            .ShouldBe("[black on yellow]c[/][grey]at[/]");
    }

    [Test]
    public void Render_ShouldSpanTheCaretAcrossTwoCells_WhenItSitsBetweenThem()
    {
        LineMarkup.Render(Words("cat"), ThemeRecord, caretColumn: 0.5)
            .ShouldBe("[invert yellow]▌[/][yellow]▌[/][grey]t[/]");
    }

    [Test]
    public void Render_ShouldRoundTheCaret_ToTheNearestEighthOfACell()
    {
        LineMarkup.Render(Words("cat"), ThemeRecord, caretColumn: 0.1)
            .ShouldBe("[invert yellow]▏[/][yellow]▏[/][grey]t[/]");
    }

    [Test]
    public void Render_ShouldTreatACaretRoundingUpToAWholeCell_AsSittingOnTheNextCell()
    {
        LineMarkup.Render(Words("cat"), ThemeRecord, caretColumn: 0.99)
            .ShouldBe("[grey]c[/][black on yellow]a[/][grey]t[/]");
    }

    [Test]
    public void Render_ShouldPadTheLine_WhenTheCaretSitsPastItsLastCharacter()
    {
        LineMarkup.Render(Words("cat"), ThemeRecord, caretColumn: 3)
            .ShouldBe("[grey]cat[/][black on yellow] [/][grey] [/]");
    }

    [Test]
    public void Render_ShouldOmitTheCaret_WhenNoColumnIsGiven()
    {
        LineMarkup.Render(Words("cat"), ThemeRecord).ShouldNotContain("yellow");
    }

    [Test]
    public void Render_ShouldEscapeMarkupCharacters_WhenTheWordContainsThem()
    {
        LineMarkup.Render(Words("[x]"), ThemeRecord).ShouldBe("[grey][[x]][/]");
    }
}
