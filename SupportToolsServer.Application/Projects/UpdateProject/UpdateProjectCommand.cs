using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Projects.UpdateProject;

//upsert ვერსიით (CLAUDE.md, Registry conventions). Project.Version მოსალოდნელი ვერსიაა, Project.Name-ს კი ენდპოინტი
//მისამართის key-ით ავსებს. განახლება მთელ აგრეგატს ანაცვლებს, ბაზის პარამეტრებისა და შვილების ჩათვლით. პასუხი
//ჩანაწერის ახალი ვერსიაა
public sealed class UpdateProjectCommand : ICommand<int>
{
    public UpdateProjectCommand(StsProjectDataModel project)
    {
        Project = project;
    }

    public StsProjectDataModel Project { get; }
}
