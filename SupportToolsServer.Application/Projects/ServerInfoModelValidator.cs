using System.Net;
using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.Servers;

namespace SupportToolsServer.Application.Projects;

//ServerInfo-ს წესები. სიგრძეები ServerInfoConfiguration-ის HasMaxLength-ს ემთხვევა, მითითებული ჩანაწერების სახელებისა
//კი Server-ის, DeploymentEnvironment-ისა და ApiClient-ის სახელების სიგრძეს. მათ არსებობას handler-ი ამოწმებს (404
//ReferencedRecordsNotFound). სერვერი კლიენტის EProjectServerTools-ს არ იცნობს, ამიტომ AllowToolsList მხოლოდ სიგრძით
//მოწმდება. გზები კანონიკური ფორმითაა (README G3) და მხოლოდ სიგრძით მოწმდება. ვალიდატორი public-ია და პარამეტრის
//გარეშე, რადგან AddFluentValidation ყველა public ვალიდატორს DI-ში არეგისტრირებს; ამიტომ შეტყობინებები სიას ასახელებს
//(ServerInfos.<სერვერი>|<გარემო>.ServerSidePort) და არა პროექტს
public sealed class ServerInfoModelValidator : AbstractValidator<StsServerInfoDataModel>
{
    private const string ServerInfosName = nameof(StsProjectDataModel.ServerInfos);

    public ServerInfoModelValidator()
    {
        RuleFor(x => x.ServerName).RequiredWithMaxLength(
            _ => $"{ServerInfosName}.{nameof(StsServerInfoDataModel.ServerName)}", Server.NameMaxLength);

        RuleFor(x => x.EnvironmentName).RequiredWithMaxLength(
            _ => $"{ServerInfosName}.{nameof(StsServerInfoDataModel.EnvironmentName)}",
            DeploymentEnvironment.NameMaxLength);

        RuleFor(x => x.WebAgentNameForCheck).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsServerInfoDataModel.WebAgentNameForCheck)), ApiClient.NameMaxLength);

        //0 ნიშნავს, რომ პორტი არ არის
        RuleFor(x => x.ServerSidePort).InclusiveBetween(IPEndPoint.MinPort, IPEndPoint.MaxPort)
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueOutOfRange)).WithMessage(x =>
                SupportToolsServerApiClientErrors.ValueOutOfRange(
                    ValueName(x, nameof(StsServerInfoDataModel.ServerSidePort)), IPEndPoint.MinPort,
                    IPEndPoint.MaxPort).Description);

        RuleFor(x => x.ApiVersionId).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsServerInfoDataModel.ApiVersionId)), ServerInfo.ApiVersionIdMaxLength);

        RuleFor(x => x.AppSettingsJsonSourceFileName).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsServerInfoDataModel.AppSettingsJsonSourceFileName)), ServerInfo.PathMaxLength);

        RuleFor(x => x.AppSettingsEncodedJsonFileName).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsServerInfoDataModel.AppSettingsEncodedJsonFileName)),
            ServerInfo.PathMaxLength);

        RuleFor(x => x.ServiceUserName).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsServerInfoDataModel.ServiceUserName)), ServerInfo.ServiceUserNameMaxLength);

        //ინსტრუმენტების სია (სიმრავლე): ცარიელი სია დასაშვებია, მაგრამ არა null. ყოველი სახელი შევსებულია, სვეტზე
        //გრძელი არ არის და სიაში ერთხელ გვხვდება, რეგისტრის გარეშე
        RuleFor(x => x.AllowToolsList).NotNull()
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueRequired)).WithMessage(x =>
                SupportToolsServerApiClientErrors.ValueRequired(
                    ValueName(x, nameof(StsServerInfoDataModel.AllowToolsList))).Description);

        RuleForEach(x => x.AllowToolsList).RequiredWithMaxLength(
            x => ValueName(x, nameof(StsServerInfoDataModel.AllowToolsList)), ServerInfoAllowedTool.ToolNameMaxLength);

        RuleFor(x => x.AllowToolsList).Must(x => x is null || UniqueValues.AreUnique(x))
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValuesNotUnique)).WithMessage(x =>
                SupportToolsServerApiClientErrors.ValuesNotUnique(
                    ValueName(x, nameof(StsServerInfoDataModel.AllowToolsList))).Description);

        //null ნიშნავს, რომ ServerInfo-ს ეს პარამეტრები არ აქვს. ველებს ServerInfo-ს გასაღებით ასახელებს
        //(ServerInfos.<სერვერი>|<გარემო>.CurrentDatabaseParameters.DatabaseName)
        RuleFor(x => x.CurrentDatabaseParameters!).SetValidator(x =>
            new DatabaseParametersModelValidator(ValueName(x,
                nameof(StsServerInfoDataModel.CurrentDatabaseParameters))));
        RuleFor(x => x.NewDatabaseParameters!).SetValidator(x =>
            new DatabaseParametersModelValidator(ValueName(x, nameof(StsServerInfoDataModel.NewDatabaseParameters))));
    }

    //ველის სახელი ServerInfo-ს გასაღებით, როცა სერვერისა და გარემოს სახელები შევსებულია
    private static string ValueName(StsServerInfoDataModel serverInfo, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(serverInfo.ServerName) || string.IsNullOrWhiteSpace(serverInfo.EnvironmentName))
        {
            return $"{ServerInfosName}.{propertyName}";
        }

        string key = ServerInfoContractMapper.Key(serverInfo.ServerName, serverInfo.EnvironmentName);
        return $"{ServerInfosName}.{key}.{propertyName}";
    }
}
