using Sotype.Domain;

namespace Sotype.Cli.Input;

/// <summary>
/// Applies a keypress to the session, or reports the screen-level action it asks for.
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
