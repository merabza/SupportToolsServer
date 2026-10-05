using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.ProjectTemplates.GetProjectTemplates;

public sealed class GetProjectTemplatesQuery : IQuery<List<StsProjectTemplateDataModel>>;
