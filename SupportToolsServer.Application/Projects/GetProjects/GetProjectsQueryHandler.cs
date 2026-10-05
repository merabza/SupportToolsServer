using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Projects.GetProjects;

//სია სრულ აგრეგატებს აბრუნებს, ბაზის პარამეტრებითა და შვილებით: 61 პროექტისთვის ეს მისაღებია
public sealed class GetProjectsQueryHandler : IQueryHandler<GetProjectsQuery, List<StsProjectDataModel>>
{
    private readonly IDatabaseServerConnectionRepository _databaseServerConnectionRepository;
    private readonly IEditorConfigFileTypeRepository _editorConfigFileTypeRepository;
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IGitRepoRepository _gitRepoRepository;
    private readonly INpmPackageRepository _npmPackageRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ISmartSchemaRepository _smartSchemaRepository;

    public GetProjectsQueryHandler(IProjectRepository projectRepository,
        IEditorConfigFileTypeRepository editorConfigFileTypeRepository,
        IDatabaseServerConnectionRepository databaseServerConnectionRepository,
        ISmartSchemaRepository smartSchemaRepository, IFileStorageRepository fileStorageRepository,
        IGitRepoRepository gitRepoRepository, INpmPackageRepository npmPackageRepository)
    {
        _projectRepository = projectRepository;
        _editorConfigFileTypeRepository = editorConfigFileTypeRepository;
        _databaseServerConnectionRepository = databaseServerConnectionRepository;
        _smartSchemaRepository = smartSchemaRepository;
        _fileStorageRepository = fileStorageRepository;
        _gitRepoRepository = gitRepoRepository;
        _npmPackageRepository = npmPackageRepository;
    }

    public async Task<Result<List<StsProjectDataModel>>> Handle(GetProjectsQuery query,
        CancellationToken cancellationToken)
    {
        List<Project> projects = await _projectRepository.GetAll(cancellationToken);
        ProjectReferenceNames names = await ProjectReferenceNames.Read(_editorConfigFileTypeRepository,
            _databaseServerConnectionRepository, _smartSchemaRepository, _fileStorageRepository, _gitRepoRepository,
            _npmPackageRepository, cancellationToken);

        List<StsProjectDataModel> projectModels =
        [
            .. projects.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x => x.ToContractModel(names))
        ];
        return projectModels;
    }
}
