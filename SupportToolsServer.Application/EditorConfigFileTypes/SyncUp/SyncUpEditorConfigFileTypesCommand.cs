using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.EditorConfigFileTypes.SyncUp;

public class SyncUpEditorConfigFileTypesCommand : ICommand
{
    public SyncUpEditorConfigFileTypesCommand(bool merge,
        List<StsEditorConfigFileTypeDataModel> uploadEditorConfigFileTypes)
    {
        Merge = merge;
        UploadEditorConfigFileTypes = uploadEditorConfigFileTypes;
    }

    public List<StsEditorConfigFileTypeDataModel> UploadEditorConfigFileTypes { get; }

    public bool Merge { get; }
}
