using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.SmartSchemas.GetSmartSchemas;

public sealed class GetSmartSchemasQuery : IQuery<List<StsSmartSchemaDataModel>>;
