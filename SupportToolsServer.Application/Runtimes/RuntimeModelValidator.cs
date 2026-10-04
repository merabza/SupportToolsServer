using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.Runtimes;

namespace SupportToolsServer.Application.Runtimes;

//Runtime-ის ველების წესები. სიგრძეები RuntimeConfiguration-ის HasMaxLength-ს ემთხვევა.
//Version-ს წესი არ სჭირდება: უარყოფითი ვერსია არც ერთ ჩანაწერს არ ემთხვევა (404 ან 409)
public sealed class RuntimeModelValidator : AbstractValidator<StsRuntimeDataModel>
{
    public RuntimeModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(StsRuntimeDataModel.Name), Runtime.NameMaxLength);

        RuleFor(x => x.Description).OptionalWithMaxLength(x => ValueName(x, nameof(StsRuntimeDataModel.Description)),
            Runtime.DescriptionMaxLength);
    }

    private static string ValueName(StsRuntimeDataModel runtime, string propertyName)
    {
        return string.IsNullOrWhiteSpace(runtime.Name) ? propertyName : $"{runtime.Name}.{propertyName}";
    }
}
