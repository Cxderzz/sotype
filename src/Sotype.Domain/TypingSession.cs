using Sotype.Domain.Statistics;
using Sotype.Domain.Words;

namespace Sotype.Domain;

/// <summary>
/// Aggregate root for a single typing test attempt. This is the only entry point for mutating
/// state — <see cref="Word"/> exposes no public mutators — so every rule below is an invariant
/// the aggregate itself enforces, not something the UI layer has to remember to apply.
/// </summary>
public sealed class TypingSession
{
    private const int TimeModeInitialWordBufferSize = 50;
    private const int TimeModeRefillThreshold = 10;

    private readonly List<Word> _words = new();
    private readonly TestConfiguration _configuration;
    private readonly IWordListProvider? _wordListProvider;
    private readonly TimeProvider _timeProvider;

    private long? _startedAt;
    private TimeSpan? _finalElapsed;

    private int _currentWordIndex;
    private TestResult? _result;

    /// <summary>
    /// Creates a <see cref="TestMode.Words"/> session over an exact, pre-generated word list.
    /// </summary>
    /// <param name="timeProvider">The clock the session is timed against. Defaults to <see cref="TimeProvider.System"/>.</param>
    public TypingSession(TestConfiguration configuration, IReadOnlyList<string> words, TimeProvider? timeProvider = null)
    {
        if (configuration.Mode != TestMode.Words)
            throw new ArgumentException($"This constructor requires {TestMode.Words} configuration.", nameof(configuration));
        if (words.Count == 0)
            throw new ArgumentException("A words-mode session needs at least one word.", nameof(words));
        if (configuration.WordCount is { } expectedWordCount && expectedWordCount != words.Count)
            throw new ArgumentException(
                $"Configuration specifies {expectedWordCount} words but {words.Count} were supplied.", nameof(words));

        _configuration = configuration;
        _timeProvider = timeProvider ?? TimeProvider.System;
        foreach (var word in words)
            _words.Add(new Word(word));
    }

    /// <summary>
    /// Creates a <see cref="TestMode.Time"/> session backed by a word provider, which is drawn
    /// from as needed so the word stream never visibly runs out before the clock does.
    /// </summary>
    /// <param name="timeProvider">The clock the session is timed against. Defaults to <see cref="TimeProvider.System"/>.</param>
    public TypingSession(TestConfiguration configuration, IWordListProvider wordListProvider, TimeProvider? timeProvider = null)
    {
        if (configuration.Mode != TestMode.Time)
            throw new ArgumentException($"This constructor requires {TestMode.Time} configuration.", nameof(configuration));

        _configuration = configuration;
        _wordListProvider = wordListProvider;
        _timeProvider = timeProvider ?? TimeProvider.System;
        RefillWords(TimeModeInitialWordBufferSize);
    }

    public TestMode Mode => _configuration.Mode;

    public TestConfiguration Configuration => _configuration;

    public bool IsFinished => _result is not null;

    public IReadOnlyList<Word> Words => _words;

    public int CurrentWordIndex => _currentWordIndex;

    public Word CurrentWord => _words[_currentWordIndex];

    public TimeSpan Elapsed => _finalElapsed
        ?? (_startedAt is { } startedAt ? _timeProvider.GetElapsedTime(startedAt) : TimeSpan.Zero);

    /// <summary>The outcome of the session. Throws until <see cref="IsFinished"/> is true.</summary>
    public TestResult Result => _result ?? throw new InvalidOperationException("The session has not finished yet.");

    /// <summary>
    /// Types one character into the current word. A no-op once the word's extra-character cap
    /// is reached — that cap is an invariant, not something the caller needs to check first.
    /// </summary>
    public void TypeCharacter(char character)
    {
        EnsureNotFinished();
        EnsureStarted();

        if (!CurrentWord.TryTypeCharacter(character))
            return;

        if (Mode == TestMode.Words && IsOnFinalWord && CurrentWord.HasReachedTargetLength)
        {
            Finish();
            return;
        }

        MaybeRefillTimeModeBuffer();
    }

