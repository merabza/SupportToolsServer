using System.Collections.Generic;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SupportToolsServerCore.Domain.ReactAppTemplates;

namespace SupportToolsServer.Application.ProjectTemplates;

internal static class ProjectTemplateContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordWithNameNotFound, ConcurrencyConflict, RecordIsInUse)
    public const string EntityName = "ProjectTemplate";

    //React-ის შაბლონი ბაზაში Id-ით ინახება, კონტრაქტში კი მისი სახელით გადაიცემა
    public static StsProjectTemplateDataModel ToContractModel(this ProjectTemplate projectTemplate,
        IReadOnlyDictionary<ReactAppTemplateId, string> reactAppTemplateNames)
    {
        return new StsProjectTemplateDataModel
        {
            Name = projectTemplate.Name,
            SupportProjectType = projectTemplate.SupportProjectType,
            TestProjectName = projectTemplate.TestProjectName,
            TestProjectShortName = projectTemplate.TestProjectShortName,
            UseDatabase = projectTemplate.UseDatabase,
            UseDbPartFolderForDatabaseProjects = projectTemplate.UseDbPartFolderForDatabaseProjects,
            UseMenu = projectTemplate.UseMenu,
            UseHttps = projectTemplate.UseHttps,
            UseReact = projectTemplate.UseReact,
            UseCarcass = projectTemplate.UseCarcass,
            UseIdentity = projectTemplate.UseIdentity,
            UseReCounter = projectTemplate.UseReCounter,
            UseSignalR = projectTemplate.UseSignalR,
            UseFluentValidation = projectTemplate.UseFluentValidation,
            ReactTemplateName = reactAppTemplateNames.GetName(projectTemplate.ReactTemplateId),
            Version = projectTemplate.Version
        };
    }
}
