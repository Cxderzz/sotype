using Sotype.Cli.Rendering;
using Sotype.Domain;

namespace Sotype.Cli.Tests.Rendering;

public class LineWrapperTests
{
    private static TypingSession Session(params string[] words) =>
        new(TestConfiguration.ForWordCount(words.Length), words);

    [Test]
    public void WrapIntoLines_ShouldFillEachLineUpToTheWrapWidth()
    {
        var lines = LineWrapper.WrapIntoLines(Session("one", "two", "three").Words, maxWidth: 8);

        lines.Select(line => line.Select(word => word.Target)).ShouldBe(
            [new[] { "one", "two" }, new[] { "three" }]);
    }

    [Test]
    public void LocateCaret_ShouldPointAtTheNextCharacterOfTheCurrentWord()
    {
        var session = Session("one", "two");
        session.Commit();
        session.TypeCharacter('t');

        var lines = LineWrapper.WrapIntoLines(session.Words, maxWidth: 40);

        LineWrapper.LocateCaret(lines, session.CurrentWord).ShouldBe(new CaretTarget(0, 5));
    }

    [Test]
    public void LocateCaret_ShouldAccountForExtraCharactersTypedPastAWordsEnd()
    {
        var session = Session("one", "two");
        session.TypeCharacter('o');
        session.TypeCharacter('n');
        session.TypeCharacter('e');
        session.TypeCharacter('x');

        var lines = LineWrapper.WrapIntoLines(session.Words, maxWidth: 40);

        LineWrapper.LocateCaret(lines, session.CurrentWord).ShouldBe(new CaretTarget(0, 4));
    }

    [Test]
    public void LocateCaret_ShouldReportTheLineTheCurrentWordWrappedOnto()
    {
        var session = Session("one", "two", "three");
        session.Commit();
        session.Commit();

        var lines = LineWrapper.WrapIntoLines(session.Words, maxWidth: 8);

        LineWrapper.LocateCaret(lines, session.CurrentWord).ShouldBe(new CaretTarget(1, 0));
    }

    [Test]
    public void SelectVisibleWindow_ShouldReturnTheCaretsLineWithOneLineEitherSide()
    {
        var session = Session("a", "b", "c", "d", "e");
        var lines = LineWrapper.WrapIntoLines(session.Words, maxWidth: 1);

        var window = LineWrapper.SelectVisibleWindow(lines, caretLine: 3);

        window.StartLine.ShouldBe(2);
        window.Lines.Select(line => line[0].Target).ShouldBe(["c", "d", "e"]);
    }

    [Test]
    public void SelectVisibleWindow_ShouldClampToTheAvailableLines_AtTheStartOfATest()
    {
        var session = Session("a", "b", "c");
        var lines = LineWrapper.WrapIntoLines(session.Words, maxWidth: 1);

        var window = LineWrapper.SelectVisibleWindow(lines, caretLine: 0);

        window.StartLine.ShouldBe(0);
        window.Lines.Count.ShouldBe(2);
    }
}
