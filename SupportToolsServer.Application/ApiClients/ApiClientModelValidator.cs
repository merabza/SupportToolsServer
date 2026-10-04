using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;

namespace SupportToolsServer.Application.ApiClients;

//API კლიენტის ველების წესები. სიგრძეები ApiClientConfiguration-ის HasMaxLength-ს ემთხვევა. შეტყობინებები ველს
//ასახელებს და არასოდეს მის მნიშვნელობას, რადგან API key საიდუმლოა.
//Version-ს წესი არ სჭირდება: უარყოფითი ვერსია არც ერთ ჩანაწერს არ ემთხვევა (404 ან 409)
public sealed class ApiClientModelValidator : AbstractValidator<StsApiClientDataModel>
{
    public ApiClientModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(StsApiClientDataModel.Name), ApiClient.NameMaxLength);

        RuleFor(x => x.Server).OptionalWithMaxLength(x => ValueName(x, nameof(StsApiClientDataModel.Server)),
            ApiClient.ServerMaxLength);

        RuleFor(x => x.ApiKey).OptionalWithMaxLength(x => ValueName(x, nameof(StsApiClientDataModel.ApiKey)),
            ApiClient.ApiKeyMaxLength);
    }

    private static string ValueName(StsApiClientDataModel apiClient, string propertyName)
    {
        return string.IsNullOrWhiteSpace(apiClient.Name) ? propertyName : $"{apiClient.Name}.{propertyName}";
    }
}
