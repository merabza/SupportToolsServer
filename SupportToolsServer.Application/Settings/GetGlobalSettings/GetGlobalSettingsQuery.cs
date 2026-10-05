using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Settings.GetGlobalSettings;

public sealed class GetGlobalSettingsQuery : IQuery<StsGlobalSettingsDataModel>;
