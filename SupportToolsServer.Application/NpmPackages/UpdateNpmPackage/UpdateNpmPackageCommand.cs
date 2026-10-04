using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.NpmPackages.UpdateNpmPackage;

//upsert ვერსიით (CLAUDE.md, Registry conventions). NpmPackage.Version მოსალოდნელი ვერსიაა, NpmPackage.Name-ს კი
//ენდპოინტი მისამართის key-ით ავსებს. პასუხი ჩანაწერის ახალი ვერსიაა
public sealed class UpdateNpmPackageCommand : ICommand<int>
{
    public UpdateNpmPackageCommand(StsNpmPackageDataModel npmPackage)
    {
        NpmPackage = npmPackage;
    }

    public StsNpmPackageDataModel NpmPackage { get; }
}
