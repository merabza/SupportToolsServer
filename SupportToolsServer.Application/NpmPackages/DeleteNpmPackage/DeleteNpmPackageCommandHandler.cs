using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.NpmPackages;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.NpmPackages.DeleteNpmPackage;

public sealed class DeleteNpmPackageCommandHandler : ICommandHandler<DeleteNpmPackageCommand>
{
    private readonly INpmPackageRepository _npmPackageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteNpmPackageCommandHandler(INpmPackageRepository npmPackageRepository, IUnitOfWork unitOfWork)
    {
        _npmPackageRepository = npmPackageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteNpmPackageCommand command, CancellationToken cancellationToken)
    {
        NpmPackage? npmPackage = await _npmPackageRepository.GetByName(command.Name, cancellationToken);
        if (npmPackage is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(NpmPackageContractMapper.EntityName,
                command.Name);
        }

        if (command.Version is not null && command.Version.Value != npmPackage.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(NpmPackageContractMapper.EntityName,
                command.Name, command.Version.Value, npmPackage.Version);
        }

        //აქ მოწმდება, ხომ არ მიმართავს ჩანაწერს სხვა აგრეგატი (409 RecordIsInUse მომხმარებლების სიით). NpmPackage-ს ჯერ
        //არავინ მიმართავს: B6 (Project-ის ProjectNpmPackages) FK-ს Restrict-ით დაამატებს და პროექტების შემოწმებას აქ
        //ჩასვამს

        _npmPackageRepository.Delete(npmPackage);
        return await RecordVersions.SaveChanges(_unitOfWork, NpmPackageContractMapper.EntityName, command.Name,
            npmPackage.Version, async ct => (await _npmPackageRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }
}
