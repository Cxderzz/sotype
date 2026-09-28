using Sotype.Cli.Rendering;
using Sotype.Domain;

namespace Sotype.Cli.Tests.Rendering;

public class WordLayoutTests
{
    private static TypingSession Session(params string[] words) =>
        new(TestConfiguration.ForWordCount(words.Length), words);

    private static WordLayout Layout(TypingSession session, int width) =>
        WordLayout.Create(session.Words, session.CurrentWord, width);

    [Test]
    public void Create_ShouldFillEachLineUpToTheWrapWidth()
    {
        var layout = Layout(Session("one", "two", "three"), width: 8);

        layout.Lines.Select(line => line.Select(word => word.Target))
            .ShouldBe([new[] { "one", "two" }, new[] { "three" }]);
    }

    [Test]
    public void Create_ShouldPutTheCaretOnTheNextCharacterOfTheCurrentWord()
    {
        var session = Session("one", "two");
        session.Commit();
        session.TypeCharacter('t');

        Layout(session, width: 40).CaretColumn.ShouldBe(5);
    }

    [Test]
    public void Create_ShouldPutTheCaretOnTheSeparatingSpace_WhenAWordIsFullyTyped()
    {
        var session = Session("one", "two");
        session.TypeCharacter('o');
        session.TypeCharacter('n');
        session.TypeCharacter('e');

        Layout(session, width: 40).CaretColumn.ShouldBe(3);
    }

    [Test]
    public void Create_ShouldAccountForExtraCharactersTypedPastAWordsEnd()
    {
        var session = Session("one", "two");
        session.TypeCharacter('o');
        session.TypeCharacter('n');
        session.TypeCharacter('e');
        session.TypeCharacter('x');

        Layout(session, width: 40).CaretColumn.ShouldBe(4);
    }

    [Test]
    public void Create_ShouldReportTheLineTheCurrentWordWrappedOnto()
    {
        var session = Session("one", "two", "three");
        session.Commit();
        session.Commit();

        var layout = Layout(session, width: 8);

        layout.CaretLine.ShouldBe(1);
        layout.CaretColumn.ShouldBe(0);
    }

    [Test]
    public void Create_ShouldShowTheCaretsLineWithOneLineEitherSide()
    {
        var session = Session("a", "b", "c", "d", "e");
        session.Commit();
        session.Commit();
        session.Commit();

        var layout = Layout(session, width: 1);

        layout.Lines.Select(line => line[0].Target).ShouldBe(["c", "d", "e"]);
        layout.CaretRow.ShouldBe(1);
        layout.CaretLine.ShouldBe(3);
    }

    [Test]
    public void Create_ShouldClampTheVisibleLines_AtTheStartOfATest()
    {
        var layout = Layout(Session("a", "b", "c"), width: 1);

        layout.Lines.Count.ShouldBe(2);
        layout.CaretRow.ShouldBe(0);
    }
}
