using Sotype.Domain.Statistics;

namespace Sotype.Domain.Tests.Statistics;

public class TypingStatisticsCalculatorTests
{
    [Test]
    public void CalculateWpm_ShouldReturnStandardWordsPerMinute_WhenGivenCorrectCharactersAndElapsedTime()
    {
        // 50 correct characters == 10 "words" (5 chars/word) typed in exactly 1 minute.
        var wpm = TypingStatisticsCalculator.CalculateWpm(50, TimeSpan.FromMinutes(1));

        wpm.ShouldBe(10);
    }

    [Test]
    public void CalculateWpm_ShouldReturnZero_WhenElapsedTimeIsZero()
    {
        var wpm = TypingStatisticsCalculator.CalculateWpm(50, TimeSpan.Zero);

        wpm.ShouldBe(0);
    }

    [Test]
    public void CalculateRawWpm_ShouldIncludeIncorrectAndExtraCharacters_WhenCalculatingWordsPerMinute()
    {
        // 30 correct + 10 incorrect + 10 extra == 50 chars == 10 "words" typed in 1 minute.
        var rawWpm = TypingStatisticsCalculator.CalculateRawWpm(30, 10, 10, TimeSpan.FromMinutes(1));

        rawWpm.ShouldBe(10);
    }

    [Test]
    public void CalculateRawWpm_ShouldReturnZero_WhenElapsedTimeIsZero()
    {
        var rawWpm = TypingStatisticsCalculator.CalculateRawWpm(30, 10, 10, TimeSpan.Zero);

        rawWpm.ShouldBe(0);
    }

    [TestCase(80, 20, 0, 80)]
    [TestCase(100, 0, 0, 100)]
    [TestCase(0, 10, 0, 0)]
    public void CalculateAccuracy_ShouldReturnPercentageOfCorrectCharacters_WhenCharactersWereTyped(
        int correct, int incorrect, int extra, double expectedAccuracy)
    {
        var accuracy = TypingStatisticsCalculator.CalculateAccuracy(correct, incorrect, extra);

        accuracy.ShouldBe(expectedAccuracy);
    }

    [Test]
    public void CalculateAccuracy_ShouldReturnZero_WhenNoCharactersWereTyped()
    {
        var accuracy = TypingStatisticsCalculator.CalculateAccuracy(0, 0, 0);

        accuracy.ShouldBe(0);
    }
}
