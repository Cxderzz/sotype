namespace Sotype.Domain;

/// <summary>How a <see cref="TypingSession"/> decides when it is finished.</summary>
public enum TestMode
{
    /// <summary>The session runs for a fixed duration; word count is effectively unbounded.</summary>
    Time,

    /// <summary>The session runs for a fixed number of words; duration is unbounded.</summary>
    Words
}
