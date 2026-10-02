using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Environments.GetEnvironments;

public sealed class GetEnvironmentsQuery : IQuery<List<StsEnvironmentDataModel>>;
