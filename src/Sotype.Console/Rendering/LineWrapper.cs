using Sotype.Domain;

namespace Sotype.Cli.Rendering;

/// <summary>
/// Greedily wraps a word sequence into display lines against a maximum width, and picks a
/// small scrolling window of lines around the active word — so a long test scrolls like
/// monkeytype's box rather than filling (or overflowing) the whole screen.
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
            // Typed.Count can exceed Target.Length once extra characters are typed past a
            // word's end — using Target.Length alone here would under-count that word's true
            // rendered width, letting a line silently overflow past maxWidth.
            var wordWidth = Math.Max(word.Target.Length, word.Typed.Count);

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
    /// Finds the line containing <paramref name="currentWord"/> (by reference — this works even
    /// when <paramref name="lines"/> was built from a partial slice of the session's full word
    /// list) and returns it plus a small margin of lines before/after.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<Word>> SelectVisibleWindow(
        IReadOnlyList<IReadOnlyList<Word>> lines, Word currentWord, int linesBefore = 1, int linesAfter = 1)
    {
        if (lines.Count == 0)
            return lines;

        var currentLineIndex = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Any(word => ReferenceEquals(word, currentWord)))
            {
                currentLineIndex = i;
                break;
            }
        }

        var start = Math.Max(0, currentLineIndex - linesBefore);
        var end = Math.Min(lines.Count - 1, currentLineIndex + linesAfter);
        return lines.Skip(start).Take(end - start + 1).ToList();
    }
}
