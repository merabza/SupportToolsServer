using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.ApiClients;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.DatabaseServerConnections.UpdateDatabaseServerConnection;

public sealed class
    UpdateDatabaseServerConnectionCommandHandler : ICommandHandler<UpdateDatabaseServerConnectionCommand, int>
{
    private readonly IApiClientRepository _apiClientRepository;
    private readonly IDatabaseServerConnectionRepository _databaseServerConnectionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDatabaseServerConnectionCommandHandler(
        IDatabaseServerConnectionRepository databaseServerConnectionRepository,
        IApiClientRepository apiClientRepository, IUnitOfWork unitOfWork)
    {
        _databaseServerConnectionRepository = databaseServerConnectionRepository;
        _apiClientRepository = apiClientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateDatabaseServerConnectionCommand command,
        CancellationToken cancellationToken)
    {
        StsDatabaseServerConnectionDataModel model = command.DatabaseServerConnection;

        //Version 0 — შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია. კავშირი თვალყურის დევნებით იკითხება,
        //რომ შენახვისას ჩანაცვლებული folders set-ები წაიშალოს
        DatabaseServerConnection? stored =
            await _databaseServerConnectionRepository.GetByNameForUpdate(model.Name, cancellationToken);
        Result versionResult = RecordVersions.Check(DatabaseServerConnectionContractMapper.EntityName, model.Name,
            model.Version, stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        //ვებაგენტი სახელით მოდის, ბაზაში კი მისი Id ინახება. ცარიელი სახელი ნიშნავს, რომ ვებაგენტი არ არის
        ApiClientId? dbWebAgentId = null;
        if (!string.IsNullOrWhiteSpace(model.DbWebAgentName))
        {
            ApiClient? dbWebAgent = await _apiClientRepository.GetByName(model.DbWebAgentName, cancellationToken);
            if (dbWebAgent is null)
            {
                return SupportToolsServerApiClientErrors.ReferencedRecordsNotFound(ApiClientContractMapper.EntityName,
                    [model.DbWebAgentName]);
            }

            dbWebAgentId = dbWebAgent.Id;
        }

        List<DatabaseFoldersSet> foldersSets =
        [
            .. model.DatabaseFoldersSets.Select(x => DatabaseFoldersSet.Create(x.Name, x.Backup, x.Data, x.DataLog))
        ];
        DatabaseServerConnection connection;
        if (stored is null)
        {
            connection = DatabaseServerConnection.Create(model.Name, model.DatabaseServerProvider, dbWebAgentId,
                model.RemoteDbConnectionName, model.ServerAddress, model.WindowsNtIntegratedSecurity, model.ServerUser,
                model.ServerPass, model.TrustServerCertificate, model.ConnectionTimeOut, model.Encrypt, foldersSets);
            _databaseServerConnectionRepository.Add(connection);
        }
        else
        {
            stored.Update(model.Name, model.DatabaseServerProvider, dbWebAgentId, model.RemoteDbConnectionName,
                model.ServerAddress, model.WindowsNtIntegratedSecurity, model.ServerUser, model.ServerPass,
                model.TrustServerCertificate, model.ConnectionTimeOut, model.Encrypt, foldersSets);
            _databaseServerConnectionRepository.Update(stored);
            connection = stored;
        }

        //შენახვის ჩავარდნისას ვერსია თავიდან თვალყურის დევნების გარეშე იკითხება, რომ ბაზის მნიშვნელობა მივიღოთ და არა
        //ამ მოთხოვნის მიერ შეცვლილი კავშირისა
        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork,
            DatabaseServerConnectionContractMapper.EntityName, model.Name, model.Version,
            async ct => (await _databaseServerConnectionRepository.GetByName(model.Name, ct))?.Version,
            cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return connection.Version;
    }
}
