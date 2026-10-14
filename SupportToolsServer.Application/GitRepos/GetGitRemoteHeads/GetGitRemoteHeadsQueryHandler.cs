using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.GitRepos.GitRemoteHeads;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.GetGitRemoteHeads;

//მოთხოვნილი (ან ყველა) რეპოზიტორიის remote-ის მიმდინარე ვერსია ქეშიდან, სახელით დალაგებული (OrdinalIgnoreCase).
//უცნობი სახელი შედეგში არ ჩანს. SupportTools-ის V3 სინქრონიზაცია ამით ადგენს, რომელ ფოლდერს არ სჭირდება git სერვერთან
//მიმართვა
public sealed class GetGitRemoteHeadsQueryHandler : IQueryHandler<GetGitRemoteHeadsQuery,
    List<StsGitRemoteHeadDataModel>>
{
    private readonly IGitRemoteHeadsCache _gitRemoteHeadsCache;
    private readonly IGitRepoRepository _gitRepoRepository;

    public GetGitRemoteHeadsQueryHandler(IGitRepoRepository gitRepoRepository,
        IGitRemoteHeadsCache gitRemoteHeadsCache)
    {
        _gitRepoRepository = gitRepoRepository;
        _gitRemoteHeadsCache = gitRemoteHeadsCache;
    }

    public async Task<Result<List<StsGitRemoteHeadDataModel>>> Handle(GetGitRemoteHeadsQuery query,
        CancellationToken cancellationToken)
    {
        var gitNames = new HashSet<string>(query.GitNames, StringComparer.OrdinalIgnoreCase);
        List<GitRepo> gitRepos = [.. (await _gitRepoRepository.GetAll(cancellationToken))
            .Where(x => gitNames.Count == 0 || gitNames.Contains(x.Name))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)];
        return await _gitRemoteHeadsCache.GetHeads(gitRepos, cancellationToken);
    }
}
