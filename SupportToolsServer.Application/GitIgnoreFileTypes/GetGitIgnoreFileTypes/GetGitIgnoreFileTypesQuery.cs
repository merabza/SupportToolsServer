using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.GitIgnoreFileTypes.GetGitIgnoreFileTypes;

public class GetGitIgnoreFileTypesQuery : IQuery<List<StsGitIgnoreFileTypeDataModel>>;
