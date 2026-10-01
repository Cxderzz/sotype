using Sotype.Cli.Rendering;
using Sotype.Cli.Rendering.Charts;
using Sotype.Domain;
using Sotype.Domain.History;
using Sotype.Domain.Themes;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sotype.Cli.Screens;

/// <summary>
/// Lifetime stats and trends across every finished test, then waits for a key to go back.
/// </summary>
public static class StatsScreen
{
    private const int RecentTests = 10;
    private const int TrendTests = 60;
    private const int MovingAverageTests = 10;
    private const int ActivityWeeks = 20;

    public static void Show(IAnsiConsole console, IReadOnlyList<RunRecord> runs, ThemeRecord themeRecord, DateTimeOffset now)
    {
        console.Clear();
        console.Write(new Rule($"[{themeRecord.Accent} bold]stats[/]").LeftJustified().RuleStyle(Card.BorderColour));

        if (runs.Count == 0)
        {
            console.MarkupLine("[grey58]no tests yet[/]");
        }
        else
        {
            var oldestFirst = runs.OrderBy(run => run.Timestamp).ToList();
            var today = DateOnly.FromDateTime(now.Date);

            console.Write(Headline(oldestFirst, themeRecord));
            console.Write(WpmTrend(oldestFirst, themeRecord));
            console.Write(SideBySide(WpmByTest(oldestFirst, themeRecord), Keystrokes(oldestFirst, themeRecord)));
            console.Write(SideBySide(Activity(oldestFirst, today), PracticeCalendar(oldestFirst, themeRecord, today)));
        }

        console.MarkupLine("[grey58]any key[/] back");
        console.Input.ReadKey(intercept: true);
    }

    /// <summary>
    /// A row of headline numbers, with recent trends under the averages.
    /// </summary>
    private static Columns Headline(List<RunRecord> runs, ThemeRecord themeRecord)
    {
        var recent = runs.TakeLast(RecentTests).ToList();
        var timeTyping = TimeSpan.FromMinutes(runs.Sum(MinutesTyping));

        return new Columns(
            new StatTile("tests", $"[bold]{runs.Count}[/]"),
            new StatTile("best wpm", $"[{themeRecord.Accent} bold]{runs.Max(run => run.Wpm):0.#}[/]"),
            new StatTile($"avg wpm (last {RecentTests})", $"[bold]{recent.Average(run => run.Wpm):0.#}[/]")
            {
                Detail = new Sparkline(runs.Select(run => run.Wpm).ToList(), Colour(themeRecord.Accent)) { Width = 20 },
            },
            new StatTile($"accuracy (last {RecentTests})", $"[bold]{recent.Average(run => run.Accuracy):0.#}%[/]")
            {
                Detail = new Sparkline(runs.Select(run => run.Accuracy).ToList(), Colour(themeRecord.Correct)) { Width = 20 },
            },
            new StatTile("time typing", $"[bold]{(int)timeTyping.TotalHours}h {timeTyping.Minutes}m[/]"))
        {
            // Keep the tiles packed together on the left, rather than spread across the screen.
            Expand = false,
        };
    }

    /// <summary>
    /// Wpm for each recent test, with a moving average to show the trend through the noise.
    /// </summary>
    private static Panel WpmTrend(List<RunRecord> runs, ThemeRecord themeRecord)
    {
        var wpm = runs.TakeLast(TrendTests).Select(run => run.Wpm).ToList();

        var chart = new LineChart { Height = 10 }
            .AddSeries(new ChartSeries("wpm", wpm, Colour(themeRecord.Correct)))
            .AddSeries(new ChartSeries($"{MovingAverageTests}-test average", MovingAverage(wpm, MovingAverageTests), Colour(themeRecord.Accent)));

        return Card.Create($"wpm, last {wpm.Count} tests", chart);
    }

