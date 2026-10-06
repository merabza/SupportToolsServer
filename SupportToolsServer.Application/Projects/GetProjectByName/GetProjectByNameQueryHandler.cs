using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Projects.GetProjectByName;

public sealed class GetProjectByNameQueryHandler : IQueryHandler<GetProjectByNameQuery, StsProjectDataModel>
{
    private readonly IApiClientRepository _apiClientRepository;
    private readonly IDatabaseServerConnectionRepository _databaseServerConnectionRepository;
    private readonly IEditorConfigFileTypeRepository _editorConfigFileTypeRepository;
    private readonly IDeploymentEnvironmentRepository _environmentRepository;
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IGitRepoRepository _gitRepoRepository;
    private readonly INpmPackageRepository _npmPackageRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IServerRepository _serverRepository;
    private readonly ISmartSchemaRepository _smartSchemaRepository;

    public GetProjectByNameQueryHandler(IProjectRepository projectRepository,
        IEditorConfigFileTypeRepository editorConfigFileTypeRepository,
        IDatabaseServerConnectionRepository databaseServerConnectionRepository,
        ISmartSchemaRepository smartSchemaRepository, IFileStorageRepository fileStorageRepository,
        IGitRepoRepository gitRepoRepository, INpmPackageRepository npmPackageRepository,
        IServerRepository serverRepository, IDeploymentEnvironmentRepository environmentRepository,
        IApiClientRepository apiClientRepository)
    {
        _projectRepository = projectRepository;
        _editorConfigFileTypeRepository = editorConfigFileTypeRepository;
        _databaseServerConnectionRepository = databaseServerConnectionRepository;
        _smartSchemaRepository = smartSchemaRepository;
        _fileStorageRepository = fileStorageRepository;
        _gitRepoRepository = gitRepoRepository;
        _npmPackageRepository = npmPackageRepository;
        _serverRepository = serverRepository;
        _environmentRepository = environmentRepository;
        _apiClientRepository = apiClientRepository;
    }

    public async Task<Result<StsProjectDataModel>> Handle(GetProjectByNameQuery query,
        CancellationToken cancellationToken)
    {
        Project? project = await _projectRepository.GetByName(query.Name, cancellationToken);
        if (project is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(ProjectContractMapper.EntityName,
                query.Name);
        }

        return project.ToContractModel(await ProjectReferenceNames.Read(_editorConfigFileTypeRepository,
            _databaseServerConnectionRepository, _smartSchemaRepository, _fileStorageRepository, _gitRepoRepository,
            _npmPackageRepository, _serverRepository, _environmentRepository, _apiClientRepository,
            cancellationToken));
    }
}
