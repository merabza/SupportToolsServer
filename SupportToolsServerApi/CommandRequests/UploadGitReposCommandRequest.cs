using System.Collections.Generic;
using MediatRMessagingAbstractions;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServerApi.CommandRequests;

public sealed class UploadGitReposCommandRequest : ICommand
{
    public required Dictionary<string, GitDataDto> Gits { get; set; }
    public required List<GitIgnoreFileDto> GitIgnoreFiles { get; set; }

}