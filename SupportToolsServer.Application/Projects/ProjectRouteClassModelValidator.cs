using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.Projects;

namespace SupportToolsServer.Application.Projects;

//პროექტის route კლასის წესები. სიგრძეები ProjectRouteClassConfiguration-ის HasMaxLength-ს ემთხვევა. ვალიდატორი
//public-ია და პარამეტრის გარეშე, რადგან AddFluentValidation ყველა public ვალიდატორს DI-ში არეგისტრირებს; ამიტომ
//შეტყობინებები სიას ასახელებს (RouteClasses.<key>.Base) და არა პროექტს
public sealed class ProjectRouteClassModelValidator : AbstractValidator<StsProjectRouteClassDataModel>
{
    private const string RouteClassesName = nameof(StsProjectDataModel.RouteClasses);

    public ProjectRouteClassModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(
            _ => $"{RouteClassesName}.{nameof(StsProjectRouteClassDataModel.Name)}", ProjectRouteClass.NameMaxLength);

        RuleFor(x => x.Root).OptionalWithMaxLength(x => ValueName(x, nameof(StsProjectRouteClassDataModel.Root)),
            ProjectRouteClass.RootMaxLength);

        RuleFor(x => x.ApiVersion).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsProjectRouteClassDataModel.ApiVersion)), ProjectRouteClass.ApiVersionMaxLength);

        RuleFor(x => x.Base).OptionalWithMaxLength(x => ValueName(x, nameof(StsProjectRouteClassDataModel.Base)),
            ProjectRouteClass.BaseMaxLength);
    }

    private static string ValueName(StsProjectRouteClassDataModel routeClass, string propertyName)
    {
        return string.IsNullOrWhiteSpace(routeClass.Name)
            ? $"{RouteClassesName}.{propertyName}"
            : $"{RouteClassesName}.{routeClass.Name}.{propertyName}";
    }
}
