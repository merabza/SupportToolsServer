using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupportToolsServer.Application.GitRepos.RefreshGitProjects;
using SupportToolsServer.Infrastructure.GitProjects;
using SupportToolsServer.Infrastructure.Options;
using SupportToolsServer.Tests.TestInfrastructure;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace SupportToolsServer.Tests.Infrastructure.GitProjects;

//An hour of the refresh period passes in ScaledTimeProvider.MillisecondsPerHour milliseconds
public sealed class GitProjectsRefreshBackgroundServiceTests : IAsyncDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly CollectingLogger<GitProjectsRefreshBackgroundService> _logger = new();
    private readonly ServiceProvider _provider;
    private readonly Recorder _recorder = new();

    public GitProjectsRefreshBackgroundServiceTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_recorder);
        services.AddScoped<ICommandHandler<RefreshGitProjectsCommand>, RecordingHandler>();
        _provider = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        _recorder.Dispose();
        await _provider.DisposeAsync();
    }

    private GitProjectsRefreshBackgroundService CreateService(int refreshHours)
    {
        return new GitProjectsRefreshBackgroundService(
            MsOptions.Create(new AppOptions { WorkFolder = "Work", GitProjectsRefreshHours = refreshHours }),
            _provider.GetRequiredService<IServiceScopeFactory>(), new ScaledTimeProvider(), _logger);
    }

    //The first refresh runs at the start, the next ones after every period, each in its own scope
    [Fact]
    public async Task ExecuteAsync_RefreshesAtTheStartAndThenAfterEveryPeriod()
    {
        using GitProjectsRefreshBackgroundService sut = CreateService(1);

        await sut.StartAsync(CancellationToken.None);
        await _recorder.WaitForDisposedHandlers(3);
        await sut.StopAsync(CancellationToken.None);

        Assert.True(_recorder.Handled >= 3);
        Assert.Equal(_recorder.Handled, _recorder.CreatedHandlers);
        Assert.Empty(_logger.Entries);
    }

    //The first refresh does not wait for a period: with a period of the longest allowed refresh it still comes at once
    [Fact]
    public async Task ExecuteAsync_RefreshesOnce_BeforeTheFirstPeriodEnds()
    {
        using GitProjectsRefreshBackgroundService sut = CreateService(AppOptions.MaxGitProjectsRefreshHours);

        await sut.StartAsync(CancellationToken.None);
        await _recorder.WaitForDisposedHandlers(1);
        await sut.StopAsync(CancellationToken.None);

        Assert.Equal(1, _recorder.Handled);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRefresh_WhenTheRefreshIsTurnedOff()
    {
        using GitProjectsRefreshBackgroundService sut = CreateService(0);

        await sut.StartAsync(CancellationToken.None);
        await sut.ExecuteTask!.WaitAsync(Timeout, CancellationToken.None);

        Assert.True(sut.ExecuteTask.IsCompletedSuccessfully);
        Assert.Equal(0, _recorder.Handled);
        Assert.Empty(_logger.Entries);
    }

    //An unexpected exception (for example, of an unreachable database) is logged and the next period refreshes again
    [Fact]
    public async Task ExecuteAsync_LogsAnUnexpectedException_AndRefreshesAgainAfterThePeriod()
    {
        var exception = new InvalidOperationException("database is down");
        _recorder.ExceptionToThrow = exception;
        using GitProjectsRefreshBackgroundService sut = CreateService(1);

        await sut.StartAsync(CancellationToken.None);
        await _recorder.WaitForDisposedHandlers(2);
        await sut.StopAsync(CancellationToken.None);

        Assert.True(_logger.Entries.Count >= 2);
        Assert.All(_logger.Entries, entry =>
        {
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Equal("Refresh of git projects failed", entry.Message);
            Assert.Same(exception, entry.Exception);
        });
    }

    //The stop cancels the refresh that runs; the cancellation is not an error to log
    [Fact]
    public async Task ExecuteAsync_StopsWithoutLogging_WhenTheRefreshIsCancelled()
    {
        _recorder.ExceptionToThrow = new OperationCanceledException();
        using GitProjectsRefreshBackgroundService sut = CreateService(1);

        await sut.StartAsync(CancellationToken.None);

        Task executeTask = sut.ExecuteTask!;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            executeTask.WaitAsync(Timeout, CancellationToken.None));
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task ExecuteAsync_PassesTheStoppingTokenToTheHandler()
    {
        using GitProjectsRefreshBackgroundService sut = CreateService(AppOptions.MaxGitProjectsRefreshHours);

        await sut.StartAsync(CancellationToken.None);
        await _recorder.WaitForDisposedHandlers(1);
        await sut.StopAsync(CancellationToken.None);

        Assert.True(_recorder.LastCancellationToken.CanBeCanceled);
        Assert.True(_recorder.LastCancellationToken.IsCancellationRequested);
    }

    internal sealed class Recorder : IDisposable
    {
        private readonly SemaphoreSlim _disposed = new(0);
        private int _createdHandlers;
        private int _handled;

        public int Handled => _handled;
        public int CreatedHandlers => _createdHandlers;
        public Exception? ExceptionToThrow { get; set; }
        public CancellationToken LastCancellationToken { get; set; }
        private int DisposedHandlers { get; set; }

        public void Dispose()
        {
            _disposed.Dispose();
        }

        public void HandlerCreated()
        {
            Interlocked.Increment(ref _createdHandlers);
        }

        public void CommandHandled()
        {
            Interlocked.Increment(ref _handled);
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

    //A scoped handler: its disposal shows that the scope of the refresh was disposed
    internal sealed class RecordingHandler : ICommandHandler<RefreshGitProjectsCommand>, IAsyncDisposable
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

        public Task<Result> Handle(RefreshGitProjectsCommand command, CancellationToken cancellationToken)
        {
            _recorder.CommandHandled();
            _recorder.LastCancellationToken = cancellationToken;
            if (_recorder.ExceptionToThrow is not null)
            {
                throw _recorder.ExceptionToThrow;
            }

            return Task.FromResult(Result.Success());
        }
    }
}
