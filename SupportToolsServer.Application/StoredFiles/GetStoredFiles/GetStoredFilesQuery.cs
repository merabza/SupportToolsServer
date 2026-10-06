using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.StoredFiles.GetStoredFiles;

public sealed class GetStoredFilesQuery : IQuery<List<StsStoredFileInfoDataModel>>;
