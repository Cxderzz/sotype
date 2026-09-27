using System.Text;
using Sotype.Cli.Theming;
using Sotype.Domain;
using Spectre.Console;

namespace Sotype.Cli.Rendering;

/// <summary>
/// Turns typed-character state into Spectre <see cref="Markup"/>. Every literal character goes
/// through <see cref="Markup.Escape(string)"/>, since a typed character (or word-list content)
/// could itself contain '[' or ']', which Spectre would otherwise try to interpret as markup.
/// </summary>
public static class MarkupBuilder
{
    public static string RenderLine(IReadOnlyList<Word> words, Word currentWord, Theme theme)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < words.Count; i++)
        {
            var word = words[i];
            var hasNextWordOnLine = i < words.Count - 1;
            var isCurrent = ReferenceEquals(word, currentWord);

            // Once the current word has reached (or overrun) its target length, its cursor
            // belongs on the space that follows rather than on an extra cell appended to the
            // word — reusing that existing separator keeps the line's width constant instead
            // of growing it by one character. This applies for the whole time the word stays
            // "done or overrunning" (not just the single frame it first reaches target length):
            // RenderWord's own trailing-cursor fallback would otherwise keep tacking on a fresh
            // blank cell after every extra character typed, one bug for as long as overrunning
            // continues, not just at the boundary. RenderWord's fallback only fires when there's
            // no following word on this line to lend a space.
            var cursorOnSeparator = isCurrent && word.Typed.Count >= word.Target.Length && hasNextWordOnLine;

            builder.Append(RenderWord(word, isCurrent && !cursorOnSeparator, theme));

            if (hasNextWordOnLine)
            {
                if (cursorOnSeparator)
                    AppendCursor(builder, " ", theme);
                else
                    builder.Append(' ');
            }
        }

        return builder.ToString();
    }

    public static string RenderWord(Word word, bool isCurrent, Theme theme)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < word.Target.Length; i++)
        {
            var glyph = Markup.Escape(word.Target[i].ToString());

            if (isCurrent && i == word.Typed.Count)
            {
                AppendCursor(builder, glyph, theme);
                continue;
            }

            var color = i < word.Typed.Count ? ColorFor(word.Typed[i].State, theme) : theme.Pending;
            builder.Append($"[{color}]{glyph}[/]");
        }

        for (var i = word.Target.Length; i < word.Typed.Count; i++)
        {
            var glyph = Markup.Escape(word.Typed[i].Character.ToString());
            builder.Append($"[{theme.Extra}]{glyph}[/]");
        }

        // The cursor sits one past the last extra character once the user has overrun the
        // word's length but hasn't hit the extra-character cap yet.
        if (isCurrent && word.Typed.Count >= word.Target.Length && word.Typed.Count < word.MaxLength)
            AppendCursor(builder, " ", theme);

        return builder.ToString();
    }

    private static void AppendCursor(StringBuilder builder, string glyph, Theme theme) =>
        builder.Append($"[{theme.CursorForeground} on {theme.CursorBackground}]{glyph}[/]");

    private static string ColorFor(CharacterState state, Theme theme) => state switch
    {
        CharacterState.Correct => theme.Correct,
        CharacterState.Incorrect => theme.Incorrect,
        CharacterState.Extra => theme.Extra,
        _ => theme.Pending
    };
}
