using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.GetGitRepoByKey;

public class GetGitRepoByKeyQueryHandler : IQueryHandler<GetGitRepoByKeyQuery, StsGitDataModel>
{
    private readonly IGitIgnoreFileTypeRepository _gitIgnoreFileTypeRepository;
    private readonly IGitRepoRepository _gitRepoRepository;

    public GetGitRepoByKeyQueryHandler(IGitRepoRepository gitRepoRepository,
        IGitIgnoreFileTypeRepository gitIgnoreFileTypeRepository)
    {
        _gitRepoRepository = gitRepoRepository;
        _gitIgnoreFileTypeRepository = gitIgnoreFileTypeRepository;
    }

    public async Task<Result<StsGitDataModel>> Handle(GetGitRepoByKeyQuery query, CancellationToken cancellationToken)
    {
        GitRepo? gitRepo = await _gitRepoRepository.GetByName(query.Key, cancellationToken);
        if (gitRepo is null)
        {
            return SupportToolsServerApiClientErrors.GitWithKeyNotFound(query.Key);
        }

        List<GitIgnoreFileType> gitIgnoreFileTypes = await _gitIgnoreFileTypeRepository.GetAll(cancellationToken);
        GitIgnoreFileType gitIgnoreFileType = gitIgnoreFileTypes.Single(x => x.Id == gitRepo.GitIgnoreFileTypeId);
        return gitRepo.ToContractModel(gitIgnoreFileType.Name);
    }
}