    /// <summary>
    /// Removes the last typed character. If the current word is already empty and isn't the
    /// first word, steps back into the previous word (without removing one of its characters —
    /// that happens on the next call) so it can be corrected, per <see cref="TestConfiguration.AllowBackspaceIntoPreviousWord"/>.
    /// </summary>
    public void Backspace()
    {
        EnsureNotFinished();

        if (CurrentWord.TryRemoveLastCharacter())
            return;

        if (_currentWordIndex == 0 || !_configuration.AllowBackspaceIntoPreviousWord)
            return;

        _currentWordIndex--;
        CurrentWord.Uncommit();
    }

    /// <summary>
    /// Commits the current word (the user pressed space) and advances. In <see cref="TestMode.Words"/>,
    /// committing the final word ends the session.
    /// </summary>
    public void Commit()
    {
        EnsureNotFinished();
        EnsureStarted();

        CurrentWord.Commit();

        if (Mode == TestMode.Words && IsOnFinalWord)
        {
            Finish();
            return;
        }

        _currentWordIndex++;
        MaybeRefillTimeModeBuffer();
    }

    /// <summary>
    /// Called periodically by the render loop so a <see cref="TestMode.Time"/> session notices
    /// the clock has run out even when the user isn't actively typing. A no-op for word-count
    /// sessions, before the first keystroke, and once already finished.
    /// </summary>
    public void Tick()
    {
        if (IsFinished || Mode != TestMode.Time || _startedAt is null)
            return;

        if (Elapsed >= _configuration.Duration!.Value)
            Finish();
    }

    /// <summary>
    /// Ends the session and computes its <see cref="TestResult"/>. Idempotent — calling this
    /// again (or letting <see cref="Tick"/>/completion logic call it) just returns the same result.
    /// </summary>
    public TestResult Finish()
    {
        if (_result is not null)
            return _result;

        var elapsed = Elapsed;
        _finalElapsed = elapsed;

        var (correct, incorrect, extra, missed) = CountCharacters();

        _result = new TestResult(
            Mode,
            elapsed,
            correct,
            incorrect,
            extra,
            missed,
            TypingStatisticsCalculator.CalculateWpm(correct, elapsed),
            TypingStatisticsCalculator.CalculateRawWpm(correct, incorrect, extra, elapsed),
            TypingStatisticsCalculator.CalculateAccuracy(correct, incorrect, extra));

        return _result;
    }

    private bool IsOnFinalWord => _currentWordIndex == _words.Count - 1;

    private void EnsureNotFinished()
    {
        if (IsFinished)
            throw new InvalidOperationException("Cannot type into a session that has already finished.");
    }

    private void EnsureStarted()
    {
        _startedAt ??= _timeProvider.GetTimestamp();
    }

    private void MaybeRefillTimeModeBuffer()
    {
        if (Mode != TestMode.Time)
            return;

        if (_words.Count - _currentWordIndex > TimeModeRefillThreshold)
            return;

        RefillWords(TimeModeInitialWordBufferSize);
    }

    private void RefillWords(int count)
    {
        foreach (var word in _wordListProvider!.TakeRandomWords(count))
            _words.Add(new Word(word));
    }

    /// <summary>
    /// Tallies every word the user actually engaged with: committed words (including ones
    /// skipped with nothing typed — their whole length counts as missed) and, if it has any
    /// input at all, the current in-progress word. Words never reached at all — pre-buffered
    /// ahead in <see cref="TestMode.Time"/>, or simply the untouched word the user now happens
    /// to be sitting on — are excluded entirely rather than counted as missed.
    /// </summary>
    private (int Correct, int Incorrect, int Extra, int Missed) CountCharacters()
    {
        var correct = 0;
        var incorrect = 0;
        var extra = 0;
        var missed = 0;

        foreach (var word in _words)
        {
            if (!word.IsCommitted && word.Typed.Count == 0)
                continue;

            foreach (var typedCharacter in word.Typed)
            {
                switch (typedCharacter.State)
                {
                    case CharacterState.Correct: correct++; break;
                    case CharacterState.Incorrect: incorrect++; break;
                    case CharacterState.Extra: extra++; break;
                }
            }

            missed += Math.Max(0, word.Target.Length - word.Typed.Count);
        }

        return (correct, incorrect, extra, missed);
    }
}
