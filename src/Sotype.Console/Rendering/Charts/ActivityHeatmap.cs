using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sotype.Cli.Rendering.Charts;

/// <summary>
/// An activity grid: one column per week and one row per weekday, with each day shaded
/// by how busy it was compared to the busiest day.
/// </summary>
/// <param name="countsByDay">How many things happened on each day. Missing days count as zero.</param>
/// <param name="lastDay">The newest day shown, normally today. Its week is the right-hand column.</param>
public sealed class ActivityHeatmap(IReadOnlyDictionary<DateOnly, int> countsByDay, DateOnly lastDay) : Renderable
{
    private const string DayGlyph = "■";
    private static readonly string[] WeekdayLabels = ["mon", "", "wed", "", "fri", "", "sun"];

    public int Weeks { get; init; } = 20;

    /// <summary>
    /// Shades from an empty day (first) to the busiest day (last).
    /// </summary>
    public IReadOnlyList<Color> Shades { get; init; } =
        [Color.Grey23, Color.DarkGreen, Color.Green4, Color.Green3, Color.Green1];

    public bool ShowLegend { get; init; } = true;

    protected override Measurement Measure(RenderOptions options, int maxWidth) =>
        ((IRenderable)BuildMarkup()).Measure(options, maxWidth);

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
        ((IRenderable)BuildMarkup()).Render(options, maxWidth);

    private Markup BuildMarkup()
    {
        var firstMonday = StartOfWeek(lastDay).AddDays(-7 * (Weeks - 1));
        var busiestDay = countsByDay.Values.DefaultIfEmpty(0).Max();
        var markup = new StringBuilder();

        for (var weekday = 0; weekday < 7; weekday++)
        {
            markup.Append($"[grey58]{WeekdayLabels[weekday],-3}[/]");

            for (var week = 0; week < Weeks; week++)
            {
                var day = firstMonday.AddDays(week * 7 + weekday);
                markup.Append(day > lastDay ? "  " : " " + Shaded(ShadeFor(countsByDay.GetValueOrDefault(day), busiestDay)));
            }

            markup.AppendLine();
        }

        if (ShowLegend)
            markup.AppendLine().Append("[grey58]less[/] ").AppendJoin(' ', Shades.Select(Shaded)).Append(" [grey58]more[/]");

        return new Markup(markup.ToString());
    }

    /// <summary>
    /// An empty day gets the first shade; any other day gets one of the rest, scaled to the busiest day.
    /// </summary>
    private Color ShadeFor(int count, int busiestDay)
    {
        if (count <= 0)
            return Shades[0];

        var activeShades = Shades.Count - 1;
        var level = (int)Math.Ceiling((double)count / busiestDay * activeShades);

        return Shades[Math.Clamp(level, 1, activeShades)];
    }

    private static string Shaded(Color colour) => $"[{colour.ToMarkup()}]{DayGlyph}[/]";

    private static DateOnly StartOfWeek(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
}
