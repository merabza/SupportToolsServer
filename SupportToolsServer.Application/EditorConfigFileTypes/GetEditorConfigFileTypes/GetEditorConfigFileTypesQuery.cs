using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.EditorConfigFileTypes.GetEditorConfigFileTypes;

public class GetEditorConfigFileTypesQuery : IQuery<List<StsEditorConfigFileTypeDataModel>>;
