using MediatRMessagingAbstractions;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServerApi.CommandRequests;

public sealed class UpdateOneGitRepoRequestCommand : ICommand
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateOneGitRepoRequestCommand(StsGitDataModel newRecord)
    {
        NewRecord = newRecord;
    }

    public StsGitDataModel NewRecord { get; }
}