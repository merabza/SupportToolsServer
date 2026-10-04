using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.NpmPackages;

namespace SupportToolsServer.Application.NpmPackages;

internal static class NpmPackageContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordWithNameNotFound, ConcurrencyConflict, RecordIsInUse)
    public const string EntityName = "NpmPackage";

    public static StsNpmPackageDataModel ToContractModel(this NpmPackage npmPackage)
    {
        return new StsNpmPackageDataModel
        {
            Name = npmPackage.Name, Description = npmPackage.Description, Version = npmPackage.Version
        };
    }
}
