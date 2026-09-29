using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace SupportToolsServer.Tests.TestInfrastructure;

//Collects the Debug.WriteLine lines (Debug writes to the Trace listeners)
internal sealed class CollectingTraceListener : TraceListener
{
    private readonly ConcurrentQueue<string> _lines = new();

    public IReadOnlyCollection<string> Lines => _lines;

    public override void Write(string? message)
    {
    }

    public override void WriteLine(string? message)
    {
        if (message is not null)
        {
            _lines.Enqueue(message);
        }
    }
}
