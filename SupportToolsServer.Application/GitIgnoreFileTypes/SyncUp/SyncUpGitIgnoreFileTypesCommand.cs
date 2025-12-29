using System.Collections.Generic;
using MediatRMessagingAbstractions;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServer.Application.GitIgnoreFileTypes.SyncUp;

public class SyncUpGitIgnoreFileTypesCommand : ICommand
{
    public bool Merge { get; }
    public readonly List<StsGitIgnoreFileTypeDataModel> UploadGitIgnoreFileTypes;

    public SyncUpGitIgnoreFileTypesCommand(bool merge, List<StsGitIgnoreFileTypeDataModel> uploadGitIgnoreFileTypes)
    {
        Merge = merge;
        UploadGitIgnoreFileTypes = uploadGitIgnoreFileTypes;
    }
}