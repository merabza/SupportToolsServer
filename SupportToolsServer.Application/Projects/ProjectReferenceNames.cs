using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.DatabaseServerConnections;
using SupportToolsServer.Application.EditorConfigFileTypes;
using SupportToolsServer.Application.FileStorages;
using SupportToolsServer.Application.GitRepos;
using SupportToolsServer.Application.NpmPackages;
using SupportToolsServer.Application.SmartSchemas;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerCore.Domain.SmartSchemas;

namespace SupportToolsServer.Application.Projects;

//პროექტის მიერ მითითებული ჩანაწერების სახელები Id-ების მიხედვით. პროექტი მითითებებს Id-ებით ინახავს, კონტრაქტში კი
//სახელებს გადასცემს, ამიტომ GET-ები მითითებული ტიპების ყველა ჩანაწერს ერთხელ კითხულობს
internal sealed class ProjectReferenceNames
{
    public required IReadOnlyDictionary<EditorConfigFileTypeId, string> EditorConfigFileTypes { get; init; }
    public required IReadOnlyDictionary<DatabaseServerConnectionId, string> DatabaseServerConnections { get; init; }
    public required IReadOnlyDictionary<SmartSchemaId, string> SmartSchemas { get; init; }
    public required IReadOnlyDictionary<FileStorageId, string> FileStorages { get; init; }
    public required IReadOnlyDictionary<GitRepoId, string> GitRepos { get; init; }
    public required IReadOnlyDictionary<NpmPackageId, string> NpmPackages { get; init; }

    public static async Task<ProjectReferenceNames> Read(
        IEditorConfigFileTypeRepository editorConfigFileTypeRepository,
        IDatabaseServerConnectionRepository databaseServerConnectionRepository,
        ISmartSchemaRepository smartSchemaRepository, IFileStorageRepository fileStorageRepository,
        IGitRepoRepository gitRepoRepository, INpmPackageRepository npmPackageRepository,
        CancellationToken cancellationToken)
    {
        return new ProjectReferenceNames
        {
            EditorConfigFileTypes = (await editorConfigFileTypeRepository.GetAll(cancellationToken)).ToNamesById(),
            DatabaseServerConnections =
                (await databaseServerConnectionRepository.GetAll(cancellationToken)).ToNamesById(),
            SmartSchemas = (await smartSchemaRepository.GetAll(cancellationToken)).ToNamesById(),
            FileStorages = (await fileStorageRepository.GetAll(cancellationToken)).ToNamesById(),
            GitRepos = (await gitRepoRepository.GetAll(cancellationToken)).ToNamesById(),
            NpmPackages = (await npmPackageRepository.GetAll(cancellationToken)).ToNamesById()
        };
    }
}
