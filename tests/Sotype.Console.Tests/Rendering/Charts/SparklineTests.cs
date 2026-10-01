using Sotype.Cli.Rendering.Charts;
using Spectre.Console;

namespace Sotype.Cli.Tests.Rendering.Charts;

public class SparklineTests
{
    [Test]
    public void Render_ShouldScaleFromTheLowestBlockToTheHighest()
    {
        var sparkline = new Sparkline([10, 20, 30], Color.White);

        RenderedText.Of(sparkline).Trim().ShouldBe("▁▅█");
    }

    [Test]
    public void Render_ShouldUseTheLowestBlock_WhenEveryValueIsTheSame()
    {
        var sparkline = new Sparkline([5, 5, 5], Color.White);

        RenderedText.Of(sparkline).Trim().ShouldBe("▁▁▁");
    }

    [Test]
    public void Render_ShouldShowOnlyTheMostRecentValues_WhenThereAreMoreValuesThanColumns()
    {
        var sparkline = new Sparkline([100, 1, 2], Color.White) { Width = 2 };

        RenderedText.Of(sparkline).Trim().ShouldBe("▁█");
    }
}
