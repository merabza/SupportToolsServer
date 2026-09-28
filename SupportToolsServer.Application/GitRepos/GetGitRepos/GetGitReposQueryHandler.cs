using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.GetGitRepos;

public class GetGitReposQueryHandler : IQueryHandler<GetGitReposQuery, List<StsGitDataModel>>
{
    private readonly IGitIgnoreFileTypeRepository _gitIgnoreFileTypeRepository;
    private readonly IGitRepoRepository _gitRepoRepository;

    public GetGitReposQueryHandler(IGitRepoRepository gitRepoRepository,
        IGitIgnoreFileTypeRepository gitIgnoreFileTypeRepository)
    {
        _gitRepoRepository = gitRepoRepository;
        _gitIgnoreFileTypeRepository = gitIgnoreFileTypeRepository;
    }

    public async Task<Result<List<StsGitDataModel>>> Handle(GetGitReposQuery query,
        CancellationToken cancellationToken)
    {
        List<GitRepo> gitRepos = await _gitRepoRepository.GetAll(cancellationToken);
        List<GitIgnoreFileType> gitIgnoreFileTypes = await _gitIgnoreFileTypeRepository.GetAll(cancellationToken);
        Dictionary<GitIgnoreFileTypeId, string> patternNames = gitIgnoreFileTypes.ToDictionary(x => x.Id, x => x.Name);

        List<StsGitDataModel> gitRepoModels =
        [
            .. gitRepos.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.ToContractModel(patternNames[x.GitIgnoreFileTypeId]))
        ];
        return gitRepoModels;
    }
}
