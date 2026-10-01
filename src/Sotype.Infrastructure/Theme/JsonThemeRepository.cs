using System.Text.Json;
using Sotype.Domain.Constants;
using Sotype.Domain.Themes;
using ThemeObject = Sotype.Domain.Themes.Theme;

namespace Sotype.Infrastructure.Theme;

public class JsonThemeRepository : IThemeRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public JsonThemeRepository(string? filePath = null)
    {
        _filePath = filePath ?? DefaultFilePath();
        InitialiseConfig();
    }

    private void InitialiseConfig()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        if (!File.Exists(_filePath))
            File.Create(_filePath).Dispose(); // Ensure the file exists before reading

        var json = File.ReadAllText(_filePath);

        var themes = DeserializeJson(json);

        if (themes is not null)
            return;

        SeedAndGetDefaultThemes();
    }

    public IReadOnlyList<ThemeObject> GetAll()
    {
        var json = File.ReadAllText(_filePath);

        var themes = DeserializeJson(json);

        return themes ?? throw new JsonException("No theme configuration file found.");
    }

    private List<ThemeObject> SeedAndGetDefaultThemes()
    {
        var json = JsonSerializer.Serialize(GetDefaultThemes(), SerializerOptions);

        File.WriteAllText(_filePath, json);

        return DeserializeJson(json) ?? throw new JsonException("Failed to deserialize default themes after seeding.");
    }

    private List<ThemeObject>? DeserializeJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<ThemeObject>>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public ThemeObject GetByName(string name)
    {
        return GetAll().FirstOrDefault(theme => theme.Name == name) ?? throw new KeyNotFoundException($"Theme with name '{name}' not found.");
    }

    public static List<ThemeObject> GetDefaultThemes()
    {
        var serikaDark = new ThemeObject(
            Name: "SerikaDark",
            Correct: "yellow",
            Incorrect: "red",
            Extra: "red3",
            Pending: "grey42",
            Accent: "yellow",
            CursorForeground: "grey11",
            CursorBackground: "yellow");

        var dracula = new ThemeObject(
            Name: "Dracula",
            Correct: "green",
            Incorrect: "red",
            Extra: "orange3",
            Pending: "grey54",
            Accent: "mediumpurple2",
            CursorForeground: "grey11",
            CursorBackground: "mediumpurple2");

        var ayuLight = new ThemeObject(
            Name: "AyuLight",
            Correct: "green4",
            Incorrect: "red3",
            Extra: "orange3",
            Pending: "grey58",
            Accent: "darkorange3",
            CursorForeground: "white",
            CursorBackground: "darkorange3");

        return [serikaDark, dracula, ayuLight];
    }

    private static string DefaultFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), StringLookups.AppName, "themes.json");

    public void SaveMultiple(IEnumerable<ThemeObject> themes)
    {
        var themesList = themes.ToList();
        if (!themesList.Any()) return;

        var existingThemes = GetAll();
        var updatedThemes = existingThemes.Concat(themesList).Distinct().ToList();

        var json = JsonSerializer.Serialize(updatedThemes, SerializerOptions);
        File.WriteAllText(_filePath, json);
    }
}
