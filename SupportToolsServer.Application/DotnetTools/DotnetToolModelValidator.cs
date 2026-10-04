using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DotnetTools;

namespace SupportToolsServer.Application.DotnetTools;

//DotnetTool-ის ველების წესები. სიგრძეები DotnetToolConfiguration-ის HasMaxLength-ს ემთხვევა.
//Version-ს წესი არ სჭირდება: უარყოფითი ვერსია არც ერთ ჩანაწერს არ ემთხვევა (404 ან 409)
public sealed class DotnetToolModelValidator : AbstractValidator<StsDotnetToolDataModel>
{
    public DotnetToolModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(StsDotnetToolDataModel.Name), DotnetTool.NameMaxLength);

        RuleFor(x => x.PackageId).RequiredWithMaxLength(x => ValueName(x, nameof(StsDotnetToolDataModel.PackageId)),
            DotnetTool.PackageIdMaxLength);

        RuleFor(x => x.MaxVersion).OptionalWithMaxLength(x => ValueName(x, nameof(StsDotnetToolDataModel.MaxVersion)),
            DotnetTool.MaxVersionMaxLength);

        RuleFor(x => x.Description).OptionalWithMaxLength(x => ValueName(x, nameof(StsDotnetToolDataModel.Description)),
            DotnetTool.DescriptionMaxLength);
    }

    private static string ValueName(StsDotnetToolDataModel dotnetTool, string propertyName)
    {
        return string.IsNullOrWhiteSpace(dotnetTool.Name) ? propertyName : $"{dotnetTool.Name}.{propertyName}";
    }
}
