using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ReactAppTemplates;

namespace SupportToolsServer.Application.ReactAppTemplates;

//ReactAppTemplate-ის ველების წესები. სიგრძეები ReactAppTemplateConfiguration-ის HasMaxLength-ს ემთხვევა.
//Version-ს წესი არ სჭირდება: უარყოფითი ვერსია არც ერთ ჩანაწერს არ ემთხვევა (404 ან 409)
public sealed class ReactAppTemplateModelValidator : AbstractValidator<StsReactAppTemplateDataModel>
{
    public ReactAppTemplateModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(StsReactAppTemplateDataModel.Name),
            ReactAppTemplate.NameMaxLength);

        RuleFor(x => x.Template).RequiredWithMaxLength(x => ValueName(x, nameof(StsReactAppTemplateDataModel.Template)),
            ReactAppTemplate.TemplateMaxLength);
    }

    private static string ValueName(StsReactAppTemplateDataModel reactAppTemplate, string propertyName)
    {
        return string.IsNullOrWhiteSpace(reactAppTemplate.Name)
            ? propertyName
            : $"{reactAppTemplate.Name}.{propertyName}";
    }
}
