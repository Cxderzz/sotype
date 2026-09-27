namespace Sotype.Domain.Statistics;

/// <summary>
/// Domain service computing the standard monkeytype-style typing statistics. Pure functions —
/// there's no state to own and nothing worth substituting in a test, so this stays static
/// rather than an injected interface.
/// </summary>
public static class TypingStatisticsCalculator
{
    private const int CharactersPerWord = 5;

    /// <summary>
    /// Net words per minute: correct characters only, standardized at 5 characters per word.
    /// Zero when <paramref name="elapsed"/> is zero (e.g. a session finished before any
    /// keystroke started its clock) rather than dividing by zero.
    /// </summary>
    public static double CalculateWpm(int correctCharacters, TimeSpan elapsed)
    {
        var minutes = elapsed.TotalMinutes;
        return minutes > 0 ? correctCharacters / (double)CharactersPerWord / minutes : 0;
    }

    /// <summary>Like <see cref="CalculateWpm"/>, but counting incorrect and extra characters too.</summary>
    public static double CalculateRawWpm(int correctCharacters, int incorrectCharacters, int extraCharacters, TimeSpan elapsed)
    {
        var minutes = elapsed.TotalMinutes;
        if (minutes <= 0)
            return 0;

        var totalCharacters = correctCharacters + incorrectCharacters + extraCharacters;
        return totalCharacters / (double)CharactersPerWord / minutes;
    }

    /// <summary>
    /// Percentage of typed characters that were correct. Zero when no characters were typed
    /// at all, rather than dividing by zero.
    /// </summary>
    public static double CalculateAccuracy(int correctCharacters, int incorrectCharacters, int extraCharacters)
    {
        var totalCharacters = correctCharacters + incorrectCharacters + extraCharacters;
        return totalCharacters > 0 ? correctCharacters / (double)totalCharacters * 100 : 0;
    }
}
