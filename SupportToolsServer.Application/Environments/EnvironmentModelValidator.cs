using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DeploymentEnvironments;

namespace SupportToolsServer.Application.Environments;

//გარემოს ველების წესები. სიგრძეები DeploymentEnvironmentConfiguration-ის HasMaxLength-ს ემთხვევა.
//Version-ს წესი არ სჭირდება: უარყოფითი ვერსია არც ერთ ჩანაწერს არ ემთხვევა (404 ან 409)
public sealed class EnvironmentModelValidator : AbstractValidator<StsEnvironmentDataModel>
{
    public EnvironmentModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(StsEnvironmentDataModel.Name),
            DeploymentEnvironment.NameMaxLength);

        RuleFor(x => x.Description)
            .OptionalWithMaxLength(x => ValueName(x, nameof(StsEnvironmentDataModel.Description)),
                DeploymentEnvironment.DescriptionMaxLength);
    }

    private static string ValueName(StsEnvironmentDataModel environment, string propertyName)
    {
        return string.IsNullOrWhiteSpace(environment.Name) ? propertyName : $"{environment.Name}.{propertyName}";
    }
}
