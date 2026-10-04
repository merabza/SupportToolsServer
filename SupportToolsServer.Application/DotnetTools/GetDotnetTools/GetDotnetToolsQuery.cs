using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.DotnetTools.GetDotnetTools;

public sealed class GetDotnetToolsQuery : IQuery<List<StsDotnetToolDataModel>>;
