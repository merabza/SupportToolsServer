using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Servers.GetServers;

public sealed class GetServersQuery : IQuery<List<StsServerDataModel>>;
