using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.MediatRMessagingAbstractions;

namespace SupportToolsServerApi.CommandRequests;

public sealed class UploadGitReposRequestCommand : ICommand
{
    public required List<StsGitDataModel> Gits { get; set; }
    public required List<StsGitIgnoreFileTypeDataModel> GitIgnoreFiles { get; set; }
}
