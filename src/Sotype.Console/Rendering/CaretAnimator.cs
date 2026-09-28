namespace Sotype.Cli.Rendering;

/// <summary>
/// Animates the caret between character cells. Positions are quantised to eighths of a cell,
/// the finest the block-element glyphs can paint.
/// </summary>
public sealed class CaretAnimator
{
    public const int SubCellSteps = 8;

    private static readonly TimeSpan MoveDuration = TimeSpan.FromMilliseconds(100);

    private double _origin;
    private double _column;
    private int _targetColumn;
    private int _line = -1;
    private int _drawnEighths;
    private TimeSpan _elapsed;

    public int Line => _line;

    public double Column => _drawnEighths / (double)SubCellSteps;

    public bool IsMoving { get; private set; }

    /// <summary>
    /// Moves the caret toward the given cell, restarting the animation if it is a new one.
    /// A change of line jumps: rows have no sub-row positions to animate through.
    /// </summary>
    public void Advance(int line, int column, TimeSpan delta)
    {
        if (line != _line)
        {
            _line = line;
            StartMove(column, from: column);
        }
        else if (column != _targetColumn)
        {
            StartMove(column, from: _column);
        }

        _elapsed += delta;

        var progress = _elapsed >= MoveDuration ? 1 : _elapsed / MoveDuration;
        _column = _origin + ((_targetColumn - _origin) * EaseOut(progress));
        _drawnEighths = (int)Math.Round(_column * SubCellSteps);
        IsMoving = progress < 1;
    }

    private void StartMove(int target, double from)
    {
        _origin = from;
        _column = from;
        _targetColumn = target;

        // A move to the cell the caret is already on is over before it starts.
        _elapsed = Math.Abs(from - target) < 0.001 ? MoveDuration : TimeSpan.Zero;
    }

    private static double EaseOut(double progress) => 1 - ((1 - progress) * (1 - progress));
}
