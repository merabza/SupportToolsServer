using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.NpmPackages;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.NpmPackages.GetNpmPackageByName;

public sealed class GetNpmPackageByNameQueryHandler : IQueryHandler<GetNpmPackageByNameQuery, StsNpmPackageDataModel>
{
    private readonly INpmPackageRepository _npmPackageRepository;

    public GetNpmPackageByNameQueryHandler(INpmPackageRepository npmPackageRepository)
    {
        _npmPackageRepository = npmPackageRepository;
    }

    public async Task<Result<StsNpmPackageDataModel>> Handle(GetNpmPackageByNameQuery query,
        CancellationToken cancellationToken)
    {
        NpmPackage? npmPackage = await _npmPackageRepository.GetByName(query.Name, cancellationToken);
        if (npmPackage is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(NpmPackageContractMapper.EntityName,
                query.Name);
        }

        return npmPackage.ToContractModel();
    }
}
