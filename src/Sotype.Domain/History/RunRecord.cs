namespace Sotype.Domain.History;

/// <summary>
/// A completed, persisted run. Mirrors <see cref="TestConfiguration"/> (rather than a free-text
/// label) so the results screen can group "personal best" by mode + duration/word-count exactly.
/// </summary>
public sealed record RunRecord(
    DateTimeOffset Timestamp,
    TestMode Mode,
    TimeSpan? Duration,
    int? WordCount,
    double Wpm,
    double RawWpm,
    double Accuracy,
    int CorrectCharacters,
    int IncorrectCharacters,
    int ExtraCharacters,
    int MissedCharacters,
    string ThemeName);
