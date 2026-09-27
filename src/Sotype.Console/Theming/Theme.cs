namespace Sotype.Cli.Theming;

/// <summary>
/// A color palette for the typing screen. Presentation concept, deliberately kept out of
/// the domain — the correctness of a typed character is a domain rule, but which color
/// represents "correct" is not.
/// </summary>
/// <remarks>Color values are Spectre.Console markup color names, e.g. "yellow" or "grey42".</remarks>
public sealed record Theme(
    string Name,
    string Correct,
    string Incorrect,
    string Extra,
    string Pending,
    string Accent,
    string CursorForeground,
    string CursorBackground);
