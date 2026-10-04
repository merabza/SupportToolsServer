using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.FileStorages;

namespace SupportToolsServer.Application.FileStorages;

//ფაილსაცავის ველების წესები. სიგრძეები FileStorageConfiguration-ის HasMaxLength-ს ემთხვევა. გზა მოწმდება მხოლოდ
//სიგრძით: URL-იცაა და ლოკალური გზაც, და სერვერი მას ინახავს ისე, როგორც მოვიდა. შეტყობინებები ველს ასახელებს და
//არასოდეს მის მნიშვნელობას, რადგან მომხმარებელი და პაროლი საიდუმლოა.
//Version-ს წესი არ სჭირდება: უარყოფითი ვერსია არც ერთ ჩანაწერს არ ემთხვევა (404 ან 409)
public sealed class FileStorageModelValidator : AbstractValidator<StsFileStorageDataModel>
{
    public FileStorageModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(StsFileStorageDataModel.Name),
            FileStorage.NameMaxLength);

        RuleFor(x => x.FileStoragePath)
            .OptionalWithMaxLength(x => ValueName(x, nameof(StsFileStorageDataModel.FileStoragePath)),
                FileStorage.FileStoragePathMaxLength);

        RuleFor(x => x.UserName).OptionalWithMaxLength(x => ValueName(x, nameof(StsFileStorageDataModel.UserName)),
            FileStorage.UserNameMaxLength);

        RuleFor(x => x.Password).OptionalWithMaxLength(x => ValueName(x, nameof(StsFileStorageDataModel.Password)),
            FileStorage.PasswordMaxLength);
    }

    private static string ValueName(StsFileStorageDataModel fileStorage, string propertyName)
    {
        return string.IsNullOrWhiteSpace(fileStorage.Name) ? propertyName : $"{fileStorage.Name}.{propertyName}";
    }
}
