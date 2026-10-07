using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.GitRepos.GetGitProjects;

public sealed class GetGitProjectsQuery : IQuery<List<StsGitProjectDataModel>>;
