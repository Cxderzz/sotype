namespace Sotype.Domain.Configuration;

/// <summary>Port for persisting <see cref="UserPreferences"/>. Implemented by <c>Sotype.Infrastructure</c>.</summary>
public interface IPreferencesRepository
{
    UserPreferences Load();

    void Save(UserPreferences preferences);
}
