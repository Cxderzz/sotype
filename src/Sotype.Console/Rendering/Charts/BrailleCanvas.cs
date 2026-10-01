using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sotype.Cli.Rendering.Charts;

/// <summary>
/// A block of terminal cells that can be drawn on dot by dot, using braille characters.
/// </summary>
/// <remarks>
/// Each braille character is a grid of 2x4 dots, so the canvas has twice as many dots across as it
/// has columns, and four times as many down as it has rows. Dot (0, 0) is the top left.
/// <para>
/// A terminal cell can only have one colour, so each cell holds dots of one colour only. Drawing in
/// a different colour clears the dots already in that cell first, so the newest drawing sits on top
/// rather than older dots being repainted in its colour.
/// </para>
/// </remarks>
public sealed class BrailleCanvas
{
    private const int DotsPerColumn = 2;
    private const int DotsPerRow = 4;
    private const char BlankBraille = '⠀';

    // The bit each dot adds to the blank braille character, indexed [x, y] within its cell.
    // Unicode numbers the dots down the left column, then down the right, then along the bottom row.
    private static readonly int[,] DotBits =
    {
        { 0x01, 0x02, 0x04, 0x40 },
        { 0x08, 0x10, 0x20, 0x80 },
    };

    private readonly int[,] _cellBits;
    private readonly Color?[,] _cellColours;

    public BrailleCanvas(int columns, int rows)
    {
        Columns = columns;
        Rows = rows;
        _cellBits = new int[columns, rows];
        _cellColours = new Color?[columns, rows];
    }

    public int Columns { get; }

    public int Rows { get; }

    public int DotWidth => Columns * DotsPerColumn;

    public int DotHeight => Rows * DotsPerRow;

    /// <summary>
    /// Turns on one dot, clearing its cell first if the cell holds dots of another colour.
    /// Dots outside the canvas are ignored.
    /// </summary>
    public void SetDot(int x, int y, Color colour)
    {
        if (x < 0 || y < 0 || x >= DotWidth || y >= DotHeight)
            return;

        var (column, row) = (x / DotsPerColumn, y / DotsPerRow);

        if (_cellColours[column, row] != colour)
            _cellBits[column, row] = 0;

        _cellBits[column, row] |= DotBits[x % DotsPerColumn, y % DotsPerRow];
        _cellColours[column, row] = colour;
    }

    /// <summary>
    /// Draws a straight line between two dots, using Bresenham's line algorithm.
    /// </summary>
    public void DrawLine((int X, int Y) from, (int X, int Y) to, Color colour)
    {
        var (x, y) = from;
        var distanceX = Math.Abs(to.X - x);
        var distanceY = -Math.Abs(to.Y - y);
        var stepX = x < to.X ? 1 : -1;
        var stepY = y < to.Y ? 1 : -1;
        var error = distanceX + distanceY;

        while (true)
        {
            SetDot(x, y, colour);

            if (x == to.X && y == to.Y)
                return;

            // Step across, down, or both: whichever keeps the line closest to the true line. Both
            // checks must use the error from before either step, or the line can miss its end.
            var doubledError = 2 * error;

            if (doubledError >= distanceY)
            {
                error += distanceY;
                x += stepX;
            }

            if (doubledError <= distanceX)
            {
                error += distanceX;
                y += stepY;
            }
        }
    }

    /// <summary>
    /// The braille character for a cell; the blank braille character if no dots are set.
    /// </summary>
    public char GlyphAt(int column, int row) => (char)(BlankBraille + _cellBits[column, row]);

    /// <summary>
    /// One row of cells, ready to write to the console. Empty cells are plain spaces, since some
    /// fonts draw the blank braille character as faint dots.
    /// </summary>
    public IEnumerable<Segment> RenderRow(int row)
    {
        for (var column = 0; column < Columns; column++)
        {
            yield return _cellColours[column, row] is { } colour
                ? new Segment(GlyphAt(column, row).ToString(), new Style(colour))
                : new Segment(" ");
        }
    }
}
