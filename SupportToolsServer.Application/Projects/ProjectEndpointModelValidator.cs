using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.Projects;

namespace SupportToolsServer.Application.Projects;

//პროექტის endpoint-ის წესები. სიგრძეები ProjectEndpointConfiguration-ის HasMaxLength-ს ემთხვევა. სერვერი კლიენტის
//EHttpMethod-სა და EEndpointType-ს არ იცნობს, ამიტომ ისინი მხოლოდ სიგრძით მოწმდება. ვალიდატორი public-ია და პარამეტრის
//გარეშე, რადგან AddFluentValidation ყველა public ვალიდატორს DI-ში არეგისტრირებს; ამიტომ შეტყობინებები სიას ასახელებს
//(Endpoints.<key>.EndpointRoute) და არა პროექტს
public sealed class ProjectEndpointModelValidator : AbstractValidator<StsProjectEndpointDataModel>
{
    private const string EndpointsName = nameof(StsProjectDataModel.Endpoints);

    public ProjectEndpointModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(
            _ => $"{EndpointsName}.{nameof(StsProjectEndpointDataModel.Name)}", ProjectEndpoint.NameMaxLength);

        RuleFor(x => x.EndpointName).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsProjectEndpointDataModel.EndpointName)),
            ProjectEndpoint.EndpointNameMaxLength);

        RuleFor(x => x.EndpointRoute).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsProjectEndpointDataModel.EndpointRoute)),
            ProjectEndpoint.EndpointRouteMaxLength);

        RuleFor(x => x.HttpMethod).RequiredWithMaxLength(
            x => ValueName(x, nameof(StsProjectEndpointDataModel.HttpMethod)), ProjectEndpoint.HttpMethodMaxLength);

        RuleFor(x => x.EndpointType).RequiredWithMaxLength(
            x => ValueName(x, nameof(StsProjectEndpointDataModel.EndpointType)),
            ProjectEndpoint.EndpointTypeMaxLength);

        RuleFor(x => x.ReturnType).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsProjectEndpointDataModel.ReturnType)), ProjectEndpoint.ReturnTypeMaxLength);
    }

    private static string ValueName(StsProjectEndpointDataModel endpoint, string propertyName)
    {
        return string.IsNullOrWhiteSpace(endpoint.Name)
            ? $"{EndpointsName}.{propertyName}"
            : $"{EndpointsName}.{endpoint.Name}.{propertyName}";
    }
}
