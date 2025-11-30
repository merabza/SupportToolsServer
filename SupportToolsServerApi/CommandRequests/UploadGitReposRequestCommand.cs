using System.Collections.Generic;
using MediatRMessagingAbstractions;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServerApi.CommandRequests;

public sealed class UploadGitReposRequestCommand : ICommand
{
    public required List<StsGitDataModel> Gits { get; set; }
    public required List<StsGitIgnoreFileTypeDataModel> GitIgnoreFiles { get; set; }

}