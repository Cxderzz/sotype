using Sotype.Cli.Rendering.Charts;

namespace Sotype.Cli.Tests.Rendering.Charts;

public class ActivityHeatmapTests
{
    // A Wednesday.
    private static readonly DateOnly Today = new(2026, 9, 30);

    [Test]
    public void Render_ShouldShowOneSquarePerWeekPerWeekday_UpToToday()
    {
        var heatmap = new ActivityHeatmap(new Dictionary<DateOnly, int>(), Today) { Weeks = 3, ShowLegend = false };

        var lines = RenderedText.Of(heatmap).TrimEnd().Split('\n');

        lines.Length.ShouldBe(7);
        lines[0].ShouldBe("mon ■ ■ ■");
        lines[2].ShouldBe("wed ■ ■ ■");
        // Thursday onwards hasn't happened yet this week.
        lines[3].TrimEnd().ShouldBe("    ■ ■");
    }

    [Test]
    public void Render_ShouldShowTheLegend_ByDefault()
    {
        var heatmap = new ActivityHeatmap(new Dictionary<DateOnly, int>(), Today) { Weeks = 1 };

        RenderedText.Of(heatmap).ShouldContain("less ■ ■ ■ ■ ■ more");
    }
}
