using Sotype.Infrastructure.Words;

namespace Sotype.Infrastructure.Tests.Words;

public class EmbeddedWordListProviderTests
{
    [Test]
    public void TakeRandomWords_ShouldReturnRequestedCount_WhenCountIsPositive()
    {
        var provider = new EmbeddedWordListProvider();

        var words = provider.TakeRandomWords(30);

        words.Count.ShouldBe(30);
    }

    [Test]
    public void TakeRandomWords_ShouldReturnOnlyNonEmptyWords_WhenCalled()
    {
        var provider = new EmbeddedWordListProvider();

        var words = provider.TakeRandomWords(50);

        words.ShouldAllBe(word => !string.IsNullOrWhiteSpace(word));
    }

    [Test]
    public void TakeRandomWords_ShouldReturnEmptyList_WhenCountIsZero()
    {
        var provider = new EmbeddedWordListProvider();

        var words = provider.TakeRandomWords(0);

        words.ShouldBeEmpty();
    }

    [Test]
    public void TakeRandomWords_ShouldThrow_WhenCountIsNegative()
    {
        var provider = new EmbeddedWordListProvider();

        Should.Throw<ArgumentOutOfRangeException>(() => provider.TakeRandomWords(-1));
    }

    [Test]
    public void TakeRandomWords_ShouldProduceTheSameSequence_WhenGivenIdenticallySeededRandoms()
    {
        var first = new EmbeddedWordListProvider(new Random(42));
        var second = new EmbeddedWordListProvider(new Random(42));

        first.TakeRandomWords(20).ShouldBe(second.TakeRandomWords(20));
    }
}
