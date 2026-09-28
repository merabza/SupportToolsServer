using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.GitRepos.UpdateGitRepo;

//რეპოზიტორიის დამატება ან განახლება სახელით. GitProjectName-ს ენდპოინტი მისამართიდან ავსებს
public class UpdateGitRepoCommand : ICommand
{
    public UpdateGitRepoCommand(StsGitDataModel gitRepo)
    {
        GitRepo = gitRepo;
    }

    public StsGitDataModel GitRepo { get; }
}
