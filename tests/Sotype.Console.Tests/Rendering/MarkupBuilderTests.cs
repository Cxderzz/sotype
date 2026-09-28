using Sotype.Cli.Rendering;
using Sotype.Cli.Theming;
using Sotype.Domain;

namespace Sotype.Cli.Tests.Rendering;

public class MarkupBuilderTests
{
    private static readonly Theme Theme = new(
        Name: "Test",
        Correct: "green",
        Incorrect: "red",
        Extra: "orange3",
        Pending: "grey",
        Accent: "yellow",
        CursorForeground: "black",
        CursorBackground: "yellow");

    private static IReadOnlyList<Word> Line(params string[] words)
    {
        var session = new TypingSession(TestConfiguration.ForWordCount(words.Length), words);
        return session.Words;
    }

    [Test]
    public void RenderLine_ShouldColourEachCharacterByItsState_ShowingTheTargetLetterThroughout()
    {
        var session = new TypingSession(TestConfiguration.ForWordCount(1), new[] { "cat" });
        session.TypeCharacter('c');
        session.TypeCharacter('x');

        MarkupBuilder.RenderLine(session.Words, Theme)
            .ShouldBe("[green]c[/][red]a[/][grey]t[/]");
    }

    [Test]
    public void RenderLine_ShouldDrawTheCaretAsTheReverseVideoedCharacter_WhenItSitsOnACellBoundary()
    {
        MarkupBuilder.RenderLine(Line("cat"), Theme, caretColumn: 0)
            .ShouldBe("[black on yellow]c[/][grey]at[/]");
    }

    [Test]
    public void RenderLine_ShouldSpanTheCaretAcrossTwoCells_WhenItSitsBetweenThem()
    {
        // Half a cell along: the trailing half of 'c' and the leading half of 'a'.
        MarkupBuilder.RenderLine(Line("cat"), Theme, caretColumn: 0.5)
            .ShouldBe("[invert yellow]▌[/][yellow]▌[/][grey]t[/]");
    }

    [Test]
    public void RenderLine_ShouldRoundTheCaretToTheNearestEighthOfACell()
    {
        MarkupBuilder.RenderLine(Line("cat"), Theme, caretColumn: 0.1)
            .ShouldBe("[invert yellow]▏[/][yellow]▏[/][grey]t[/]");
    }

    [Test]
    public void RenderLine_ShouldTreatACaretRoundingUpToAWholeCell_AsSittingOnTheNextCell()
    {
        MarkupBuilder.RenderLine(Line("cat"), Theme, caretColumn: 0.99)
            .ShouldBe("[grey]c[/][black on yellow]a[/][grey]t[/]");
    }

    [Test]
    public void RenderLine_ShouldOmitTheCaretEntirely_WhenNoColumnIsGiven()
    {
        MarkupBuilder.RenderLine(Line("cat"), Theme).ShouldNotContain("yellow");
    }

    [Test]
    public void RenderLine_ShouldPadTheLine_WhenTheCaretSitsPastItsLastCharacter()
    {
        // Nothing follows the final word to lend the caret a cell, so one is added for it.
        MarkupBuilder.RenderLine(Line("cat"), Theme, caretColumn: 3)
            .ShouldBe("[grey]cat[/][black on yellow] [/][grey] [/]");
    }

    [Test]
    public void RenderLine_ShouldPlaceTheCaretOnTheSeparatingSpace_WhenAWordIsFullyTyped()
    {
        var session = new TypingSession(TestConfiguration.ForWordCount(2), new[] { "hi", "yo" });
        session.TypeCharacter('h');
        session.TypeCharacter('i');

        var caret = LineWrapper.LocateCaret([session.Words], session.CurrentWord);

        MarkupBuilder.RenderLine(session.Words, Theme, caret.Column)
            .ShouldBe("[green]hi[/][black on yellow] [/][grey]yo[/]");
    }

    [Test]
    public void RenderLine_ShouldShowCharactersTypedPastAWordsEnd_AsTheyWereTyped()
    {
        var session = new TypingSession(TestConfiguration.ForWordCount(2), new[] { "hi", "yo" });
        session.TypeCharacter('h');
        session.TypeCharacter('i');
        session.TypeCharacter('z');

        MarkupBuilder.RenderLine(session.Words, Theme)
            .ShouldBe("[green]hi[/][orange3]z[/][grey] yo[/]");
    }

    [Test]
    public void RenderLine_ShouldEscapeMarkupCharacters_WhenTheTypedTextContainsThem()
    {
        var session = new TypingSession(TestConfiguration.ForWordCount(1), new[] { "[x]" });

        MarkupBuilder.RenderLine(session.Words, Theme).ShouldBe("[grey][[x]][/]");
    }
}
