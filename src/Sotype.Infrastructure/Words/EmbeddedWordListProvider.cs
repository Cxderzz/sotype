using System.Reflection;
using Sotype.Domain.Words;

namespace Sotype.Infrastructure.Words;

/// <summary>
/// Supplies random words from a common-English word list embedded in this assembly, so the
/// app never needs network access to run a test.
/// </summary>
public sealed class EmbeddedWordListProvider : IWordListProvider
{
    private const string ResourceName = "Sotype.Infrastructure.Words.Resources.words_english_200.txt";

    private readonly IReadOnlyList<string> _words;
    private readonly Random _random;

    /// <param name="random">Injectable for deterministic tests; defaults to <see cref="Random.Shared"/>.</param>
    public EmbeddedWordListProvider(Random? random = null)
    {
        _random = random ?? Random.Shared;
        _words = LoadWords();
    }

    public IReadOnlyList<string> TakeRandomWords(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must not be negative.");

        var words = new string[count];
        for (var i = 0; i < count; i++)
            words[i] = _words[_random.Next(_words.Count)];

        return words;
    }

    private static IReadOnlyList<string> LoadWords()
    {
        var assembly = typeof(EmbeddedWordListProvider).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found in {assembly.FullName}.");
        using var reader = new StreamReader(stream);

        var words = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0)
                words.Add(trimmed);
        }

        if (words.Count == 0)
            throw new InvalidOperationException($"Embedded resource '{ResourceName}' contained no words.");

        return words;
    }
}
