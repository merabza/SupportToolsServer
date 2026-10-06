using System.Text;
using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.StoredFiles;

namespace SupportToolsServer.Application.StoredFiles;

//საიდუმლო ფაილის წესები. გზის სიგრძე StoredFileConfiguration-ის HasMaxLength-ს ემთხვევა, ფორმა PathRules-შია.
//შიგთავსი nvarchar(max)-ია და მის ზომას აქ ვზღუდავთ, UTF-8 ბაიტებით, როგორც Sha256 და Length ითვლება.
//შეტყობინებები ველს ასახელებენ, შიგთავსს კი არასოდეს. Version-ს წესი არ სჭირდება: უარყოფითი ვერსია არც ერთ ჩანაწერს
//არ ემთხვევა (404 ან 409)
public sealed class StoredFileModelValidator : AbstractValidator<StsStoredFileDataModel>
{
    public StoredFileModelValidator()
    {
        RuleFor(x => x.Path)
            .RequiredWithMaxLength(_ => nameof(StsStoredFileDataModel.Path), StoredFile.PathMaxLength)
            .ValidAbsoluteFilePath(_ => nameof(StsStoredFileDataModel.Path));

        //შიგთავსი შეიძლება ცარიელი იყოს, მაგრამ არა null
        RuleFor(x => x.Content).NotNull().WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueRequired))
            .WithMessage(x => SupportToolsServerApiClientErrors.ValueRequired(ContentValueName(x)).Description)
            .Must(x => x is null || Encoding.UTF8.GetByteCount(x) <= StsStoredFileDataModel.ContentMaxBytes)
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueTooLarge)).WithMessage(x =>
                SupportToolsServerApiClientErrors
                    .ValueTooLarge(ContentValueName(x), StsStoredFileDataModel.ContentMaxBytes).Description);
    }

    private static string ContentValueName(StsStoredFileDataModel storedFile)
    {
        return string.IsNullOrWhiteSpace(storedFile.Path)
            ? nameof(StsStoredFileDataModel.Content)
            : $"{storedFile.Path}.{nameof(StsStoredFileDataModel.Content)}";
    }
}
