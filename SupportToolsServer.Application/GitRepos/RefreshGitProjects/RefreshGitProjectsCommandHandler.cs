using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.RefreshGitProjects;

//თითო რეპოზიტორიის განახლება რიგში დგება, სახელის მიხედვით. რიგის ერთადერთი მკითხველი მათ თითო-თითოდ ასრულებს
public sealed class RefreshGitProjectsCommandHandler : ICommandHandler<RefreshGitProjectsCommand>
{
    private readonly IGitProjectUpdateQueue _gitProjectUpdateQueue;
    private readonly IGitRepoRepository _gitRepoRepository;

    public RefreshGitProjectsCommandHandler(IGitRepoRepository gitRepoRepository,
        IGitProjectUpdateQueue gitProjectUpdateQueue)
    {
        _gitRepoRepository = gitRepoRepository;
        _gitProjectUpdateQueue = gitProjectUpdateQueue;
    }

    public async Task<Result> Handle(RefreshGitProjectsCommand command, CancellationToken cancellationToken)
    {
        List<GitRepo> gitRepos = await _gitRepoRepository.GetAll(cancellationToken);
        foreach (GitRepo gitRepo in gitRepos.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            await _gitProjectUpdateQueue.Enqueue(
                new UpdateGitProjectCommand(gitRepo.Id, gitRepo.Name, gitRepo.Address, gitRepo.FolderName),
                cancellationToken);
        }

        return Result.Success();
    }
}
