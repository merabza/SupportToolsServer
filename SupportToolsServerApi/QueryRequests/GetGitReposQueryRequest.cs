using System.Collections.Generic;
using MediatRMessagingAbstractions;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServerApi.QueryRequests;

public sealed class GetGitReposQueryRequest : IQuery<List<GitDataDomain>>;