using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.ReactAppTemplates.UpdateReactAppTemplate;

//upsert ვერსიით (CLAUDE.md, Registry conventions). ReactAppTemplate.Version მოსალოდნელი ვერსიაა,
//ReactAppTemplate.Name-ს კი ენდპოინტი მისამართის key-ით ავსებს. პასუხი ჩანაწერის ახალი ვერსიაა
public sealed class UpdateReactAppTemplateCommand : ICommand<int>
{
    public UpdateReactAppTemplateCommand(StsReactAppTemplateDataModel reactAppTemplate)
    {
        ReactAppTemplate = reactAppTemplate;
    }

    public StsReactAppTemplateDataModel ReactAppTemplate { get; }
}
