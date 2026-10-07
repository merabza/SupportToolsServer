using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupportToolsServer.Application.GitRepos.RefreshGitProjects;
using SupportToolsServer.Infrastructure.Options;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Infrastructure.GitProjects;

//სერვერის კლონებსა და GitProjects-ს ახალს ინარჩუნებს (B9): სტარტზე და შემდეგ ყოველ AppOptions:GitProjectsRefreshHours
//საათში ყველა რეპოზიტორიის განახლება რიგში დგება (RefreshGitProjectsCommand). 0 ამ განახლებას თიშავს. ბრძანების
//შეცდომას ლოგში LoggingDecorator წერს, აქ მხოლოდ მოულოდნელი გამონაკლისები იჭირება (მაგალითად, მიუწვდომელი ბაზა), რომ
//შემდეგი განახლება მაინც შესრულდეს
public sealed class GitProjectsRefreshBackgroundService : BackgroundService
{
    private readonly IOptions<AppOptions> _appOptions;
    private readonly ILogger<GitProjectsRefreshBackgroundService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TimeProvider _timeProvider;

    public GitProjectsRefreshBackgroundService(IOptions<AppOptions> appOptions,
        IServiceScopeFactory serviceScopeFactory, TimeProvider timeProvider,
        ILogger<GitProjectsRefreshBackgroundService> logger)
    {
        _appOptions = appOptions;
        _serviceScopeFactory = serviceScopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int refreshHours = _appOptions.Value.GitProjectsRefreshHours;
        if (refreshHours <= 0)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(refreshHours), _timeProvider);
        do
        {
            await Refresh(stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task Refresh(CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<RefreshGitProjectsCommand>>();
            await handler.Handle(new RefreshGitProjectsCommand(), stoppingToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, "Refresh of git projects failed");
        }
    }
}
