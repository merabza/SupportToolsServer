using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.GitRepos.UploadGitRepos;

public class UploadGitReposCommand : ICommand
{
    public UploadGitReposCommand(List<StsGitDataModel> gits, List<StsGitIgnoreFileTypeDataModel> gitIgnoreFiles)
    {
        Gits = gits;
        GitIgnoreFiles = gitIgnoreFiles;
    }

    public List<StsGitDataModel> Gits { get; }

    public List<StsGitIgnoreFileTypeDataModel> GitIgnoreFiles { get; }
}
