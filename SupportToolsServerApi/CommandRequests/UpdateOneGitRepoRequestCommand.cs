using SupportToolsServerApiContracts.Models;
using SystemTools.MediatRMessagingAbstractions;

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
