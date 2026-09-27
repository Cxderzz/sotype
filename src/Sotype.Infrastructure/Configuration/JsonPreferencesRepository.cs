using System.Text.Json;
using Sotype.Domain.Configuration;

namespace Sotype.Infrastructure.Configuration;

/// <summary>
/// Persists <see cref="UserPreferences"/> as JSON. Defaults to <c>$XDG_CONFIG_HOME/sotype/preferences.json</c>
/// on Linux (.NET's <see cref="Environment.SpecialFolder.ApplicationData"/> already resolves there);
/// pass an explicit <paramref name="filePath"/> to point at a different location, e.g. in tests.
/// </summary>
public sealed class JsonPreferencesRepository(string? filePath = null) : IPreferencesRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _filePath = filePath ?? DefaultFilePath();

    public UserPreferences Load()
    {
        if (!File.Exists(_filePath))
            return UserPreferences.Default;

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<UserPreferences>(json) ?? UserPreferences.Default;
        }
        catch (JsonException)
        {
            return UserPreferences.Default;
        }
    }

    public void Save(UserPreferences preferences)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(_filePath, JsonSerializer.Serialize(preferences, SerializerOptions));
    }

    private static string DefaultFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "sotype", "preferences.json");
}
