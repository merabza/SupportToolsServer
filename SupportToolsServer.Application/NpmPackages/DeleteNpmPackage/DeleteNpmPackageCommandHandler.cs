using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Projects;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerCore.Domain.Projects;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.NpmPackages.DeleteNpmPackage;

public sealed class DeleteNpmPackageCommandHandler : ICommandHandler<DeleteNpmPackageCommand>
{
    private readonly INpmPackageRepository _npmPackageRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteNpmPackageCommandHandler(INpmPackageRepository npmPackageRepository,
        IProjectRepository projectRepository, IUnitOfWork unitOfWork)
    {
        _npmPackageRepository = npmPackageRepository;
        _projectRepository = projectRepository;
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

        //NpmPackage-ს პროექტები მიმართავს (FK, Restrict), ამიტომ გამოყენებულს არ ვშლით: 409 RecordIsInUse პროექტების
        //სიით
        List<string> usages = [.. (await _projectRepository.GetAll(cancellationToken)).GetUsages(npmPackage.Id)];
        if (usages.Count > 0)
        {
            return SupportToolsServerApiClientErrors.RecordIsInUse(NpmPackageContractMapper.EntityName, command.Name,
                usages);
        }

        _npmPackageRepository.Delete(npmPackage);
        return await RecordVersions.SaveChanges(_unitOfWork, NpmPackageContractMapper.EntityName, command.Name,
            npmPackage.Version, async ct => (await _npmPackageRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }
}
