using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.Runtimes;
using SupportToolsServerCore.Domain.Servers;

namespace SupportToolsServer.Application.Servers;

//სერვერის ველების წესები. სიგრძეები ServerConfiguration-ის HasMaxLength-ს ემთხვევა, მითითებული ჩანაწერების სახელებისა
//კი ApiClient-ისა და Runtime-ის სახელების სიგრძეს. მათ არსებობას handler-ი ამოწმებს (404 ReferencedRecordsNotFound).
//გზები სამიზნე სერვერისაა, ამიტომ მხოლოდ სიგრძით მოწმდება.
//Version-ს წესი არ სჭირდება: უარყოფითი ვერსია არც ერთ ჩანაწერს არ ემთხვევა (404 ან 409)
public sealed class ServerModelValidator : AbstractValidator<StsServerDataModel>
{
    public ServerModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(StsServerDataModel.Name), Server.NameMaxLength);

        RuleFor(x => x.WebAgentName).OptionalWithMaxLength(x => ValueName(x, nameof(StsServerDataModel.WebAgentName)),
            ApiClient.NameMaxLength);

        RuleFor(x => x.WebAgentInstallerName).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsServerDataModel.WebAgentInstallerName)), ApiClient.NameMaxLength);

        RuleFor(x => x.FilesUserName).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsServerDataModel.FilesUserName)), Server.FilesUserNameMaxLength);

        RuleFor(x => x.FilesUsersGroupName).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsServerDataModel.FilesUsersGroupName)), Server.FilesUsersGroupNameMaxLength);

        RuleFor(x => x.Runtime).OptionalWithMaxLength(x => ValueName(x, nameof(StsServerDataModel.Runtime)),
            Runtime.NameMaxLength);

        RuleFor(x => x.ServerSideDownloadFolder).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsServerDataModel.ServerSideDownloadFolder)),
            Server.ServerSideDownloadFolderMaxLength);

        RuleFor(x => x.ServerSideDeployFolder).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsServerDataModel.ServerSideDeployFolder)),
            Server.ServerSideDeployFolderMaxLength);
    }

    private static string ValueName(StsServerDataModel server, string propertyName)
    {
        return string.IsNullOrWhiteSpace(server.Name) ? propertyName : $"{server.Name}.{propertyName}";
    }
}
