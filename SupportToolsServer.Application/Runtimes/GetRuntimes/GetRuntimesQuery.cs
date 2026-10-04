using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Runtimes.GetRuntimes;

public sealed class GetRuntimesQuery : IQuery<List<StsRuntimeDataModel>>;
