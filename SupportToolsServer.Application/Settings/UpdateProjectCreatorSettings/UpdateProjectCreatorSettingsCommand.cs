using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Settings.UpdateProjectCreatorSettings;

//upsert ვერსიით (CLAUDE.md, Registry conventions). ProjectCreatorSettings.Version მოსალოდნელი ვერსიაა: 0 ნიშნავს
//პირველ შექმნას. ჩანაწერი ერთადერთია, ამიტომ გასაღები არ აქვს. პასუხი ჩანაწერის ახალი ვერსიაა
public sealed class UpdateProjectCreatorSettingsCommand : ICommand<int>
{
    public UpdateProjectCreatorSettingsCommand(StsProjectCreatorSettingsDataModel projectCreatorSettings)
    {
        ProjectCreatorSettings = projectCreatorSettings;
    }

    public StsProjectCreatorSettingsDataModel ProjectCreatorSettings { get; }
}
