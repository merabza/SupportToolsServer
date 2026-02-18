using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.MediatRMessagingAbstractions;

namespace SupportToolsServer.Application.GitIgnoreFileTypes.SyncUp;

public class SyncUpGitIgnoreFileTypesCommand : ICommand
{
    public SyncUpGitIgnoreFileTypesCommand(bool merge, List<StsGitIgnoreFileTypeDataModel> uploadGitIgnoreFileTypes)
    {
        Merge = merge;
        UploadGitIgnoreFileTypes = uploadGitIgnoreFileTypes;
    }

    public List<StsGitIgnoreFileTypeDataModel> UploadGitIgnoreFileTypes { get; }

    public bool Merge { get; }
}
