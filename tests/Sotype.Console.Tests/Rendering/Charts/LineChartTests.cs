using Sotype.Cli.Rendering.Charts;
using Spectre.Console;

namespace Sotype.Cli.Tests.Rendering.Charts;

public class LineChartTests
{
    [Test]
    public void Render_ShouldLabelTheTopMiddleAndBottomOfTheYAxis()
    {
        var chart = new LineChart { Height = 5 }
            .AddSeries(new ChartSeries("wpm", [50, 100], Color.White));

        var lines = RenderedText.Of(chart).Split('\n');

        lines[0].ShouldStartWith("100 ┤");
        lines[1].ShouldStartWith("    │");
        lines[2].ShouldStartWith(" 75 ┤");
        lines[4].ShouldStartWith(" 50 ┤");
        lines[5].ShouldStartWith("    └");
    }

    [Test]
    public void Render_ShouldUseTheGivenRange_WhenMinAndMaxAreSet()
    {
        var chart = new LineChart { Height = 3, Min = 0, Max = 200 }
            .AddSeries(new ChartSeries("wpm", [50, 100], Color.White));

        var lines = RenderedText.Of(chart).Split('\n');

        lines[0].ShouldStartWith("200 ┤");
        lines[2].ShouldStartWith("  0 ┤");
    }

    [Test]
    public void Render_ShouldNameEverySeriesInTheLegend()
    {
        var chart = new LineChart { Height = 3 }
            .AddSeries(new ChartSeries("wpm", [1, 2], Color.White))
            .AddSeries(new ChartSeries("average", [1, 2], Color.White));

        RenderedText.Of(chart).ShouldContain("⣿ wpm   ⣿ average");
    }

    [Test]
    public void Render_ShouldDrawNothing_WhenThereAreNoValues()
    {
        var chart = new LineChart().AddSeries(new ChartSeries("wpm", [], Color.White));

        RenderedText.Of(chart).ShouldBeEmpty();
    }
}
