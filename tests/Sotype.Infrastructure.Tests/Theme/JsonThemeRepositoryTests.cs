using Sotype.Domain.Constants;
using Sotype.Domain.Themes;
using Sotype.Infrastructure.Theme;
using JsonException = System.Text.Json.JsonException;

namespace Sotype.Infrastructure.Tests.Theme;

[TestFixture]
public class JsonThemeRepositoryTests
{
    private string _directory = null!;
    private string _filePath = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"{StringLookups.AppName}-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "themes.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private static ThemeRecord CreateTheme(string name = "Test theme") => new(
        name,
        "colour1",
        "colour2",
        "colour3",
        "colour4",
        "colour5",
        "colour5",
        "colour6"
        );

    [Test]
    public void GetAll_ShouldReturnEmptyList_WhenNoFileExistsYet()
    {
        // Arrange
        // Act
        var repository = new JsonThemeRepository(_filePath);

        // Assert
        repository.GetAll().ShouldBeEquivalentTo(JsonThemeRepository.GetDefaultThemes());
    }

    [Test]
    public void SaveMultiple_ShouldSaveTheme_AndBeReturnedCorrectly()
    {
        // Arrange
        var repository = new JsonThemeRepository(_filePath);
        var record = CreateTheme();

        // Act
        repository.SaveMultiple([record]);

        // Assert
        repository.GetAll().ShouldContain(record);
    }

    [Test]
    public void Add_ShouldAppendToExistingRecords_WhenCalledMultipleTimes()
    {
        // Arrange
        var repository = new JsonThemeRepository(_filePath);

        repository.SaveMultiple([CreateTheme("Test theme 1"), CreateTheme("Test theme 2")]);
        repository.GetAll().Count.ShouldBe(2 + JsonThemeRepository.GetDefaultThemes().Count);
    }

    [Test]
    public void GetAll_ShouldThrowException_WhenFileContainsInvalidJson()
    {
        // Arrange
        File.WriteAllText(_filePath, "{ not valid json");

        // Act
        IReadOnlyList<ThemeRecord> Act()
        {
            var repository = new JsonThemeRepository(_filePath);
            return repository.GetAll();
        }

        // Assert
        Should.Throw<JsonException>(Act);
    }
}
