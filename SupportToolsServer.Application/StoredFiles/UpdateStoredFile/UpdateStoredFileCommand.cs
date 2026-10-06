using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.StoredFiles.UpdateStoredFile;

//upsert ვერსიით (CLAUDE.md, Registry conventions). StoredFile.Version მოსალოდნელი ვერსიაა, StoredFile.Path კი
//ჩანაწერის გასაღები (route-ის key არ არის). პასუხი ფაილის ახალი ვერსიაა
public sealed class UpdateStoredFileCommand : ICommand<int>
{
    public UpdateStoredFileCommand(StsStoredFileDataModel storedFile)
    {
        StoredFile = storedFile;
    }

    public StsStoredFileDataModel StoredFile { get; }
}
