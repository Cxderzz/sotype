using Sotype.Domain;

namespace Sotype.Cli.Rendering;

/// <summary>
/// The lines of words on screen and where the caret sits within them.
/// </summary>
/// <param name="Lines">The visible window of wrapped lines.</param>
/// <param name="CaretRow">The caret's line, indexed into <paramref name="Lines"/>.</param>
/// <param name="CaretLine">The caret's line, indexed into the whole wrapped text.</param>
/// <param name="CaretColumn">Cells from the start of that line.</param>
public sealed record WordLayout(
    IReadOnlyList<IReadOnlyList<Word>> Lines,
    int CaretRow,
    int CaretLine,
    int CaretColumn)
{
    private const int LinesAroundCaret = 1;

    /// <summary>
    /// Wraps <paramref name="words"/> in full, not from the current word: re-wrapping from a
    /// moving start point regroups earlier lines, which shows up as words jumping while typing.
    /// </summary>
    public static WordLayout Create(IReadOnlyList<Word> words, Word currentWord, int width)
    {
        var lines = Wrap(words, width);
        var (caretLine, caretColumn) = LocateCaret(lines, currentWord);

        var start = Math.Max(0, caretLine - LinesAroundCaret);
        var end = Math.Min(lines.Count - 1, caretLine + LinesAroundCaret);
        var visible = lines.Skip(start).Take(end - start + 1).ToList();

        return new WordLayout(visible, caretLine - start, caretLine, caretColumn);
    }

    private static List<List<Word>> Wrap(IReadOnlyList<Word> words, int width)
    {
        var lines = new List<List<Word>>();
        var line = new List<Word>();
        var lineWidth = 0;

        foreach (var word in words)
        {
            var separator = line.Count > 0 ? 1 : 0;

            if (line.Count > 0 && lineWidth + separator + Width(word) > width)
            {
                lines.Add(line);
                line = [];
                separator = 0;
                lineWidth = 0;
            }

            line.Add(word);
            lineWidth += separator + Width(word);
        }

        if (line.Count > 0)
            lines.Add(line);

        return lines;
    }

    /// <summary>
    /// The typed count doubles as the caret's offset into the current word: the next character
    /// mid-word, the trailing space at the word's end, or one past the last extra character.
    /// </summary>
    private static (int Line, int Column) LocateCaret(List<List<Word>> lines, Word currentWord)
    {
        for (var line = 0; line < lines.Count; line++)
        {
            var column = 0;

            foreach (var word in lines[line])
            {
                if (ReferenceEquals(word, currentWord))
                    return (line, column + word.Typed.Count);

                column += Width(word) + 1;
            }
        }

        return (0, 0);
    }

    /// <summary>
    /// Cells a word occupies. Characters typed past its end widen it.
    /// </summary>
    private static int Width(Word word) => Math.Max(word.Target.Length, word.Typed.Count);
}
