using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Infrastructure.GitProjects;

//რიგში ჩამდგარ ბრძანებებს თითო-თითოდ, საკუთარ scope-ში ასრულებს. ბრძანების შეცდომას ლოგში
//LoggingDecorator წერს, აქ მხოლოდ მოულოდნელი გამონაკლისები იჭირება, რომ რიგის დამუშავება არ შეწყდეს
public sealed class GitProjectUpdateBackgroundService : BackgroundService
{
    private readonly ILogger<GitProjectUpdateBackgroundService> _logger;
    private readonly GitProjectUpdateQueue _queue;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public GitProjectUpdateBackgroundService(GitProjectUpdateQueue queue, IServiceScopeFactory serviceScopeFactory,
        ILogger<GitProjectUpdateBackgroundService> logger)
    {
        _queue = queue;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (UpdateGitProjectCommand command in _queue.ReadAll(stoppingToken))
        {
            try
            {
                await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateGitProjectCommand>>();
                await handler.Handle(command, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "Update of git project {GitProjectName} failed", command.GitProjectName);
            }
        }
    }
}
