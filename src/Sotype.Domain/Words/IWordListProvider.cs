namespace Sotype.Domain.Words;

/// <summary>
/// Port through which the domain asks for words to type, without knowing where they come from.
/// Implemented by <c>Sotype.Infrastructure</c> (e.g. reading an embedded word list).
/// </summary>
public interface IWordListProvider
{
    /// <summary>Returns <paramref name="count"/> randomly chosen words.</summary>
    IReadOnlyList<string> TakeRandomWords(int count);
}
