using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.ApiClients.UpdateApiClient;

public sealed class UpdateApiClientCommandHandler : ICommandHandler<UpdateApiClientCommand, int>
{
    private readonly IApiClientRepository _apiClientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateApiClientCommandHandler(IApiClientRepository apiClientRepository, IUnitOfWork unitOfWork)
    {
        _apiClientRepository = apiClientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateApiClientCommand command, CancellationToken cancellationToken)
    {
        StsApiClientDataModel model = command.ApiClient;

        //Version 0 — შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია
        ApiClient? stored = await _apiClientRepository.GetByName(model.Name, cancellationToken);
        Result versionResult = RecordVersions.Check(ApiClientContractMapper.EntityName, model.Name, model.Version,
            stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        ApiClient apiClient;
        if (stored is null)
        {
            apiClient = ApiClient.Create(model.Name, model.Server, model.ApiKey);
            _apiClientRepository.Add(apiClient);
        }
        else
        {
            stored.Update(model.Name, model.Server, model.ApiKey);
            _apiClientRepository.Update(stored);
            apiClient = stored;
        }

        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork, ApiClientContractMapper.EntityName,
            model.Name, model.Version, async ct => (await _apiClientRepository.GetByName(model.Name, ct))?.Version,
            cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return apiClient.Version;
    }
}
