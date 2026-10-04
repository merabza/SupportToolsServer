using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ReactAppTemplates;

namespace SupportToolsServer.Application.ReactAppTemplates;

internal static class ReactAppTemplateContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordWithNameNotFound, ConcurrencyConflict, RecordIsInUse)
    public const string EntityName = "ReactAppTemplate";

    public static StsReactAppTemplateDataModel ToContractModel(this ReactAppTemplate reactAppTemplate)
    {
        return new StsReactAppTemplateDataModel
        {
            Name = reactAppTemplate.Name, Template = reactAppTemplate.Template, Version = reactAppTemplate.Version
        };
    }
}
