using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SupportToolsServer.Tests.TestInfrastructure;

//Keeps every formatted log entry, so the tests can assert what was logged
internal sealed class CollectingLogger<T> : ILogger<T>
{
    public ConcurrentQueue<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Enqueue((logLevel, formatter(state, exception), exception));
    }
}
