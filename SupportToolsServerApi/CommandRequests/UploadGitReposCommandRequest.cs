using MediatRMessagingAbstractions;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServerApi.CommandRequests;

public sealed class UploadGitReposCommandRequest : ICommand
{
    public required Dictionary<string, GitDataDomain> Gits { get; set; }
    public required List<GitIgnoreFile> GitIgnoreFiles { get; set; }

}