    /// <summary>
    /// Average and best wpm for each kind of test, such as 30s or 25w.
    /// </summary>
    private static Panel WpmByTest(List<RunRecord> runs, ThemeRecord themeRecord)
    {
        var chart = new BarChart()
            .Width(40)
            .UseValueFormatter(value => value.ToString("0"));

        var byTest = runs
            .OrderBy(run => run.Mode)
            .ThenBy(run => run.Duration ?? TimeSpan.Zero)
            .ThenBy(run => run.WordCount ?? 0)
            .GroupBy(TestLabel);

        foreach (var test in byTest)
        {
            chart.AddItem($"{test.Key} avg", test.Average(run => run.Wpm), Colour(themeRecord.Pending));
            chart.AddItem($"{test.Key} best", test.Max(run => run.Wpm), Colour(themeRecord.Accent));
        }

        return Card.Create("wpm by test", chart);
    }

    /// <summary>
    /// Every keystroke ever typed, split the same way as the results screen, and accuracy by day.
    /// </summary>
    private static Panel Keystrokes(List<RunRecord> runs, ThemeRecord themeRecord)
    {
        var breakdown = new BreakdownChart()
            .Width(40)
            .AddItem("correct", runs.Sum(run => run.CorrectCharacters), Colour(themeRecord.Correct))
            .AddItem("incorrect", runs.Sum(run => run.IncorrectCharacters), Colour(themeRecord.Incorrect))
            .AddItem("extra", runs.Sum(run => run.ExtraCharacters), Colour(themeRecord.Extra))
            .AddItem("missed", runs.Sum(run => run.MissedCharacters), Color.Grey50);

        var accuracyByDay = runs
            .GroupBy(DayOf)
            .Select(day => day.Average(run => run.Accuracy))
            .ToList();

        return Card.Create("keystrokes", new Rows(
            breakdown,
            Text.Empty,
            new Markup("[grey58]daily accuracy[/]"),
            new Sparkline(accuracyByDay, Colour(themeRecord.Correct)) { Width = 40 }));
    }

    private static Panel Activity(List<RunRecord> runs, DateOnly today)
    {
        var testsByDay = runs
            .GroupBy(DayOf)
            .ToDictionary(day => day.Key, day => day.Count());

        return Card.Create(
            $"activity, last {ActivityWeeks} weeks",
            new ActivityHeatmap(testsByDay, today) { Weeks = ActivityWeeks });
    }

    /// <summary>
    /// This month, with the days that had at least one test highlighted.
    /// </summary>
    private static Calendar PracticeCalendar(List<RunRecord> runs, ThemeRecord themeRecord, DateOnly today)
    {
        var calendar = new Calendar(today.Year, today.Month)
            .Border(TableBorder.Rounded)
            .BorderColor(Card.BorderColour)
            .HeaderStyle(new Style(Color.Grey58))
            .HighlightStyle(Style.Parse($"{themeRecord.Accent} bold"));

        var practiceDays = runs
            .Select(DayOf)
            .Distinct()
            .Where(day => day.Year == today.Year && day.Month == today.Month);

        foreach (var day in practiceDays)
            calendar.AddCalendarEvent(day.Year, day.Month, day.Day);

        return calendar;
    }

    private static Grid SideBySide(IRenderable left, IRenderable right) => new Grid()
        .AddColumn()
        .AddColumn()
        .AddRow(left, right);

    /// <summary>
    /// Each value averaged with up to <paramref name="window"/> - 1 values before it.
    /// </summary>
    private static List<double> MovingAverage(List<double> values, int window) => values
        .Select((_, index) => values.Take(index + 1).TakeLast(window).Average())
        .ToList();

    /// <summary>
    /// A word is 5 characters, so typed characters / 5 / raw wpm gives the minutes spent typing.
    /// </summary>
    private static double MinutesTyping(RunRecord run) =>
        (run.CorrectCharacters + run.IncorrectCharacters) / 5.0 / run.RawWpm;

    private static string TestLabel(RunRecord run) => run.Mode == TestMode.Time
        ? $"{run.Duration!.Value.TotalSeconds}s"
        : $"{run.WordCount}w";

    private static DateOnly DayOf(RunRecord run) => DateOnly.FromDateTime(run.Timestamp.Date);

    /// <summary>
    /// Theme colours are stored as markup colour names; the charts take <see cref="Color"/>s.
    /// </summary>
    private static Color Colour(string markupColour) => Style.Parse(markupColour).Foreground;
}
