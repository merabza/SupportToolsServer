using System;
using System.Threading;
using System.Threading.Tasks;

namespace SupportToolsServer.Tests.TestInfrastructure;

//The system clock whose timers run an hour in MillisecondsPerHour milliseconds, so that a period of hours passes in a
//test. PeriodicTimer creates its timer without a period and sets it with Change, so the timer is scaled as well
internal sealed class ScaledTimeProvider : TimeProvider
{
    public const int MillisecondsPerHour = 20;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        return new ScaledTimer(System.CreateTimer(callback, state, Scale(dueTime), Scale(period)));
    }

    private static TimeSpan Scale(TimeSpan time)
    {
        return time == Timeout.InfiniteTimeSpan
            ? time
            : TimeSpan.FromMilliseconds(time.TotalHours * MillisecondsPerHour);
    }

    private sealed class ScaledTimer : ITimer
    {
        private readonly ITimer _timer;

        public ScaledTimer(ITimer timer)
        {
            _timer = timer;
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            return _timer.Change(Scale(dueTime), Scale(period));
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return _timer.DisposeAsync();
        }
    }
}
