using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.MediatRMessagingAbstractions;

namespace SupportToolsServerApi.QueryRequests;

public sealed class GetGitReposRequestQuery : IQuery<List<StsGitDataModel>>;
