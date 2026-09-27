using Sotype.Domain;

namespace Sotype.Cli.Input;

/// <summary>
/// Translates one raw keypress into either a mutation on the active <see cref="TypingSession"/>
/// (space commits the current word, backspace corrects it, anything else is typed as a
/// character) or a screen-level <see cref="InputEvent"/> the render loop must react to
/// (Tab restarts, Escape returns to the menu). Depends only on <see cref="ConsoleKeyInfo"/>
/// and the domain — it never touches <see cref="Console"/> itself, so it's unit-testable
/// without a real terminal.
/// </summary>
public static class InputReader
{
    public static InputEvent Dispatch(ConsoleKeyInfo key, TypingSession session)
    {
        switch (key.Key)
        {
            case ConsoleKey.Tab:
                return InputEvent.Restart;

            case ConsoleKey.Escape:
                return InputEvent.BackToMenu;

            case ConsoleKey.Backspace:
                session.Backspace();
                return InputEvent.None;

            case ConsoleKey.Spacebar:
                session.Commit();
                return InputEvent.None;

            default:
                if (!char.IsControl(key.KeyChar))
                    session.TypeCharacter(key.KeyChar);
                return InputEvent.None;
        }
    }
}
