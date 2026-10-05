using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.ProjectTemplates.UpdateProjectTemplate;

//upsert ვერსიით (CLAUDE.md, Registry conventions). ProjectTemplate.Version მოსალოდნელი ვერსიაა, ProjectTemplate.Name-ს
//კი ენდპოინტი მისამართის key-ით ავსებს. პასუხი ჩანაწერის ახალი ვერსიაა
public sealed class UpdateProjectTemplateCommand : ICommand<int>
{
    public UpdateProjectTemplateCommand(StsProjectTemplateDataModel projectTemplate)
    {
        ProjectTemplate = projectTemplate;
    }

    public StsProjectTemplateDataModel ProjectTemplate { get; }
}
