namespace Sotype.Domain.Tests;

public class TestConfigurationTests
{
    [Test]
    public void ForDuration_ShouldSetTimeModeWithGivenDuration_WhenDurationIsPositive()
    {
        var configuration = TestConfiguration.ForDuration(TimeSpan.FromSeconds(60));

        configuration.Mode.ShouldBe(TestMode.Time);
        configuration.Duration.ShouldBe(TimeSpan.FromSeconds(60));
        configuration.WordCount.ShouldBeNull();
    }

    [Test]
    public void ForDuration_ShouldThrow_WhenDurationIsZeroOrNegative()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => TestConfiguration.ForDuration(TimeSpan.Zero));
    }

    [Test]
    public void ForWordCount_ShouldSetWordsModeWithGivenCount_WhenCountIsPositive()
    {
        var configuration = TestConfiguration.ForWordCount(25);

        configuration.Mode.ShouldBe(TestMode.Words);
        configuration.WordCount.ShouldBe(25);
        configuration.Duration.ShouldBeNull();
    }

    [Test]
    public void ForWordCount_ShouldThrow_WhenCountIsZeroOrNegative()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => TestConfiguration.ForWordCount(0));
    }

    [Test]
    public void ForWordCount_ShouldDefaultToAllowingBackspaceIntoPreviousWord_WhenNotSpecified()
    {
        var configuration = TestConfiguration.ForWordCount(10);

        configuration.AllowBackspaceIntoPreviousWord.ShouldBeTrue();
    }
}
