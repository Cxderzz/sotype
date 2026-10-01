using Sotype.Cli.Rendering.Charts;
using Spectre.Console;

namespace Sotype.Cli.Tests.Rendering.Charts;

public class BrailleCanvasTests
{
    [Test]
    public void Size_ShouldHaveTwoDotsAcrossAndFourDown_PerCell()
    {
        var canvas = new BrailleCanvas(columns: 3, rows: 2);

        canvas.DotWidth.ShouldBe(6);
        canvas.DotHeight.ShouldBe(8);
    }

    [Test]
    public void GlyphAt_ShouldBeBlankBraille_WhenNoDotsAreSet()
    {
        var canvas = new BrailleCanvas(columns: 1, rows: 1);

        canvas.GlyphAt(0, 0).ShouldBe('⠀');
    }

    [TestCase(0, 0, '⠁')]
    [TestCase(1, 0, '⠈')]
    [TestCase(0, 3, '⡀')]
    [TestCase(1, 3, '⢀')]
    public void SetDot_ShouldTurnOnTheMatchingBrailleDot(int x, int y, char expected)
    {
        var canvas = new BrailleCanvas(columns: 1, rows: 1);

        canvas.SetDot(x, y, Color.White);

        canvas.GlyphAt(0, 0).ShouldBe(expected);
    }

    [Test]
    public void SetDot_ShouldPutTheDotInTheRightCell_WhenItIsPastTheFirstCell()
    {
        var canvas = new BrailleCanvas(columns: 2, rows: 2);

        canvas.SetDot(2, 4, Color.White);

        canvas.GlyphAt(1, 1).ShouldBe('⠁');
        canvas.GlyphAt(0, 0).ShouldBe('⠀');
    }

    [Test]
    public void SetDot_ShouldIgnoreTheDot_WhenItIsOutsideTheCanvas()
    {
        var canvas = new BrailleCanvas(columns: 1, rows: 1);

        Should.NotThrow(() =>
        {
            canvas.SetDot(-1, 0, Color.White);
            canvas.SetDot(2, 0, Color.White);
            canvas.SetDot(0, 4, Color.White);
        });
        canvas.GlyphAt(0, 0).ShouldBe('⠀');
    }

    [Test]
    public void DrawLine_ShouldFillTheLeftColumn_WhenTheLineIsVertical()
    {
        var canvas = new BrailleCanvas(columns: 1, rows: 1);

        canvas.DrawLine((0, 0), (0, 3), Color.White);

        canvas.GlyphAt(0, 0).ShouldBe('⡇');
    }

    [Test]
    public void DrawLine_ShouldFillTheDiagonal_WhenTheLineIsAt45Degrees()
    {
        var canvas = new BrailleCanvas(columns: 2, rows: 1);

        canvas.DrawLine((0, 0), (3, 3), Color.White);

        // (0,0) and (1,1) in the first cell; (2,2) and (3,3) in the second.
        canvas.GlyphAt(0, 0).ShouldBe('⠑');
        canvas.GlyphAt(1, 0).ShouldBe('⢄');
    }

    [TestCase(0, 0, 7, 2)]
    [TestCase(7, 2, 0, 0)]
    [TestCase(0, 7, 3, 0)]
    [CancelAfter(2000)]
    public void DrawLine_ShouldReachBothEnds_WhenTheLineIsNotStraightOrDiagonal(int fromX, int fromY, int toX, int toY)
    {
        var canvas = new BrailleCanvas(columns: 4, rows: 2);

        canvas.DrawLine((fromX, fromY), (toX, toY), Color.White);

        // Each cell is 2 dots across and 4 down.
        canvas.GlyphAt(fromX / 2, fromY / 4).ShouldNotBe('⠀');
        canvas.GlyphAt(toX / 2, toY / 4).ShouldNotBe('⠀');
    }

    [Test]
    public void SetDot_ShouldKeepEarlierDots_WhenTheyAreTheSameColour()
    {
        var canvas = new BrailleCanvas(columns: 1, rows: 1);

        canvas.SetDot(0, 0, Color.Green);
        canvas.SetDot(1, 0, Color.Green);

        canvas.GlyphAt(0, 0).ShouldBe('⠉');
    }

    [Test]
    public void SetDot_ShouldClearTheCellFirst_WhenItHoldsDotsOfAnotherColour()
    {
        var canvas = new BrailleCanvas(columns: 2, rows: 1);
        canvas.SetDot(0, 0, Color.Green);
        canvas.SetDot(2, 0, Color.Green);

        canvas.SetDot(1, 0, Color.Purple);

        // The green dot sharing a cell with the purple one is gone; the one in the next cell stays.
        canvas.GlyphAt(0, 0).ShouldBe('⠈');
        canvas.GlyphAt(1, 0).ShouldBe('⠁');
    }
}
