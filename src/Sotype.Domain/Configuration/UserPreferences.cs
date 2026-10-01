namespace Sotype.Domain.Configuration;

/// <summary>
/// The user's last-used settings, restored as the menu's defaults on the next launch.
/// </summary>
public sealed record UserPreferences(TestMode Mode, TimeSpan? Duration, int? WordCount, string ThemeName)
{
    public static UserPreferences Default { get; } = new(TestMode.Time, TimeSpan.FromSeconds(30), null, "SerikaDark");
}
