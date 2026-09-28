namespace Sotype.Cli.Theming;

///<summary>
/// The themes offered in the menu.
/// </summary>
/// TODO GH-4: make these configurable in JSON and load them
public static class ThemeCatalog
{
    public static readonly Theme SerikaDark = new(
        Name: "SerikaDark",
        Correct: "yellow",
        Incorrect: "red",
        Extra: "red3",
        Pending: "grey42",
        Accent: "yellow",
        CursorForeground: "grey11",
        CursorBackground: "yellow");

    public static readonly Theme Dracula = new(
        Name: "Dracula",
        Correct: "green",
        Incorrect: "red",
        Extra: "orange3",
        Pending: "grey54",
        Accent: "mediumpurple2",
        CursorForeground: "grey11",
        CursorBackground: "mediumpurple2");

    public static readonly Theme AyuLight = new(
        Name: "AyuLight",
        Correct: "green4",
        Incorrect: "red3",
        Extra: "orange3",
        Pending: "grey58",
        Accent: "darkorange3",
        CursorForeground: "white",
        CursorBackground: "darkorange3");

    public static readonly IReadOnlyList<Theme> All = [SerikaDark, Dracula, AyuLight];

    public static Theme GetByName(string name) =>
        All.FirstOrDefault(theme => string.Equals(theme.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? SerikaDark;
}
