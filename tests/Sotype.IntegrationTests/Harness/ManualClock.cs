namespace Sotype.IntegrationTests.Harness;

/// <summary>
/// A clock that only moves when told to, so a test controls exactly how long a typing test
/// took. Timers are left on the real clock, so the test screen still paces its frames.
/// </summary>
public sealed class ManualClock : TimeProvider
{
    private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private long _ticks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override long GetTimestamp() => Interlocked.Read(ref _ticks);

    public override DateTimeOffset GetUtcNow() => Epoch + TimeSpan.FromTicks(GetTimestamp());

    public void Advance(TimeSpan by) => Interlocked.Add(ref _ticks, by.Ticks);
}
