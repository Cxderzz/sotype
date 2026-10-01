namespace Sotype.Domain.Themes;

/// <summary>
/// Contract for persisting themes. Implemented by <c>Sotype.Infrastructure</c>.
/// </summary>
public interface IThemeRepository
{
    public IReadOnlyList<ThemeRecord> GetAll();

    public ThemeRecord GetByName(string name);

    public void SaveMultiple(IEnumerable<ThemeRecord> themes);
}
