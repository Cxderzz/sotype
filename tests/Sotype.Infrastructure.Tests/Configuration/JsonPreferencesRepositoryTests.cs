using Sotype.Domain;
using Sotype.Domain.Configuration;
using Sotype.Infrastructure.Configuration;

namespace Sotype.Infrastructure.Tests.Configuration;

public class JsonPreferencesRepositoryTests
{
    private string _directory = null!;
    private string _filePath = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"sotype-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "preferences.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Test]
    public void Load_ShouldReturnDefaultPreferences_WhenNoFileExistsYet()
    {
        var repository = new JsonPreferencesRepository(_filePath);

        repository.Load().ShouldBe(UserPreferences.Default);
    }

    [Test]
    public void Save_ShouldPersistPreferencesThatRoundTripExactly_WhenCalled()
    {
        var repository = new JsonPreferencesRepository(_filePath);
        var preferences = new UserPreferences(TestMode.Words, null, 50, "Dracula");

        repository.Save(preferences);

        repository.Load().ShouldBe(preferences);
    }

    [Test]
    public void Load_ShouldReturnDefaultPreferences_WhenFileContainsInvalidJson()
    {
        File.WriteAllText(_filePath, "{ not valid json");
        var repository = new JsonPreferencesRepository(_filePath);

        repository.Load().ShouldBe(UserPreferences.Default);
    }
}
