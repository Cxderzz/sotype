namespace Sotype.Domain;

/// <summary>
/// The settings a <see cref="TypingSession"/> is configured with. Immutable value object;
/// use <see cref="ForDuration"/> or <see cref="ForWordCount"/> rather than a public constructor,
/// since exactly one of <see cref="Duration"/>/<see cref="WordCount"/> is meaningful per <see cref="Mode"/>.
/// </summary>
public sealed record TestConfiguration
{
    public TestMode Mode { get; }
    public TimeSpan? Duration { get; }
    public int? WordCount { get; }

    /// <summary>
    /// Whether backspacing past the start of the current (empty) word steps back into the
    /// previous word to correct it, matching monkeytype's default behavior. A future
    /// "confidence mode" would construct a configuration with this set to false.
    /// </summary>
    public bool AllowBackspaceIntoPreviousWord { get; }

    private TestConfiguration(TestMode mode, TimeSpan? duration, int? wordCount, bool allowBackspaceIntoPreviousWord)
    {
        Mode = mode;
        Duration = duration;
        WordCount = wordCount;
        AllowBackspaceIntoPreviousWord = allowBackspaceIntoPreviousWord;
    }

    public static TestConfiguration ForDuration(TimeSpan duration, bool allowBackspaceIntoPreviousWord = true)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration must be positive.");

        return new TestConfiguration(TestMode.Time, duration, null, allowBackspaceIntoPreviousWord);
    }

    public static TestConfiguration ForWordCount(int wordCount, bool allowBackspaceIntoPreviousWord = true)
    {
        if (wordCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(wordCount), wordCount, "Word count must be positive.");

        return new TestConfiguration(TestMode.Words, null, wordCount, allowBackspaceIntoPreviousWord);
    }
}
