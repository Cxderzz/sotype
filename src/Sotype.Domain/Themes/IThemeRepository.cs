namespace Sotype.Domain.Themes;

/// <summary>
/// Contract for persisting themes. Implemented by <c>Sotype.Infrastructure</c>.
/// </summary>
public interface IThemeRepository
{
    public IReadOnlyList<Theme> GetAll();

    public Theme GetByName(string name);
}