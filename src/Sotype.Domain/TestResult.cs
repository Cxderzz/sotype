namespace Sotype.Domain;

/// <summary>
/// The immutable outcome of a finished <see cref="TypingSession"/>. This is the only view
/// of a session's typing detail exposed once it has ended.
/// </summary>
public sealed record TestResult(
    TestMode Mode,
    TimeSpan Elapsed,
    int CorrectCharacters,
    int IncorrectCharacters,
    int ExtraCharacters,
    int MissedCharacters,
    double Wpm,
    double RawWpm,
    double Accuracy);
