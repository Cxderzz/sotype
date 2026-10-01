using Sotype.Domain;
using Sotype.Domain.History;

namespace Sotype.Cli.Screens;

/// <summary>
/// Made-up test history for trying out the stats screen: a typist who improves from about 55 to
/// 85 wpm over a few months, practising on most days.
/// </summary>
/// <remarks>The same seed always gives the same history.</remarks>
public static class MockRunHistory
{
    private sealed record MockTest(TestMode Mode, TimeSpan? Duration, int? WordCount, double Popularity)
    {
        public bool IsShort => Duration?.TotalSeconds <= 15 || WordCount <= 10;
    }

    // Popularities add up to 1.
    private static readonly MockTest[] Tests =
    [
        new(TestMode.Time, TimeSpan.FromSeconds(15), null, 0.20),
        new(TestMode.Time, TimeSpan.FromSeconds(30), null, 0.35),
        new(TestMode.Time, TimeSpan.FromSeconds(60), null, 0.15),
        new(TestMode.Words, null, 10, 0.10),
        new(TestMode.Words, null, 25, 0.15),
        new(TestMode.Words, null, 50, 0.05),
    ];

    public static IReadOnlyList<RunRecord> Generate(DateTimeOffset now, int days = 120, int seed = 42)
    {
        var random = new Random(seed);
        var runs = new List<RunRecord>();

        for (var daysAgo = days; daysAgo >= 0; daysAgo--)
        {
            // 0 on the first day, 1 today.
            var progress = 1 - (double)daysAgo / days;

            // Days off get rarer as the habit sticks: from about 45% of days to about 25%.
            var chanceOfDayOff = 0.45 - 0.2 * progress;
            if (random.NextDouble() < chanceOfDayOff)
                continue;

            var day = new DateTimeOffset(now.Date.AddDays(-daysAgo), now.Offset);
            var firstTestAt = day.AddHours(random.Next(18, 23));
            var testsToday = random.Next(1, 9);

            for (var i = 0; i < testsToday; i++)
                runs.Add(MakeRun(random, PickTest(random), firstTestAt.AddMinutes(i * 3), progress));
        }

        return runs;
    }

    private static RunRecord MakeRun(Random random, MockTest test, DateTimeOffset timestamp, double progress)
    {
        // Short tests read a little faster, and results get more consistent with practice.
        var shortTestBonus = test.IsShort ? 6 : 0;
        var spread = 16 - 6 * progress;
        var wpm = Math.Max(20, 55 + 30 * progress + shortTestBonus + Noise(random, spread));

        var accuracy = Math.Clamp(91 + 6 * progress + Noise(random, 5), 80, 100);
        var rawWpm = wpm / (accuracy / 100) + random.NextDouble() * 2;

        var seconds = test.Duration?.TotalSeconds ?? test.WordCount!.Value * 60.0 / wpm;
        var typedCharacters = (int)(rawWpm * 5 * seconds / 60);
        var incorrectCharacters = (int)(typedCharacters * (1 - accuracy / 100));

        return new RunRecord(
            Timestamp: timestamp,
            Mode: test.Mode,
            Duration: test.Duration,
            WordCount: test.WordCount,
            Wpm: wpm,
            RawWpm: rawWpm,
            Accuracy: accuracy,
            CorrectCharacters: typedCharacters - incorrectCharacters,
            IncorrectCharacters: incorrectCharacters,
            ExtraCharacters: random.Next(0, 4),
            MissedCharacters: random.Next(0, 3),
            ThemeName: "default");
    }

    /// <summary>
    /// A random test, more popular tests being picked more often.
    /// </summary>
    private static MockTest PickTest(Random random)
    {
        var roll = random.NextDouble();

        foreach (var test in Tests)
        {
            roll -= test.Popularity;
            if (roll <= 0)
                return test;
        }

        return Tests[^1];
    }

    /// <summary>
    /// A random amount between -spread/2 and +spread/2.
    /// </summary>
    private static double Noise(Random random, double spread) => (random.NextDouble() - 0.5) * spread;
}
