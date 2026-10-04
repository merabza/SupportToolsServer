using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.ApiClients.GetApiClients;

public sealed class GetApiClientsQuery : IQuery<List<StsApiClientDataModel>>;
