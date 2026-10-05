using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.Projects;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Projects.DeleteProject;

public sealed class DeleteProjectCommandHandler : ICommandHandler<DeleteProjectCommand>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProjectCommandHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteProjectCommand command, CancellationToken cancellationToken)
    {
        Project? project = await _projectRepository.GetByName(command.Name, cancellationToken);
        if (project is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(ProjectContractMapper.EntityName,
                command.Name);
        }

        if (command.Version is not null && command.Version.Value != project.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(ProjectContractMapper.EntityName,
                command.Name, command.Version.Value, project.Version);
        }

        //პროექტს სხვა აგრეგატი არ მიმართავს, ამიტომ გამოყენების შემოწმება არ სჭირდება. შვილები და ბაზის პარამეტრები
        //პროექტთან ერთად იშლება
        _projectRepository.Delete(project);
        return await RecordVersions.SaveChanges(_unitOfWork, ProjectContractMapper.EntityName, command.Name,
            project.Version, async ct => (await _projectRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }
}
