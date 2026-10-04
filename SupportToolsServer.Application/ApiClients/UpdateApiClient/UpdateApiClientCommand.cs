using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.ApiClients.UpdateApiClient;

//upsert ვერსიით (CLAUDE.md, Registry conventions). ApiClient.Version მოსალოდნელი ვერსიაა, ApiClient.Name-ს კი
//ენდპოინტი მისამართის key-ით ავსებს. პასუხი ჩანაწერის ახალი ვერსიაა
public sealed class UpdateApiClientCommand : ICommand<int>
{
    public UpdateApiClientCommand(StsApiClientDataModel apiClient)
    {
        ApiClient = apiClient;
    }

    public StsApiClientDataModel ApiClient { get; }
}
