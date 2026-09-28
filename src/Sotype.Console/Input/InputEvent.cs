namespace Sotype.Cli.Input;

/// <summary>
/// A screen-level action a keypress asks for, as opposed to typing into the session.
/// </summary>
public enum InputEvent
{
    None,
    Restart,
    BackToMenu,
    Quit
}
