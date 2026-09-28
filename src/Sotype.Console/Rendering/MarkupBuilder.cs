using System.Text;
using Sotype.Cli.Theming;
using Sotype.Domain;
using Spectre.Console;

namespace Sotype.Cli.Rendering;

/// <summary>
/// Turns typed-character state into Spectre <see cref="Markup"/>. A line is built as a flat run
/// of cells first and only serialised at the end, which is what lets the caret be painted
/// <em>over</em> the finished text at a position of its own — including one that straddles two
/// cells. Every literal character goes through <see cref="Markup.Escape(string)"/>, since a typed
/// character (or word-list content) could itself contain '[' or ']', which Spectre would
/// otherwise try to interpret as markup.
/// </summary>
public static class MarkupBuilder
{
    /// <param name="caretColumn">
    /// Where to paint the caret, in cells from the start of the line, or null for a line the
    /// caret isn't on. Fractional values are what the smooth caret is for — see
    /// <see cref="PaintCaret"/>.
    /// </param>
    public static string RenderLine(IReadOnlyList<Word> words, Theme theme, double? caretColumn = null)
    {
        var cells = BuildCells(words, theme);

        if (caretColumn is { } column)
            PaintCaret(cells, column, theme);

        return Serialise(cells);
    }

    /// <summary>One character of the line, and the Spectre style it is drawn in.</summary>
    private readonly record struct Cell(char Glyph, string Style);

    private static List<Cell> BuildCells(IReadOnlyList<Word> words, Theme theme)
    {
        var cells = new List<Cell>();

        for (var wordIndex = 0; wordIndex < words.Count; wordIndex++)
        {
            var word = words[wordIndex];

            for (var i = 0; i < word.Target.Length; i++)
            {
                var style = i < word.Typed.Count ? StyleFor(word.Typed[i].State, theme) : theme.Pending;
                cells.Add(new Cell(word.Target[i], style));
            }

            // Characters typed past the word's end are shown as themselves, since there's no
            // target character left for them to be compared against.
            for (var i = word.Target.Length; i < word.Typed.Count; i++)
                cells.Add(new Cell(word.Typed[i].Character, theme.Extra));

            if (wordIndex < words.Count - 1)
                cells.Add(new Cell(' ', theme.Pending));
        }

        return cells;
    }

    /// <summary>
    /// Paints a one-cell-wide caret starting at <paramref name="column"/>, which need not land on
    /// a cell boundary.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On a boundary, the caret is the cell's own character reverse-videoed — the character stays
    /// readable underneath, exactly as before there was anything to animate.
    /// </para>
    /// <para>
    /// Off a boundary, it spans two cells, and the block-element glyphs are the only way to fill
    /// part of one: they come in left-anchored widths (U+258F is a left eighth, U+2588 a full
    /// block), so the caret's leading edge — which fills a cell from the <em>left</em> — is just
    /// the matching glyph in the caret's colour. Its trailing edge needs the opposite, a cell
    /// filled from the right, and no right-anchored glyph exists past a single eighth. Hence the
    /// invert: the same left-anchored glyph drawn with the caret colour as the cell's background,
    /// so the glyph's own ink covers the part the caret has already left rather than the part it
    /// now occupies. Inverting is also what keeps this independent of the terminal's background
    /// colour, which we have no way to ask for — reverse video resolves that colour for us.
    /// </para>
    /// <para>
    /// Both cells lose their character for the ~100ms the caret is in flight over them, which is
    /// unavoidable: a cell holds one glyph, and here that glyph is the caret. (monkeytype's block
    /// caret covers the letter it sits on outright, so this is, if anything, less obtrusive.)
    /// </para>
    /// </remarks>
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

        // The caret can sit one past the last character on the line (a word ending flush with the
        // wrap width), and its leading edge one past that again.
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

    /// <summary>U+2588..U+258F: a block filling the left <paramref name="eighths"/>/8 of a cell.</summary>
    private static char LeftBlock(int eighths) => (char)(0x2590 - eighths);

    /// <summary>
    /// Emits the cells as markup, merging neighbours that share a style into one tag rather than
    /// wrapping every character in its own — the line is rewritten many times a second, so the
    /// difference is several kilobytes per frame of output the terminal doesn't have to parse.
    /// </summary>
    private static string Serialise(List<Cell> cells)
    {
        var builder = new StringBuilder();
        var run = new StringBuilder();
        var index = 0;

        while (index < cells.Count)
        {
            var style = cells[index].Style;

            run.Clear();
            while (index < cells.Count && cells[index].Style == style)
                run.Append(cells[index++].Glyph);

            builder.Append('[').Append(style).Append(']')
                .Append(Markup.Escape(run.ToString()))
                .Append("[/]");
        }

        return builder.ToString();
    }

    private static string StyleFor(CharacterState state, Theme theme) => state switch
    {
        CharacterState.Correct => theme.Correct,
        CharacterState.Incorrect => theme.Incorrect,
        CharacterState.Extra => theme.Extra,
        _ => theme.Pending
    };
}
