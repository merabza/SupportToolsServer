using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.DatabaseServerConnections;
using SupportToolsServer.Application.FileStorages;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.SmartSchemas;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.SmartSchemas;

namespace SupportToolsServer.Application.Projects;

//ბაზის პარამეტრები (პროექტის Dev და ProdCopy, B7-ში ServerInfo-ს Current და New): ბაზის კავშირი, ჭკვიანი სქემა და
//ფაილსაცავი ბაზაში Id-ებით ინახება, კონტრაქტში კი სახელებით გადაიცემა
internal static class DatabaseParametersContractMapper
{
    public static StsDatabaseParametersDataModel ToContractModel(this DatabaseParameters parameters,
        IReadOnlyDictionary<DatabaseServerConnectionId, string> connectionNames,
        IReadOnlyDictionary<SmartSchemaId, string> smartSchemaNames,
        IReadOnlyDictionary<FileStorageId, string> fileStorageNames)
    {
        return new StsDatabaseParametersDataModel
        {
            DbConnectionName = connectionNames.GetName(parameters.DbConnectionId),
            DatabaseRecoveryModel = parameters.DatabaseRecoveryModel,
            DbServerFoldersSetName = parameters.DbServerFoldersSetName,
            DatabaseName = parameters.DatabaseName,
            SmartSchemaName = smartSchemaNames.GetName(parameters.SmartSchemaId),
            FileStorageName = fileStorageNames.GetName(parameters.FileStorageId),
            CommandTimeOut = parameters.CommandTimeOut,
            SkipBackupBeforeRestore = parameters.SkipBackupBeforeRestore,
            BackupNamePrefix = parameters.BackupNamePrefix,
            DateMask = parameters.DateMask,
            BackupFileExtension = parameters.BackupFileExtension,
            BackupNameMiddlePart = parameters.BackupNameMiddlePart,
            Compress = parameters.Compress,
            Verify = parameters.Verify,
            BackupType = parameters.BackupType
        };
    }

    //კონტრაქტის პარამეტრები დომენისად: მითითებები სახელით იძებნება და არარსებული სახელები references-ს ემატება (CLAUDE.md,
    //Registry conventions). null ნაწილი null-ად რჩება
    public static async Task<DatabaseParameters?> FindDatabaseParameters(this ReferencedRecords references,
        StsDatabaseParametersDataModel? model, IDatabaseServerConnectionRepository databaseServerConnectionRepository,
        ISmartSchemaRepository smartSchemaRepository, IFileStorageRepository fileStorageRepository,
        CancellationToken cancellationToken)
    {
        if (model is null)
        {
            return null;
        }

        DatabaseServerConnection? connection = await references.Find(model.DbConnectionName,
            DatabaseServerConnectionContractMapper.EntityName, databaseServerConnectionRepository.GetByName,
            cancellationToken);
        SmartSchema? smartSchema = await references.Find(model.SmartSchemaName, SmartSchemaContractMapper.EntityName,
            smartSchemaRepository.GetByName, cancellationToken);
        FileStorage? fileStorage = await references.Find(model.FileStorageName, FileStorageContractMapper.EntityName,
            fileStorageRepository.GetByName, cancellationToken);

        return new DatabaseParameters(connection?.Id, model.DatabaseRecoveryModel, model.DbServerFoldersSetName,
            model.DatabaseName, smartSchema?.Id, fileStorage?.Id, model.CommandTimeOut, model.SkipBackupBeforeRestore,
            model.BackupNamePrefix, model.DateMask, model.BackupFileExtension, model.BackupNameMiddlePart,
            model.Compress, model.Verify, model.BackupType);
    }

    //ველები, რომლებიც მითითებულ ჩანაწერს მიმართავს: null ნაწილი არაფერს მიმართავს
    public static bool Uses(this DatabaseParameters? parameters, DatabaseServerConnectionId connectionId)
    {
        return parameters is not null && connectionId.Equals(parameters.DbConnectionId);
    }

    public static bool Uses(this DatabaseParameters? parameters, SmartSchemaId smartSchemaId)
    {
        return parameters is not null && smartSchemaId.Equals(parameters.SmartSchemaId);
    }

    public static bool Uses(this DatabaseParameters? parameters, FileStorageId fileStorageId)
    {
        return parameters is not null && fileStorageId.Equals(parameters.FileStorageId);
    }
}
