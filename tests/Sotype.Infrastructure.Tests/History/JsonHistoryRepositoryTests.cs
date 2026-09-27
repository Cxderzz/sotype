using Sotype.Domain;
using Sotype.Domain.History;
using Sotype.Infrastructure.History;

namespace Sotype.Infrastructure.Tests.History;

public class JsonHistoryRepositoryTests
{
    private string _directory = null!;
    private string _filePath = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"sotype-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "history.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private static RunRecord CreateRecord(double wpm = 65.5) => new(
        Timestamp: new DateTimeOffset(2026, 9, 27, 12, 0, 0, TimeSpan.Zero),
        Mode: TestMode.Time,
        Duration: TimeSpan.FromSeconds(30),
        WordCount: null,
        Wpm: wpm,
        RawWpm: 70.0,
        Accuracy: 96.5,
        CorrectCharacters: 300,
        IncorrectCharacters: 10,
        ExtraCharacters: 1,
        MissedCharacters: 2,
        ThemeName: "SerikaDark");

    [Test]
    public void GetAll_ShouldReturnEmptyList_WhenNoFileExistsYet()
    {
        var repository = new JsonHistoryRepository(_filePath);

        repository.GetAll().ShouldBeEmpty();
    }

    [Test]
    public void Add_ShouldPersistARecordThatRoundTripsExactly_WhenCalled()
    {
        var repository = new JsonHistoryRepository(_filePath);
        var record = CreateRecord();

        repository.Add(record);

        repository.GetAll().ShouldBe(new[] { record });
    }

    [Test]
    public void Add_ShouldAppendToExistingRecords_WhenCalledMultipleTimes()
    {
        var repository = new JsonHistoryRepository(_filePath);

        repository.Add(CreateRecord(wpm: 60));
        repository.Add(CreateRecord(wpm: 70));

        repository.GetAll().Count.ShouldBe(2);
    }

    [Test]
    public void GetAll_ShouldReturnEmptyList_WhenFileContainsInvalidJson()
    {
        File.WriteAllText(_filePath, "{ not valid json");
        var repository = new JsonHistoryRepository(_filePath);

        repository.GetAll().ShouldBeEmpty();
    }
}
