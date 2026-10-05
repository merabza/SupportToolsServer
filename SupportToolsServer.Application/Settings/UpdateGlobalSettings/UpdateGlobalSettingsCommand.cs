using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Settings.UpdateGlobalSettings;

//upsert ვერსიით (CLAUDE.md, Registry conventions). GlobalSettings.Version მოსალოდნელი ვერსიაა: 0 ნიშნავს პირველ
//შექმნას. ჩანაწერი ერთადერთია, ამიტომ გასაღები არ აქვს. პასუხი ჩანაწერის ახალი ვერსიაა
public sealed class UpdateGlobalSettingsCommand : ICommand<int>
{
    public UpdateGlobalSettingsCommand(StsGlobalSettingsDataModel globalSettings)
    {
        GlobalSettings = globalSettings;
    }

    public StsGlobalSettingsDataModel GlobalSettings { get; }
}
