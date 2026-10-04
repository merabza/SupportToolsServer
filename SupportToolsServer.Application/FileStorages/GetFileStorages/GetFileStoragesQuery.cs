using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.FileStorages.GetFileStorages;

public sealed class GetFileStoragesQuery : IQuery<List<StsFileStorageDataModel>>;
