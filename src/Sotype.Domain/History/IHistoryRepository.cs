namespace Sotype.Domain.History;

/// <summary>
/// Contract for persisting completed runs. Implemented by <c>Sotype.Infrastructure</c>.
/// </summary>
public interface IHistoryRepository
{
    IReadOnlyList<RunRecord> GetAll();

    void Add(RunRecord record);
}
