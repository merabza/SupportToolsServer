using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.GitRepos.DeleteGitRepo;

public class DeleteGitRepoCommand : ICommand
{
    public DeleteGitRepoCommand(string key)
    {
        Key = key;
    }

    public string Key { get; }
}
