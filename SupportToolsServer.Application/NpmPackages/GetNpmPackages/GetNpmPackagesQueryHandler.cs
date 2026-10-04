using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.NpmPackages;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.NpmPackages.GetNpmPackages;

public sealed class GetNpmPackagesQueryHandler : IQueryHandler<GetNpmPackagesQuery, List<StsNpmPackageDataModel>>
{
    private readonly INpmPackageRepository _npmPackageRepository;

    public GetNpmPackagesQueryHandler(INpmPackageRepository npmPackageRepository)
    {
        _npmPackageRepository = npmPackageRepository;
    }

    public async Task<Result<List<StsNpmPackageDataModel>>> Handle(GetNpmPackagesQuery query,
        CancellationToken cancellationToken)
    {
        List<NpmPackage> npmPackages = await _npmPackageRepository.GetAll(cancellationToken);

        List<StsNpmPackageDataModel> npmPackageModels =
        [
            .. npmPackages.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x => x.ToContractModel())
        ];
        return npmPackageModels;
    }
}
