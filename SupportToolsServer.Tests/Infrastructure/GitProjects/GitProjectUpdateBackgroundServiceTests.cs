using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServer.Infrastructure.GitProjects;
using SupportToolsServer.Tests.TestInfrastructure;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.GitProjects;

public sealed class GitProjectUpdateBackgroundServiceTests : IAsyncDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly CollectingLogger<GitProjectUpdateBackgroundService> _logger = new();
    private readonly ServiceProvider _provider;
    private readonly GitProjectUpdateQueue _queue = new();
    private readonly Recorder _recorder = new();
    private readonly GitProjectUpdateBackgroundService _sut;

    public GitProjectUpdateBackgroundServiceTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_recorder);
        services.AddScoped<ICommandHandler<UpdateGitProjectCommand>, RecordingHandler>();
        _provider = services.BuildServiceProvider();
        _sut = new GitProjectUpdateBackgroundService(_queue, _provider.GetRequiredService<IServiceScopeFactory>(),
            _logger);
    }

    public async ValueTask DisposeAsync()
    {
        _sut.Dispose();
        _recorder.Dispose();
        await _provider.DisposeAsync();
    }

    private static UpdateGitProjectCommand Command(string name)
    {
        return new UpdateGitProjectCommand(name, $"address{name}", $"Folder{name}");
    }

    [Fact]
    public async Task ExecuteAsync_HandlesEveryCommandInItsOwnScope()
    {
        await _sut.StartAsync(CancellationToken.None);

        await _queue.Enqueue(Command("RepoA"), CancellationToken.None);
        await _queue.Enqueue(Command("RepoB"), CancellationToken.None);
        await _recorder.WaitForDisposedHandlers(2);

        Assert.Equal([Command("RepoA"), Command("RepoB")], _recorder.Handled);
        Assert.Equal(2, _recorder.CreatedHandlers);
        await _sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_LogsAnUnexpectedException_AndContinuesWithTheNextCommand()
    {
        var exception = new InvalidOperationException("git crashed");
        _recorder.ExceptionToThrow = exception;
        await _sut.StartAsync(CancellationToken.None);

        await _queue.Enqueue(Command("RepoA"), CancellationToken.None);
        await _recorder.WaitForDisposedHandlers(1);
        _recorder.ExceptionToThrow = null;
        await _queue.Enqueue(Command("RepoB"), CancellationToken.None);
        await _recorder.WaitForDisposedHandlers(2);

        Assert.Equal([Command("RepoA"), Command("RepoB")], _recorder.Handled);
        (LogLevel level, string message, Exception? loggedException) = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Error, level);
        Assert.Equal("Update of git project RepoA failed", message);
        Assert.Same(exception, loggedException);
        await _sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_StopsWithoutLogging_WhenTheHandlerIsCancelled()
    {
        _recorder.ExceptionToThrow = new OperationCanceledException();
        await _sut.StartAsync(CancellationToken.None);

        await _queue.Enqueue(Command("RepoA"), CancellationToken.None);

        Task executeTask = _sut.ExecuteTask!;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            executeTask.WaitAsync(Timeout, CancellationToken.None));
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task ExecuteAsync_PassesTheStoppingTokenToTheHandler()
    {
        await _sut.StartAsync(CancellationToken.None);

        await _queue.Enqueue(Command("RepoA"), CancellationToken.None);
        await _recorder.WaitForDisposedHandlers(1);
        await _sut.StopAsync(CancellationToken.None);

        Assert.True(_recorder.LastCancellationToken.CanBeCanceled);
        Assert.True(_recorder.LastCancellationToken.IsCancellationRequested);
    }

    internal sealed class Recorder : IDisposable
    {
        private readonly SemaphoreSlim _disposed = new(0);
        private int _createdHandlers;

        public ConcurrentQueue<UpdateGitProjectCommand> Handled { get; } = new();
        public Exception? ExceptionToThrow { get; set; }
        public CancellationToken LastCancellationToken { get; set; }
        public int CreatedHandlers => _createdHandlers;
        private int DisposedHandlers { get; set; }

        public void Dispose()
        {
            _disposed.Dispose();
        }

        public void HandlerCreated()
        {
            Interlocked.Increment(ref _createdHandlers);
        }

        public void HandlerDisposed()
        {
            _disposed.Release();
        }

        public async Task WaitForDisposedHandlers(int count)
        {
            while (DisposedHandlers < count)
            {
                Assert.True(await _disposed.WaitAsync(Timeout, CancellationToken.None),
                    "The handler was not disposed in time");
                DisposedHandlers++;
            }
        }
    }

    //A scoped handler: its disposal shows that the scope of the command was disposed
    internal sealed class RecordingHandler : ICommandHandler<UpdateGitProjectCommand>, IAsyncDisposable
    {
        private readonly Recorder _recorder;

        public RecordingHandler(Recorder recorder)
        {
            _recorder = recorder;
            _recorder.HandlerCreated();
        }

        public ValueTask DisposeAsync()
        {
            _recorder.HandlerDisposed();
            return ValueTask.CompletedTask;
        }

        public Task<Result> Handle(UpdateGitProjectCommand command, CancellationToken cancellationToken)
        {
            _recorder.Handled.Enqueue(command);
            _recorder.LastCancellationToken = cancellationToken;
            if (_recorder.ExceptionToThrow is not null)
            {
                throw _recorder.ExceptionToThrow;
            }

            return Task.FromResult(Result.Success());
        }
    }
}
