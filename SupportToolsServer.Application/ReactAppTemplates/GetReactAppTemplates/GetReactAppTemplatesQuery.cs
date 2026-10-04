using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.ReactAppTemplates.GetReactAppTemplates;

public sealed class GetReactAppTemplatesQuery : IQuery<List<StsReactAppTemplateDataModel>>;
