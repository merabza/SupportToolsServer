using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.NpmPackages;

namespace SupportToolsServer.Application.NpmPackages;

//NpmPackage-ის ველების წესები. სიგრძეები NpmPackageConfiguration-ის HasMaxLength-ს ემთხვევა.
//Version-ს წესი არ სჭირდება: უარყოფითი ვერსია არც ერთ ჩანაწერს არ ემთხვევა (404 ან 409)
public sealed class NpmPackageModelValidator : AbstractValidator<StsNpmPackageDataModel>
{
    public NpmPackageModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(StsNpmPackageDataModel.Name), NpmPackage.NameMaxLength);

        RuleFor(x => x.Description).OptionalWithMaxLength(x => ValueName(x, nameof(StsNpmPackageDataModel.Description)),
            NpmPackage.DescriptionMaxLength);
    }

    private static string ValueName(StsNpmPackageDataModel npmPackage, string propertyName)
    {
        return string.IsNullOrWhiteSpace(npmPackage.Name) ? propertyName : $"{npmPackage.Name}.{propertyName}";
    }
}
