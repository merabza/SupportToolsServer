using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitRepoProjects;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.GetGitProjects;

//სერვერის კლონებიდან გამოთვლილი GitProjects (B9), კლიენტის ფორმით: git-ის სახელით, გზითა და ფაილის სახელით
//დალაგებული (OrdinalIgnoreCase). პროექტის სახელი ორ რეპოზიტორიაში შეიძლება განმეორდეს: სერვერი ორივეს აბრუნებს,
//ერთს კი კლიენტი თავისი წესით ირჩევს
public sealed class GetGitProjectsQueryHandler : IQueryHandler<GetGitProjectsQuery, List<StsGitProjectDataModel>>
{
    private readonly IGitRepoProjectRepository _gitRepoProjectRepository;
    private readonly IGitRepoRepository _gitRepoRepository;

    public GetGitProjectsQueryHandler(IGitRepoProjectRepository gitRepoProjectRepository,
        IGitRepoRepository gitRepoRepository)
    {
        _gitRepoProjectRepository = gitRepoProjectRepository;
        _gitRepoRepository = gitRepoRepository;
    }

    public async Task<Result<List<StsGitProjectDataModel>>> Handle(GetGitProjectsQuery query,
        CancellationToken cancellationToken)
    {
        //რეპოზიტორიები პროექტებამდე იკითხება: შუალედში დამატებული რეპოზიტორიის პროექტები გამოტოვდება, წაშლილის
        //პროექტები კი მასთან ერთად იშლება
        Dictionary<GitRepoId, string> gitNames = (await _gitRepoRepository.GetAll(cancellationToken)).ToNamesById();
        List<GitRepoProject> gitRepoProjects = await _gitRepoProjectRepository.GetAll(cancellationToken);

        List<StsGitProjectDataModel> gitProjectModels =
        [
            .. gitRepoProjects.Where(x => gitNames.ContainsKey(x.GitRepoId))
                .Select(x => x.ToContractModel(gitNames[x.GitRepoId]))
                .OrderBy(x => x.GitName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.ProjectRelativePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.ProjectFileName, StringComparer.OrdinalIgnoreCase)
        ];
        return gitProjectModels;
    }
}
