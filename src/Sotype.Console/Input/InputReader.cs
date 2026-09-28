using Sotype.Domain;

namespace Sotype.Cli.Input;

/// <summary>
/// Translates a keypress into either a mutation on the active <see cref="TypingSession"/>
/// or a screen-level <see cref="InputEvent"/> the render loop must react to
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
