using NSubstitute;
using Sotype.Domain.Words;

namespace Sotype.Domain.Tests;

public class TypingSessionTests
{
    private static TypingSession CreateWordsModeSession(params string[] words) =>
        new(TestConfiguration.ForWordCount(words.Length), words);

    private static IWordListProvider CreateWordProvider(string word = "hello")
    {
        var provider = Substitute.For<IWordListProvider>();
        provider.TakeRandomWords(Arg.Any<int>())
            .Returns(callInfo => Enumerable.Repeat(word, callInfo.Arg<int>()).ToArray());
        return provider;
    }

    [Test]
    public void TypeCharacter_ShouldMarkCharacterCorrect_WhenTypedCharacterMatchesTarget()
    {
        var session = CreateWordsModeSession("cat");

        session.TypeCharacter('c');

        session.CurrentWord.Typed[0].State.ShouldBe(CharacterState.Correct);
    }

    [Test]
    public void TypeCharacter_ShouldMarkCharacterIncorrect_WhenTypedCharacterDoesNotMatchTarget()
    {
        var session = CreateWordsModeSession("cat");

        session.TypeCharacter('x');

        session.CurrentWord.Typed[0].State.ShouldBe(CharacterState.Incorrect);
    }

    [Test]
    public void TypeCharacter_ShouldMarkCharacterExtra_WhenTypedBeyondTargetLength()
    {
        var session = CreateWordsModeSession("cat", "dog");

        foreach (var ch in "catxx")
            session.TypeCharacter(ch);

        session.CurrentWord.Typed[3].State.ShouldBe(CharacterState.Extra);
        session.CurrentWord.Typed[4].State.ShouldBe(CharacterState.Extra);
    }

    [Test]
    public void TypeCharacter_ShouldBeIgnored_WhenExtraCharacterCapReached()
    {
        var session = CreateWordsModeSession("cat", "dog");
        var maxLength = session.CurrentWord.MaxLength; // Target.Length * 2 == 6

        foreach (var ch in new string('x', maxLength))
            session.TypeCharacter(ch);

        session.TypeCharacter('x'); // one past the cap

        session.CurrentWord.Typed.Count.ShouldBe(maxLength);
    }

    [Test]
    public void TypeCharacter_ShouldNotCountTimeBeforeFirstKeystroke_WhenSessionSitsIdleBeforeTyping()
    {
        var session = new TypingSession(TestConfiguration.ForDuration(TimeSpan.FromSeconds(30)), CreateWordProvider());

        Thread.Sleep(150);
        session.TypeCharacter('h');
        var result = session.Finish();

        result.Elapsed.ShouldBeLessThan(TimeSpan.FromMilliseconds(100));
    }

    [Test]
    public void TypeCharacter_ShouldFinishSession_WhenFinalWordReachesTargetLengthWithoutTrailingSpace()
    {
        var session = CreateWordsModeSession("cat");

        session.TypeCharacter('c');
        session.TypeCharacter('a');
        session.TypeCharacter('t');

        session.IsFinished.ShouldBeTrue();
    }

    [Test]
    public void Backspace_ShouldRemoveLastTypedCharacter_WhenCurrentWordHasTypedCharacters()
    {
        var session = CreateWordsModeSession("cat");
        session.TypeCharacter('c');
        session.TypeCharacter('a');

        session.Backspace();

        session.CurrentWord.Typed.Count.ShouldBe(1);
    }

    [Test]
    public void Backspace_ShouldUncommitPreviousWord_WhenCurrentWordIsEmptyAndNotFirstWord()
    {
        var session = CreateWordsModeSession("cat", "dog");
        session.TypeCharacter('c');
        session.Commit(); // advances to "dog" with "cat" committed

        session.Backspace();

        session.CurrentWordIndex.ShouldBe(0);
        session.CurrentWord.IsCommitted.ShouldBeFalse();
        session.CurrentWord.Typed.Count.ShouldBe(1); // the backspace stepped back; it didn't also remove a char
    }

    [Test]
    public void Backspace_ShouldBeNoOp_WhenOnFirstWordWithNothingTyped()
    {
        var session = CreateWordsModeSession("cat", "dog");

        session.Backspace();

        session.CurrentWordIndex.ShouldBe(0);
        session.CurrentWord.Typed.ShouldBeEmpty();
    }

    [Test]
    public void Backspace_ShouldBeNoOp_WhenBackspacingIntoPreviousWordIsDisallowed()
    {
        var configuration = TestConfiguration.ForWordCount(2, allowBackspaceIntoPreviousWord: false);
        var session = new TypingSession(configuration, new[] { "cat", "dog" });
        session.TypeCharacter('c');
        session.Commit();

        session.Backspace();

        session.CurrentWordIndex.ShouldBe(1);
    }

    [Test]
    public void Commit_ShouldAdvanceToNextWord_WhenNotOnFinalWord()
    {
        var session = CreateWordsModeSession("cat", "dog");

        session.Commit();

        session.CurrentWordIndex.ShouldBe(1);
        session.IsFinished.ShouldBeFalse();
    }

    [Test]
    public void Commit_ShouldFinishSession_WhenCommittingFinalWordInWordsMode()
    {
        var session = CreateWordsModeSession("cat", "dog");
        session.Commit();

        session.Commit();

        session.IsFinished.ShouldBeTrue();
    }

