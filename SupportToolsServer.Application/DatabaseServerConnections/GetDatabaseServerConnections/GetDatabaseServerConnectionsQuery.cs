using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.DatabaseServerConnections.GetDatabaseServerConnections;

public sealed class GetDatabaseServerConnectionsQuery : IQuery<List<StsDatabaseServerConnectionDataModel>>;
