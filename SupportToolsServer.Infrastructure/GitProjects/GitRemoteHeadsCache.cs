using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupportToolsServer.Application.GitRepos.GitRemoteHeads;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServer.Infrastructure.Options;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Infrastructure.GitProjects;

//ჩანაწერები git-ის სახელით ინახება (OrdinalIgnoreCase). ჩანაწერი ვადაგასულია, როცა მისი შემოწმებიდან
//AppOptions:GitRemoteHeadsCacheMinutes გავიდა ან რეპოზიტორიის მისამართი შეიცვალა. ერთ ჯერზე ერთი მოთხოვნა ახლებს
//ქეშს (_lock): მეორე მას ელოდება და უკვე განახლებულ ჩანაწერებს იღებს, ხოლო Invalidate მიმდინარე განახლების
//დასრულების შემდეგ შლის ჩანაწერს, ამიტომ push-მდე წაკითხული ვერსია push-ის შეტყობინებას ვერ გადაფარავს
public sealed class GitRemoteHeadsCache : IGitRemoteHeadsCache, IDisposable
{
    //ერთდროულად მაქსიმუმ ამდენი git ls-remote
    internal const int MaxParallelism = 8;

    private readonly IOptions<AppOptions> _appOptions;
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly IGitClient _gitClient;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<GitRemoteHeadsCache> _logger;
    private readonly TimeProvider _timeProvider;

    public GitRemoteHeadsCache(IGitClient gitClient, IOptions<AppOptions> appOptions, TimeProvider timeProvider,
        ILogger<GitRemoteHeadsCache> logger)
    {
        _gitClient = gitClient;
        _appOptions = appOptions;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public void Dispose()
    {
        _lock.Dispose();
    }

    public async Task<List<StsGitRemoteHeadDataModel>> GetHeads(IReadOnlyList<GitRepo> gitRepos,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            TimeSpan lifetime = TimeSpan.FromMinutes(_appOptions.Value.GitRemoteHeadsCacheMinutes);
            var heads = new StsGitRemoteHeadDataModel[gitRepos.Count];
            await Parallel.ForAsync(0, gitRepos.Count,
                new ParallelOptions { MaxDegreeOfParallelism = MaxParallelism, CancellationToken = cancellationToken },
                (i, _) =>
                {
                    heads[i] = GetHead(gitRepos[i], lifetime);
                    return ValueTask.CompletedTask;
                });
            return [.. heads];
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task Invalidate(string gitName, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _entries.TryRemove(gitName, out _);
        }
        finally
        {
            _lock.Release();
        }
    }

    private StsGitRemoteHeadDataModel GetHead(GitRepo gitRepo, TimeSpan lifetime)
    {
        if (_entries.TryGetValue(gitRepo.Name, out CacheEntry? entry) && entry.Address == gitRepo.Address &&
            _timeProvider.GetUtcNow() - entry.CheckedAt < lifetime)
        {
            return ToContract(gitRepo.Name, entry.Head);
        }

        //შემოწმების დრო ls-remote-ის დაწყებისაა: ამ მომენტამდე remote-ში გაკეთებული ცვლილება შედეგში უკვე ჩანს
        DateTimeOffset checkedAt = _timeProvider.GetUtcNow();
        Result<GitRemoteHead> headResult = _gitClient.GetRemoteHead(gitRepo.Address);
        if (headResult.IsSuccess)
        {
            _entries[gitRepo.Name] = new CacheEntry(gitRepo.Address, headResult.Value, checkedAt);
            return ToContract(gitRepo.Name, headResult.Value);
        }

        _entries.TryRemove(gitRepo.Name, out _);
        //შეცდომის აღწერა მისამართს შეიცავს (git-ის ბრძანება), მისამართში კი შეიძლება ტოკენი იყოს, ამიტომ კლიენტი
        //მხოლოდ ზოგად აღწერას იღებს, სრული აღწერა ლოგში იწერება
        _logger.LogWarning("Remote head of git {GitName} cannot be read: {ErrorCode}", gitRepo.Name,
            headResult.Error.Code);
        return new StsGitRemoteHeadDataModel
        {
            GitName = gitRepo.Name, ErrorDescription = $"Remote head of git {gitRepo.Name} cannot be read"
        };
    }

    private static StsGitRemoteHeadDataModel ToContract(string gitName, GitRemoteHead head)
    {
        return new StsGitRemoteHeadDataModel
        {
            GitName = gitName, BranchName = head.BranchName, CommitId = head.CommitId
        };
    }

    private sealed record CacheEntry(string Address, GitRemoteHead Head, DateTimeOffset CheckedAt);
}