    [Test]
    public void Constructor_ShouldRequestInitialWordBuffer_WhenCreatingTimeModeSession()
    {
        var provider = CreateWordProvider();

        _ = new TypingSession(TestConfiguration.ForDuration(TimeSpan.FromSeconds(30)), provider);

        provider.Received(1).TakeRandomWords(50);
    }

    [Test]
    public void Commit_ShouldTopUpTheWordBuffer_WhenRemainingWordsNearsTheRefillThreshold()
    {
        var provider = CreateWordProvider();
        var session = new TypingSession(TestConfiguration.ForDuration(TimeSpan.FromMinutes(5)), provider);
        provider.ClearReceivedCalls(); // ignore the constructor's initial fill

        for (var i = 0; i < 40; i++)
            session.Commit();

        provider.Received(1).TakeRandomWords(Arg.Any<int>());
    }

    [Test]
    public void Tick_ShouldFinishSessionMidWord_WhenDurationHasElapsed()
    {
        var session = new TypingSession(TestConfiguration.ForDuration(TimeSpan.FromMilliseconds(1)), CreateWordProvider());
        session.TypeCharacter('h');
        session.TypeCharacter('e'); // 2 of 5 characters typed; 3 remain

        Thread.Sleep(15);
        session.Tick();

        session.IsFinished.ShouldBeTrue();
        session.Result.MissedCharacters.ShouldBe(3);
    }

    [Test]
    public void Tick_ShouldBeNoOp_WhenNoCharacterHasBeenTypedYet()
    {
        var session = new TypingSession(TestConfiguration.ForDuration(TimeSpan.FromMilliseconds(1)), CreateWordProvider());

        Thread.Sleep(15);
        session.Tick();

        session.IsFinished.ShouldBeFalse();
    }

    [Test]
    public void Finish_ShouldCountWholeWordAsMissed_WhenWordIsSkippedWithoutTyping()
    {
        var session = CreateWordsModeSession("cat", "dog");

        session.Commit(); // skip "cat" entirely
        var result = session.Finish();

        result.MissedCharacters.ShouldBe(3); // the untouched "cat"
    }

    [Test]
    public void Finish_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        var session = CreateWordsModeSession("cat");
        session.TypeCharacter('c');

        var first = session.Finish();
        var second = session.Finish();

        second.ShouldBe(first);
    }

    [Test]
    public void Finish_ShouldProduceExpectedStatistics_WhenTypingASentenceWithATypoThatGetsCorrected()
    {
        var session = CreateWordsModeSession("cat", "dog");

        session.TypeCharacter('c');
        session.TypeCharacter('x'); // typo
        session.Backspace(); // removes the 'x' outright — v1 counts final state, not keystroke history
        session.TypeCharacter('a');
        session.TypeCharacter('t');
        session.Commit();
        session.TypeCharacter('d');
        session.TypeCharacter('o');
        session.TypeCharacter('g'); // finishes the session (final word, no trailing space)

        var result = session.Finish();

        result.CorrectCharacters.ShouldBe(6); // c-a-t, d-o-g
        result.IncorrectCharacters.ShouldBe(0); // the typo was backspaced away before it could count
        result.ExtraCharacters.ShouldBe(0);
        result.MissedCharacters.ShouldBe(0);
    }

    [Test]
    public void Result_ShouldThrowInvalidOperationException_WhenSessionHasNotFinishedYet()
    {
        var session = CreateWordsModeSession("cat");

        Should.Throw<InvalidOperationException>(() => session.Result);
    }

    private static IEnumerable<TestCaseData> MutatingMethods()
    {
        yield return new TestCaseData((Action<TypingSession>)(s => s.TypeCharacter('a'))).SetName("TypeCharacter");
        yield return new TestCaseData((Action<TypingSession>)(s => s.Backspace())).SetName("Backspace");
        yield return new TestCaseData((Action<TypingSession>)(s => s.Commit())).SetName("Commit");
    }

    [TestCaseSource(nameof(MutatingMethods))]
    public void MutatingMethod_ShouldThrowInvalidOperationException_WhenSessionAlreadyFinished(Action<TypingSession> act)
    {
        var session = CreateWordsModeSession("cat");
        session.TypeCharacter('c');
        session.TypeCharacter('a');
        session.TypeCharacter('t'); // auto-finishes: final word, target length reached

        session.IsFinished.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => act(session));
    }

    [Test]
    public void Constructor_ShouldThrow_WhenWordsModeConstructorGivenTimeConfiguration()
    {
        var configuration = TestConfiguration.ForDuration(TimeSpan.FromSeconds(30));

        Should.Throw<ArgumentException>(() => new TypingSession(configuration, new[] { "cat" }));
    }

    [Test]
    public void Constructor_ShouldThrow_WhenTimeModeConstructorGivenWordsConfiguration()
    {
        var configuration = TestConfiguration.ForWordCount(10);

        Should.Throw<ArgumentException>(() => new TypingSession(configuration, CreateWordProvider()));
    }

    [Test]
    public void Constructor_ShouldThrow_WhenWordsModeConfigurationGivenEmptyWordList()
    {
        var configuration = TestConfiguration.ForWordCount(1);

        Should.Throw<ArgumentException>(() => new TypingSession(configuration, Array.Empty<string>()));
    }

    [Test]
    public void Constructor_ShouldThrow_WhenConfiguredWordCountDoesNotMatchSuppliedWordListLength()
    {
        var configuration = TestConfiguration.ForWordCount(2);

        Should.Throw<ArgumentException>(() => new TypingSession(configuration, new[] { "cat" }));
    }
}
