using Sotype.Cli.Input;
using Sotype.Domain;

namespace Sotype.Cli.Tests.Input;

public class InputReaderTests
{
    private static TypingSession CreateSession() =>
        new(TestConfiguration.ForWordCount(2), new[] { "cat", "dog" });

    private static ConsoleKeyInfo Key(char keyChar, ConsoleKey key) => new(keyChar, key, false, false, false);

    [Test]
    public void Dispatch_ShouldTypeCharacterIntoTheSession_WhenKeyIsALetter()
    {
        var session = CreateSession();

        InputReader.Dispatch(Key('c', ConsoleKey.C), session);

        session.CurrentWord.Typed.Count.ShouldBe(1);
        session.CurrentWord.Typed[0].State.ShouldBe(CharacterState.Correct);
    }

    [Test]
    public void Dispatch_ShouldCommitTheCurrentWord_WhenKeyIsSpacebar()
    {
        var session = CreateSession();

        InputReader.Dispatch(Key(' ', ConsoleKey.Spacebar), session);

        session.CurrentWordIndex.ShouldBe(1);
    }

    [Test]
    public void Dispatch_ShouldRemoveTheLastTypedCharacter_WhenKeyIsBackspace()
    {
        var session = CreateSession();
        session.TypeCharacter('c');

        InputReader.Dispatch(Key('\b', ConsoleKey.Backspace), session);

        session.CurrentWord.Typed.ShouldBeEmpty();
    }

    [Test]
    public void Dispatch_ShouldReturnRestart_WhenKeyIsTab()
    {
        var outcome = InputReader.Dispatch(Key('\t', ConsoleKey.Tab), CreateSession());

        outcome.ShouldBe(InputEvent.Restart);
    }

    [Test]
    public void Dispatch_ShouldReturnBackToMenu_WhenKeyIsEscape()
    {
        var outcome = InputReader.Dispatch(Key((char)27, ConsoleKey.Escape), CreateSession());

        outcome.ShouldBe(InputEvent.BackToMenu);
    }

    [Test]
    public void Dispatch_ShouldReturnNoneAndTypeNothing_WhenKeyIsAnUnmappedControlKey()
    {
        var session = CreateSession();

        var outcome = InputReader.Dispatch(Key('\0', ConsoleKey.F1), session);

        outcome.ShouldBe(InputEvent.None);
        session.CurrentWord.Typed.ShouldBeEmpty();
    }
}
