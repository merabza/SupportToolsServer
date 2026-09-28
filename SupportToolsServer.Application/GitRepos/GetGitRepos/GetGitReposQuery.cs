using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.GitRepos.GetGitRepos;

public class GetGitReposQuery : IQuery<List<StsGitDataModel>>;
