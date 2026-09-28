using Sotype.Domain;

namespace Sotype.Cli.Rendering;

public record CaretTarget(int Line, int Column);

public record VisibleWindow(IReadOnlyList<IReadOnlyList<Word>> Lines, int StartLine);

/// <summary>
/// Greedily wraps a word sequence into display lines against a maximum width, locates the caret
/// within that layout, and picks a small scrolling window of lines around the active word.
/// </summary>
public static class LineWrapper
{
    public static IReadOnlyList<IReadOnlyList<Word>> WrapIntoLines(IReadOnlyList<Word> words, int maxWidth)
    {
        var lines = new List<List<Word>>();
        var currentLine = new List<Word>();
        var currentLineWidth = 0;

        foreach (var word in words)
        {
            var separatorWidth = currentLine.Count > 0 ? 1 : 0;
            var wordWidth = RenderedWidth(word);

            if (currentLine.Count > 0 && currentLineWidth + separatorWidth + wordWidth > maxWidth)
            {
                lines.Add(currentLine);
                currentLine = [];
                separatorWidth = 0;
                currentLineWidth = 0;
            }

            currentLine.Add(word);
            currentLineWidth += separatorWidth + wordWidth;
        }

        if (currentLine.Count > 0)
            lines.Add(currentLine);

        return lines;
    }

    /// <summary>
    /// Finds the cell the caret sits on: the one holding the next character to be typed.
    /// </summary>
    /// <remarks>
    /// The offset within the current word is simply how many characters have been typed into it,
    /// in every case. Short of the word's end that's the next target character; exactly at its
    /// end it lands on the space that follows (reusing that separator rather than appending a
    /// cell, which would widen the line); past its end — while typing extra characters — the word
    /// renders one cell per typed character, so the count still points just past the last of them.
    /// </remarks>
    public static CaretTarget LocateCaret(IReadOnlyList<IReadOnlyList<Word>> lines, Word currentWord)
    {
        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var column = 0;

            foreach (var word in lines[lineIndex])
            {
                if (ReferenceEquals(word, currentWord))
                    return new CaretTarget(lineIndex, column + word.Typed.Count);

                column += RenderedWidth(word) + 1; // + the separating space
            }
        }

        return new CaretTarget(0, 0);
    }

    /// <summary>
    /// Returns the line at <paramref name="caretLine"/> plus a small margin of lines before/after.
    /// </summary>
    public static VisibleWindow SelectVisibleWindow(
        IReadOnlyList<IReadOnlyList<Word>> lines, int caretLine, int linesBefore = 1, int linesAfter = 1)
    {
        if (lines.Count == 0)
            return new VisibleWindow(lines, 0);

        caretLine = Math.Clamp(caretLine, 0, lines.Count - 1);

        var start = Math.Max(0, caretLine - linesBefore);
        var end = Math.Min(lines.Count - 1, caretLine + linesAfter);

        return new VisibleWindow(lines.Skip(start).Take(end - start + 1).ToList(), start);
    }

    private static int RenderedWidth(Word word) => Math.Max(word.Target.Length, word.Typed.Count);
}
