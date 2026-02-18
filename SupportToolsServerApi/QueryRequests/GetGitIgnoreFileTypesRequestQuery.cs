using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.MediatRMessagingAbstractions;

namespace SupportToolsServerApi.QueryRequests;

public sealed class GetGitIgnoreFileTypesRequestQuery : IQuery<List<StsGitIgnoreFileTypeDataModel>>;
