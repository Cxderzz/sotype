using System.Text;
using Sotype.Domain;
using Sotype.Domain.Themes;
using Spectre.Console;

namespace Sotype.Cli.Rendering;

/// <summary>
/// Renders one line of words as Spectre markup, with the caret painted over it.
/// </summary>
public static class LineMarkup
{
    /// <param name="caretColumn">
    /// Cells from the start of the line, or null to leave the caret off.
    /// </param>
    public static string Render(IReadOnlyList<Word> words, Theme theme, double? caretColumn = null)
    {
        var cells = BuildCells(words, theme);

        if (caretColumn is { } column)
            PaintCaret(cells, column, theme);

        return Serialise(cells);
    }

    private readonly record struct Cell(char Glyph, string Style);

    private static List<Cell> BuildCells(IReadOnlyList<Word> words, Theme theme)
    {
        var cells = new List<Cell>();

        for (var index = 0; index < words.Count; index++)
        {
            var word = words[index];

            for (var i = 0; i < word.Target.Length; i++)
                cells.Add(new Cell(word.Target[i], i < word.Typed.Count ? StyleFor(word.Typed[i].State, theme) : theme.Pending));

            // Characters typed past the word's end have no target character to be shown in place of.
            for (var i = word.Target.Length; i < word.Typed.Count; i++)
                cells.Add(new Cell(word.Typed[i].Character, theme.Extra));

            if (index < words.Count - 1)
                cells.Add(new Cell(' ', theme.Pending));
        }

        return cells;
    }

    /// <summary>
    /// Paints a one cell wide caret starting at <paramref name="column"/>, which may fall between
    /// two cells. Block glyphs only fill a cell from the left, so the caret's leading edge is the
    /// matching glyph in the caret colour and its trailing edge is that glyph inverted, putting
    /// the caret colour behind it. Inverting also resolves the terminal's background colour,
    /// which cannot be queried.
    /// </summary>
    private static void PaintCaret(List<Cell> cells, double column, Theme theme)
    {
        var cell = (int)Math.Floor(column);
        var eighths = (int)Math.Round((column - cell) * CaretAnimator.SubCellSteps);

        if (eighths == CaretAnimator.SubCellSteps)
        {
            cell++;
            eighths = 0;
        }

        if (cell < 0)
            return;

        // The caret can sit one cell past the line's last character, and its leading edge one past that.
        while (cells.Count <= cell + 1)
            cells.Add(new Cell(' ', theme.Pending));

        if (eighths == 0)
        {
            cells[cell] = cells[cell] with { Style = $"{theme.CursorForeground} on {theme.CursorBackground}" };
            return;
        }

        cells[cell] = new Cell(LeftBlock(eighths), $"invert {theme.CursorBackground}");
        cells[cell + 1] = new Cell(LeftBlock(eighths), theme.CursorBackground);
    }

    /// <summary>
    /// U+2588..U+258F: a block filling the left <paramref name="eighths"/>/8 of a cell.
    /// </summary>
    private static char LeftBlock(int eighths) => (char)(0x2590 - eighths);

    /// <summary>
    /// Merges neighbouring cells that share a style into one tag.
    /// </summary>
    private static string Serialise(List<Cell> cells)
    {
        var markup = new StringBuilder();
        var run = new StringBuilder();
        var index = 0;

        while (index < cells.Count)
        {
            var style = cells[index].Style;

            run.Clear();
            while (index < cells.Count && cells[index].Style == style)
                run.Append(cells[index++].Glyph);

            // Typed text can contain '[', which Spectre would otherwise read as markup.
            markup.Append('[').Append(style).Append(']')
                .Append(Markup.Escape(run.ToString()))
                .Append("[/]");
        }

        return markup.ToString();
    }

    private static string StyleFor(CharacterState state, Theme theme) => state switch
    {
        CharacterState.Correct => theme.Correct,
        CharacterState.Incorrect => theme.Incorrect,
        CharacterState.Extra => theme.Extra,
        _ => theme.Pending
    };
}
