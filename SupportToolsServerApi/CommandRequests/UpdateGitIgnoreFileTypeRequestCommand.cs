using MediatRMessagingAbstractions;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServerApi.CommandRequests;

public sealed class UpdateGitIgnoreFileTypeRequestCommand : ICommand
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateGitIgnoreFileTypeRequestCommand(StsGitIgnoreFileTypeDataModel newRecord)
    {
        NewRecord = newRecord;
    }

    public StsGitIgnoreFileTypeDataModel NewRecord { get; }
}