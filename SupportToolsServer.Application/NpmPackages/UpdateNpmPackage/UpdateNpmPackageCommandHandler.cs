using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.NpmPackages;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.NpmPackages.UpdateNpmPackage;

public sealed class UpdateNpmPackageCommandHandler : ICommandHandler<UpdateNpmPackageCommand, int>
{
    private readonly INpmPackageRepository _npmPackageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateNpmPackageCommandHandler(INpmPackageRepository npmPackageRepository, IUnitOfWork unitOfWork)
    {
        _npmPackageRepository = npmPackageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateNpmPackageCommand command, CancellationToken cancellationToken)
    {
        StsNpmPackageDataModel model = command.NpmPackage;

        //Version 0 — შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია
        NpmPackage? stored = await _npmPackageRepository.GetByName(model.Name, cancellationToken);
        Result versionResult = RecordVersions.Check(NpmPackageContractMapper.EntityName, model.Name, model.Version,
            stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        NpmPackage npmPackage;
        if (stored is null)
        {
            npmPackage = NpmPackage.Create(model.Name, model.Description);
            _npmPackageRepository.Add(npmPackage);
        }
        else
        {
            stored.Update(model.Name, model.Description);
            _npmPackageRepository.Update(stored);
            npmPackage = stored;
        }

        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork, NpmPackageContractMapper.EntityName,
            model.Name, model.Version, async ct => (await _npmPackageRepository.GetByName(model.Name, ct))?.Version,
            cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return npmPackage.Version;
    }
}
