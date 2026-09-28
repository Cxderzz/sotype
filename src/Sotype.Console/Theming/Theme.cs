namespace Sotype.Cli.Theming;

/// <summary>
/// A colour palette for the typing screen, as Spectre.Console markup colour names.
/// </summary>
public sealed record Theme(
    string Name,
    string Correct,
    string Incorrect,
    string Extra,
    string Pending,
    string Accent,
    string CursorForeground,
    string CursorBackground);
