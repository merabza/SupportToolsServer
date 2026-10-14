using System;

namespace SupportToolsServer.Tests.TestInfrastructure;

//A clock that moves only when the test moves it
internal sealed class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }

    public void Advance(TimeSpan time)
    {
        _utcNow += time;
    }
}
