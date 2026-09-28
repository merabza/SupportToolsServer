using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.GitIgnoreFileTypes.DeleteGitIgnoreFileType;

public class DeleteGitIgnoreFileTypeCommand : ICommand
{
    public DeleteGitIgnoreFileTypeCommand(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
