using Sotype.Cli.Rendering;

namespace Sotype.Cli.Tests.Rendering;

public class CaretAnimatorTests
{
    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(16);

    private static CaretAnimator AnimatorAt(int line, int column)
    {
        var animator = new CaretAnimator();
        animator.Advance(new CaretTarget(line, column), TimeSpan.Zero);
        return animator;
    }

    [Test]
    public void Advance_ShouldSnapToTheTarget_OnTheVeryFirstFrame()
    {
        var animator = AnimatorAt(line: 2, column: 7);

        animator.Column.ShouldBe(7);
        animator.CurrentLine.ShouldBe(2);
        animator.IsAnimating.ShouldBeFalse();
    }

    [Test]
    public void Advance_ShouldMovePartOfTheWayTowardsTheTarget_WhenTheTargetIsOnTheSameLine()
    {
        var animator = AnimatorAt(line: 0, column: 0);

        animator.Advance(new CaretTarget(0, 1), Frame);

        animator.Column.ShouldBeGreaterThan(0);
        animator.Column.ShouldBeLessThan(1);
        animator.IsAnimating.ShouldBeTrue();
    }

    [Test]
    public void Advance_ShouldReachTheTargetAndStop_AfterEnoughFrames()
    {
        var animator = AnimatorAt(line: 0, column: 0);

        for (var frame = 0; frame < 30; frame++)
            animator.Advance(new CaretTarget(0, 1), Frame);

        animator.Column.ShouldBe(1);
        animator.IsAnimating.ShouldBeFalse();
    }

    [Test]
    public void Advance_ShouldAnimateAtTheSameSpeedInRealTime_RegardlessOfFrameRate()
    {
        var smooth = AnimatorAt(line: 0, column: 0);
        var choppy = AnimatorAt(line: 0, column: 0);

        for (var frame = 0; frame < 4; frame++)
            smooth.Advance(new CaretTarget(0, 8), Frame);

        choppy.Advance(new CaretTarget(0, 8), Frame * 4);

        smooth.Column.ShouldBe(choppy.Column, tolerance: 1.0 / CaretAnimator.SubCellSteps);
    }

    [Test]
    public void Advance_ShouldSnapRatherThanGlide_WhenTheCaretWrapsToAnotherLine()
    {
        var animator = AnimatorAt(line: 0, column: 40);

        animator.Advance(new CaretTarget(1, 0), Frame);

        animator.CurrentLine.ShouldBe(1);
        animator.Column.ShouldBe(0);
        animator.IsAnimating.ShouldBeFalse();
    }

    [Test]
    public void Advance_ShouldQuantiseTheReportedColumn_ToAPositionTheRendererCanDraw()
    {
        var animator = AnimatorAt(line: 0, column: 0);

        animator.Advance(new CaretTarget(0, 1), Frame);

        var eighths = animator.Column * CaretAnimator.SubCellSteps;
        eighths.ShouldBe(Math.Round(eighths));
    }

    [Test]
    public void Advance_ShouldReportNoChange_WhenTheDrawnPositionWouldBeIdentical()
    {
        var animator = AnimatorAt(line: 0, column: 5);

        animator.Advance(new CaretTarget(0, 5), Frame).ShouldBeFalse();
    }

    [Test]
    public void Advance_ShouldReportAChange_WhenTheCaretMovesToANewSubCellPosition()
    {
        var animator = AnimatorAt(line: 0, column: 0);

        animator.Advance(new CaretTarget(0, 4), Frame).ShouldBeTrue();
    }
}
