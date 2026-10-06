using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.ApiClients;
using SupportToolsServer.Application.EditorConfigFileTypes;
using SupportToolsServer.Application.Environments;
using SupportToolsServer.Application.GitRepos;
using SupportToolsServer.Application.NpmPackages;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.Servers;
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
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Projects.UpdateProject;

public sealed class UpdateProjectCommandHandler : ICommandHandler<UpdateProjectCommand, int>
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
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProjectCommandHandler(IProjectRepository projectRepository,
        IEditorConfigFileTypeRepository editorConfigFileTypeRepository,
        IDatabaseServerConnectionRepository databaseServerConnectionRepository,
        ISmartSchemaRepository smartSchemaRepository, IFileStorageRepository fileStorageRepository,
        IGitRepoRepository gitRepoRepository, INpmPackageRepository npmPackageRepository,
        IServerRepository serverRepository, IDeploymentEnvironmentRepository environmentRepository,
        IApiClientRepository apiClientRepository, IUnitOfWork unitOfWork)
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
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateProjectCommand command, CancellationToken cancellationToken)
    {
        StsProjectDataModel model = command.Project;

        //Version 0 — შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია. პროექტი თვალყურის დევნებით იკითხება,
        //რომ შენახვისას ჩანაცვლებული შვილები წაიშალოს
        Project? stored = await _projectRepository.GetByNameForUpdate(model.Name, cancellationToken);
        Result versionResult = RecordVersions.Check(ProjectContractMapper.EntityName, model.Name, model.Version,
            stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        //მითითებები სახელით მოდის, ბაზაში კი მათი Id-ები ინახება. ცარიელი სახელი ნიშნავს, რომ მითითება არ არის. ყველა
        //არარსებული სახელი ერთ შეცდომაში ბრუნდება, ჩანაწერის ტიპებად დაჯგუფებული, კონტრაქტის რიგით
        var references = new ReferencedRecords();
        EditorConfigFileType? editorConfigFileType = await references.Find(model.EditorConfigPatternName,
            EditorConfigFileTypeContractMapper.EntityName, _editorConfigFileTypeRepository.GetByName,
            cancellationToken);
        DatabaseParameters? devDatabaseParameters = await references.FindDatabaseParameters(
            model.DevDatabaseParameters, _databaseServerConnectionRepository, _smartSchemaRepository,
            _fileStorageRepository, cancellationToken);
        DatabaseParameters? prodCopyDatabaseParameters = await references.FindDatabaseParameters(
            model.ProdCopyDatabaseParameters, _databaseServerConnectionRepository, _smartSchemaRepository,
            _fileStorageRepository, cancellationToken);
        List<ProjectGitRepo> gitRepos = await FindGitRepos(model, references, cancellationToken);
        List<ProjectNpmPackage> npmPackages = await FindNpmPackages(model, references, cancellationToken);
        List<ServerInfo> serverInfos = await FindServerInfos(model, references, cancellationToken);
        if (!references.AreAllFound)
        {
            return references.MissingError();
        }

        List<ProjectRedundantFile> redundantFiles = [.. model.RedundantFileNames.Select(ProjectRedundantFile.Create)];
        List<ProjectAllowedTool> allowedTools = [.. model.AllowToolsList.Select(ProjectAllowedTool.Create)];
        List<ProjectEndpoint> endpoints =
        [
            .. model.Endpoints.Select(x => ProjectEndpoint.Create(x.Name, x.EndpointName, x.EndpointRoute,
                x.RequireAuthorization, x.HttpMethod, x.EndpointType, x.ReturnType, x.SendMessageToCurrentUser))
        ];
        List<ProjectRouteClass> routeClasses =
        [
            .. model.RouteClasses.Select(x => ProjectRouteClass.Create(x.Name, x.Root, x.ApiVersion, x.Base))
        ];

        Project project;
        if (stored is null)
        {
            project = Project.Create(model.Name, model.ProjectType, model.ProjectGroupName, model.ProjectDescription,
                model.MajorVersion, model.MinorVersion, model.UseAlternativeWebAgent, editorConfigFileType?.Id,
                model.MainProjectName, model.ApiContractsProjectName, model.SpaProjectName, model.DbContextName,
                model.ProjectShortPrefix, model.ScaffoldSeederProjectName, model.DbContextProjectName,
                model.NewDataSeedingClassLibProjectName, model.ProgramArchiveDateMask, model.ProgramArchiveExtension,
                model.ParametersFileDateMask, model.ParametersFileExtension, model.ProjectFolderName,
                model.SolutionFileName, model.ProjectSecurityFolderPath, model.MigrationStartupProjectFilePath,
                model.MigrationProjectFilePath, model.DataSeederRulesByTableStartupProjectFilePath,
                model.OldDataConvertorForDataSeeder, model.SeedProjectFilePath, model.SeedProjectParametersFilePath,
                model.ExcludesRulesParametersFilePath, model.AppSetEnKeysJsonFileName, model.MigrationSqlFilesFolder,
                model.PrepareProdCopyDatabaseProjectFilePath, model.PrepareProdCopyDatabaseProjectParametersFilePath,
                model.PairedDbObjectsResultFileName, model.KeyGuidPart, devDatabaseParameters,
                prodCopyDatabaseParameters, gitRepos, npmPackages, redundantFiles, allowedTools, endpoints,
                routeClasses, serverInfos);
            _projectRepository.Add(project);
        }
        else
        {
            stored.Update(model.Name, model.ProjectType, model.ProjectGroupName, model.ProjectDescription,
                model.MajorVersion, model.MinorVersion, model.UseAlternativeWebAgent, editorConfigFileType?.Id,
                model.MainProjectName, model.ApiContractsProjectName, model.SpaProjectName, model.DbContextName,
                model.ProjectShortPrefix, model.ScaffoldSeederProjectName, model.DbContextProjectName,
                model.NewDataSeedingClassLibProjectName, model.ProgramArchiveDateMask, model.ProgramArchiveExtension,
                model.ParametersFileDateMask, model.ParametersFileExtension, model.ProjectFolderName,
                model.SolutionFileName, model.ProjectSecurityFolderPath, model.MigrationStartupProjectFilePath,
                model.MigrationProjectFilePath, model.DataSeederRulesByTableStartupProjectFilePath,
                model.OldDataConvertorForDataSeeder, model.SeedProjectFilePath, model.SeedProjectParametersFilePath,
                model.ExcludesRulesParametersFilePath, model.AppSetEnKeysJsonFileName, model.MigrationSqlFilesFolder,
                model.PrepareProdCopyDatabaseProjectFilePath, model.PrepareProdCopyDatabaseProjectParametersFilePath,
                model.PairedDbObjectsResultFileName, model.KeyGuidPart, devDatabaseParameters,
                prodCopyDatabaseParameters, gitRepos, npmPackages, redundantFiles, allowedTools, endpoints,
                routeClasses, serverInfos);
            _projectRepository.Update(stored);
            project = stored;
        }

        //შენახვის ჩავარდნისას ვერსია თავიდან თვალყურის დევნების გარეშე იკითხება, რომ ბაზის მნიშვნელობა მივიღოთ და არა
        //ამ მოთხოვნის მიერ შეცვლილი პროექტისა
        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork, ProjectContractMapper.EntityName,
            model.Name, model.Version, async ct => (await _projectRepository.GetByName(model.Name, ct))?.Version,
            cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return project.Version;
    }

    //git-ები ერთხელ იკითხება (თუ პროექტს ისინი აქვს) და სახელით რეგისტრის გარეშე ედრება: ჯერ პროექტის git-ები, მერე
    //scaffold seeder-ისა
    private async Task<List<ProjectGitRepo>> FindGitRepos(StsProjectDataModel model, ReferencedRecords references,
        CancellationToken cancellationToken)
    {
        List<ProjectGitRepo> gitRepos = [];
        if (model.GitProjectNames.Count == 0 && model.ScaffoldSeederGitProjectNames.Count == 0)
        {
            return gitRepos;
        }

        List<GitRepo> allGitRepos = await _gitRepoRepository.GetAll(cancellationToken);

        foreach ((List<string> names, EProjectGitRepoKind kind) in new[]
                 {
                     (model.GitProjectNames, EProjectGitRepoKind.Main),
                     (model.ScaffoldSeederGitProjectNames, EProjectGitRepoKind.ScaffoldSeed)
                 })
        {
            foreach (string name in names)
            {
                GitRepo? gitRepo = references.Find(name, GitRepoContractMapper.EntityName,
                    x => allGitRepos.Find(y => SameName(y.Name, x)));
                if (gitRepo is not null)
                {
                    gitRepos.Add(ProjectGitRepo.Create(gitRepo.Id, kind));
                }
            }
        }

        return gitRepos;
    }

    //npm პაკეტები ერთხელ იკითხება (თუ პროექტს ისინი აქვს) და სახელით რეგისტრის გარეშე ედრება
    private async Task<List<ProjectNpmPackage>> FindNpmPackages(StsProjectDataModel model,
        ReferencedRecords references, CancellationToken cancellationToken)
    {
        List<ProjectNpmPackage> npmPackages = [];
        if (model.FrontNpmPackageNames.Count == 0)
        {
            return npmPackages;
        }

        List<NpmPackage> allNpmPackages = await _npmPackageRepository.GetAll(cancellationToken);

        foreach (string name in model.FrontNpmPackageNames)
        {
            NpmPackage? npmPackage = references.Find(name, NpmPackageContractMapper.EntityName,
                x => allNpmPackages.Find(y => SameName(y.Name, x)));
            if (npmPackage is not null)
            {
                npmPackages.Add(ProjectNpmPackage.Create(npmPackage.Id));
            }
        }

        return npmPackages;
    }

    //ServerInfo-ების სერვერები, გარემოები და ვებაგენტები ერთხელ იკითხება (თუ პროექტს ServerInfo-ები აქვს) და სახელით
    //რეგისტრის გარეშე ედრება. ბაზის პარამეტრების მითითებები პროექტის ბაზის პარამეტრებივით იძებნება. ServerInfo
    //იქმნება მხოლოდ მაშინ, როცა მისი სერვერიც და გარემოც ნაპოვნია; სხვა შემთხვევაში handler-ი შეცდომას აბრუნებს
    private async Task<List<ServerInfo>> FindServerInfos(StsProjectDataModel model, ReferencedRecords references,
        CancellationToken cancellationToken)
    {
        List<ServerInfo> serverInfos = [];
        if (model.ServerInfos.Count == 0)
        {
            return serverInfos;
        }

        List<Server> allServers = await _serverRepository.GetAll(cancellationToken);
        List<DeploymentEnvironment> allEnvironments = await _environmentRepository.GetAll(cancellationToken);
        List<ApiClient> allApiClients = await _apiClientRepository.GetAll(cancellationToken);

        foreach (StsServerInfoDataModel serverInfoModel in model.ServerInfos)
        {
            Server? server = references.Find(serverInfoModel.ServerName, ServerContractMapper.EntityName,
                x => allServers.Find(y => SameName(y.Name, x)));
            DeploymentEnvironment? environment = references.Find(serverInfoModel.EnvironmentName,
                EnvironmentContractMapper.EntityName, x => allEnvironments.Find(y => SameName(y.Name, x)));
            ApiClient? webAgentForCheck = references.Find(serverInfoModel.WebAgentNameForCheck,
                ApiClientContractMapper.EntityName, x => allApiClients.Find(y => SameName(y.Name, x)));
            DatabaseParameters? currentDatabaseParameters = await references.FindDatabaseParameters(
                serverInfoModel.CurrentDatabaseParameters, _databaseServerConnectionRepository,
                _smartSchemaRepository, _fileStorageRepository, cancellationToken);
            DatabaseParameters? newDatabaseParameters = await references.FindDatabaseParameters(
                serverInfoModel.NewDatabaseParameters, _databaseServerConnectionRepository, _smartSchemaRepository,
                _fileStorageRepository, cancellationToken);
            if (server is null || environment is null)
            {
                continue;
            }

            serverInfos.Add(ServerInfo.Create(server.Id, environment.Id, webAgentForCheck?.Id,
                serverInfoModel.ServerSidePort, serverInfoModel.ApiVersionId,
                serverInfoModel.AppSettingsJsonSourceFileName, serverInfoModel.AppSettingsEncodedJsonFileName,
                serverInfoModel.ServiceUserName, currentDatabaseParameters, newDatabaseParameters,
                serverInfoModel.AllowToolsList.Select(ServerInfoAllowedTool.Create)));
        }

        return serverInfos;
    }

    private static bool SameName(string name, string otherName)
    {
        return string.Equals(name, otherName, StringComparison.OrdinalIgnoreCase);
    }
}
