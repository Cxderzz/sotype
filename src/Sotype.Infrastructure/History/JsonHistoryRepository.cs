using System.Text.Json;
using Sotype.Domain.History;

namespace Sotype.Infrastructure.History;

/// <summary>
/// Persists <see cref="RunRecord"/>s as a JSON array. Defaults to <c>$XDG_DATA_HOME/sotype/history.json</c>
/// on Linux (.NET's <see cref="Environment.SpecialFolder.LocalApplicationData"/> already resolves there);
/// pass an explicit <paramref name="filePath"/> to point at a different location, e.g. in tests.
/// </summary>
public sealed class JsonHistoryRepository(string? filePath = null) : IHistoryRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _filePath = filePath ?? DefaultFilePath();

    public IReadOnlyList<RunRecord> GetAll()
    {
        if (!File.Exists(_filePath))
            return Array.Empty<RunRecord>();

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<RunRecord>>(json) ?? new List<RunRecord>();
        }
        catch (JsonException)
        {
            return Array.Empty<RunRecord>();
        }
    }

    public void Add(RunRecord record)
    {
        var records = GetAll().ToList();
        records.Add(record);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(_filePath, JsonSerializer.Serialize(records, SerializerOptions));
    }

    private static string DefaultFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "sotype", "history.json");
}
