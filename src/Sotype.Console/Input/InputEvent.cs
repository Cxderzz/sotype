namespace Sotype.Cli.Input;

/// <summary>
/// A screen-level action a keypress requests, as opposed to a key that just mutates the active
/// <see cref="Sotype.Domain.TypingSession"/> in place (which <see cref="InputReader"/> handles
/// inline and reports back as <see cref="None"/>).
/// </summary>
public enum InputEvent
{
    None,
    Restart,
    BackToMenu,
    Quit
}
