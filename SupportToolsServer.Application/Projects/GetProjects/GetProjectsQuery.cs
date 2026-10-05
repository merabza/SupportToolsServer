using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Projects.GetProjects;

public sealed class GetProjectsQuery : IQuery<List<StsProjectDataModel>>;
