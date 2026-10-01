using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.EditorConfigFileTypes.DeleteEditorConfigFileType;

public class DeleteEditorConfigFileTypeCommand : ICommand
{
    public DeleteEditorConfigFileTypeCommand(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
