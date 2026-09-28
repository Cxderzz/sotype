namespace Sotype.Cli.Rendering;

/// <summary>
/// A smooth caret animator, for a grid of character cells: the caret glides to its target
/// column over a handful of frames instead of teleporting to it on every keystroke.
/// </summary>
/// <remarks>
/// <para>
/// A terminal cell is the smallest addressable unit of text, but not the smallest thing that can
/// be <em>painted</em>: the block-element glyphs (U+2588..U+258F) subdivide a cell into eighths,
/// so the caret can be drawn at 8x the resolution the text itself is laid out at. That's what
/// makes sub-cell motion possible at all, and it's why positions are quantised to eighths here.
/// <see cref="Column"/> only ever reports a position the renderer can actually draw, which in
/// turn lets <see cref="Advance"/> report precisely when a redraw would change anything.
/// </para>
/// </remarks>
public sealed class CaretAnimator
{
    public const int SubCellSteps = 8;
    private static readonly TimeSpan MoveDuration = TimeSpan.FromMilliseconds(100);

    private double _origin;
    private double _column;
    private int _targetColumn;
    private TimeSpan _elapsed;
    private int _drawnEighths = int.MinValue;
    public int CurrentLine { get; private set; } = -1;

    public double Column => _drawnEighths / (double)SubCellSteps;
    public bool IsAnimating { get; private set; }

    /// <summary>
    /// Advances the animation by <paramref name="delta"/> of elapsed time, starting a fresh move
    /// if <paramref name="target"/> has changed since the last frame.
    /// </summary>
    /// <returns>True if the caret should now be drawn differently than it was last frame.</returns>
    public bool Advance(CaretTarget target, TimeSpan delta)
    {
        var changedLine = target.Line != CurrentLine;

        if (changedLine)
        {
            CurrentLine = target.Line;
            StartMove(target.Column, from: target.Column);
        }
        else if (target.Column != _targetColumn)
        {
            StartMove(target.Column, from: _column);
        }

        _elapsed += delta;

        var progress = _elapsed >= MoveDuration ? 1 : _elapsed / MoveDuration;
        _column = _origin + ((_targetColumn - _origin) * EaseOut(progress));
        IsAnimating = progress < 1;

        var eighths = (int)Math.Round(_column * SubCellSteps);
        var changed = changedLine || eighths != _drawnEighths;
        _drawnEighths = eighths;

        return changed;
    }

    private void StartMove(int target, double from)
    {
        _origin = from;
        _column = from;
        _targetColumn = target;
        _elapsed = Math.Abs(from - target) < 0.001 ? MoveDuration : TimeSpan.Zero;
    }

    private static double EaseOut(double progress) => 1 - ((1 - progress) * (1 - progress));
}
