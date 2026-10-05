using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SupportToolsServerCore.Domain.ReactAppTemplates;

namespace SupportToolsServer.Application.ProjectTemplates;

//პროექტის შაბლონის ველების წესები. სიგრძეები ProjectTemplateConfiguration-ის HasMaxLength-ს ემთხვევა, React-ის
//შაბლონის სახელისა კი ReactAppTemplate-ის სახელის სიგრძეს; მის არსებობას handler-ი ამოწმებს (404
//ReferencedRecordsNotFound). სერვერი კლიენტის ESupportProjectType-ს არ იცნობს, ამიტომ პროექტის ტიპი მხოლოდ სიგრძით
//მოწმდება. Version-ს წესი არ სჭირდება: უარყოფითი ვერსია არც ერთ ჩანაწერს არ ემთხვევა (404 ან 409)
public sealed class ProjectTemplateModelValidator : AbstractValidator<StsProjectTemplateDataModel>
{
    public ProjectTemplateModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(StsProjectTemplateDataModel.Name),
            ProjectTemplate.NameMaxLength);

        RuleFor(x => x.SupportProjectType).RequiredWithMaxLength(
            x => ValueName(x, nameof(StsProjectTemplateDataModel.SupportProjectType)),
            ProjectTemplate.SupportProjectTypeMaxLength);

        RuleFor(x => x.TestProjectName).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsProjectTemplateDataModel.TestProjectName)),
            ProjectTemplate.TestProjectNameMaxLength);

        RuleFor(x => x.TestProjectShortName).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsProjectTemplateDataModel.TestProjectShortName)),
            ProjectTemplate.TestProjectShortNameMaxLength);

        RuleFor(x => x.ReactTemplateName).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsProjectTemplateDataModel.ReactTemplateName)), ReactAppTemplate.NameMaxLength);
    }

    private static string ValueName(StsProjectTemplateDataModel projectTemplate, string propertyName)
    {
        return string.IsNullOrWhiteSpace(projectTemplate.Name)
            ? propertyName
            : $"{projectTemplate.Name}.{propertyName}";
    }
}
