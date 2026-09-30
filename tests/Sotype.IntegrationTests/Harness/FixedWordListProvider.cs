using Sotype.Domain.Words;

namespace Sotype.IntegrationTests.Harness;

/// <summary>
/// Hands out the given words in order, cycling, so a test knows what to type.
/// </summary>
public sealed class FixedWordListProvider(params string[] words) : IWordListProvider
{
    private int _next;

    public IReadOnlyList<string> TakeRandomWords(int count) =>
        Enumerable.Range(0, count).Select(_ => words[_next++ % words.Length]).ToArray();
}
