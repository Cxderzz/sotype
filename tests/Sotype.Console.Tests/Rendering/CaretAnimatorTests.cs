using Sotype.Cli.Rendering;

namespace Sotype.Cli.Tests.Rendering;

public class CaretAnimatorTests
{
    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(16);

    private static CaretAnimator AnimatorAt(int line, int column)
    {
        var animator = new CaretAnimator();
        animator.Advance(line, column, TimeSpan.Zero);
        return animator;
    }

    [Test]
    public void Advance_ShouldStartOnTheTarget_OnTheFirstFrame()
    {
        var animator = AnimatorAt(line: 2, column: 7);

        animator.Line.ShouldBe(2);
        animator.Column.ShouldBe(7);
        animator.IsMoving.ShouldBeFalse();
    }

    [Test]
    public void Advance_ShouldMovePartOfTheWayTowardsTheTarget_WhenItIsOnTheSameLine()
    {
        var animator = AnimatorAt(line: 0, column: 0);

        animator.Advance(0, 1, Frame);

        animator.Column.ShouldBeGreaterThan(0);
        animator.Column.ShouldBeLessThan(1);
        animator.IsMoving.ShouldBeTrue();
    }

    [Test]
    public void Advance_ShouldReachTheTargetAndStop_AfterEnoughFrames()
    {
        var animator = AnimatorAt(line: 0, column: 0);

        for (var frame = 0; frame < 10; frame++)
            animator.Advance(0, 1, Frame);

        animator.Column.ShouldBe(1);
        animator.IsMoving.ShouldBeFalse();
    }

    [Test]
    public void Advance_ShouldCoverTheSameGroundInTheSameTime_RegardlessOfFrameRate()
    {
        var smooth = AnimatorAt(line: 0, column: 0);
        var choppy = AnimatorAt(line: 0, column: 0);

        for (var frame = 0; frame < 4; frame++)
            smooth.Advance(0, 8, Frame);

        choppy.Advance(0, 8, Frame * 4);

        smooth.Column.ShouldBe(choppy.Column);
    }

    [Test]
    public void Advance_ShouldJumpRatherThanGlide_WhenTheCaretWrapsToAnotherLine()
    {
        var animator = AnimatorAt(line: 0, column: 40);

        animator.Advance(1, 0, Frame);

        animator.Line.ShouldBe(1);
        animator.Column.ShouldBe(0);
        animator.IsMoving.ShouldBeFalse();
    }

    [Test]
    public void Advance_ShouldQuantiseTheColumn_ToAPositionTheRendererCanDraw()
    {
        var animator = AnimatorAt(line: 0, column: 0);

        animator.Advance(0, 1, Frame);

        var eighths = animator.Column * CaretAnimator.SubCellSteps;
        eighths.ShouldBe(Math.Round(eighths));
    }

    [Test]
    public void Advance_ShouldRetargetMidGlide_WhenTheNextCharacterIsTypedBeforeItLands()
    {
        var animator = AnimatorAt(line: 0, column: 0);
        animator.Advance(0, 1, Frame);

        var afterFirstKey = animator.Column;
        animator.Advance(0, 2, Frame);

        animator.Column.ShouldBeGreaterThan(afterFirstKey);
        animator.Column.ShouldBeLessThan(2);
    }
}
