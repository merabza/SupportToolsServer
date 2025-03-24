using MediatRMessagingAbstractions;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServerApi.CommandRequests;

public class UploadGitReposCommandRequest : ICommand
{
    public required List<GitDataDomain> Gits { get; set; }
    public required List<GitIgnoreFile> GitIgnoreFiles { get; set; }

}