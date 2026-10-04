using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.FileStorages.UpdateFileStorage;

//upsert ვერსიით (CLAUDE.md, Registry conventions). FileStorage.Version მოსალოდნელი ვერსიაა, FileStorage.Name-ს კი
//ენდპოინტი მისამართის key-ით ავსებს. პასუხი ჩანაწერის ახალი ვერსიაა
public sealed class UpdateFileStorageCommand : ICommand<int>
{
    public UpdateFileStorageCommand(StsFileStorageDataModel fileStorage)
    {
        FileStorage = fileStorage;
    }

    public StsFileStorageDataModel FileStorage { get; }
}
