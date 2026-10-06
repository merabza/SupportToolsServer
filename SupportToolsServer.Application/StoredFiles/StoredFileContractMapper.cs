using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.StoredFiles;

namespace SupportToolsServer.Application.StoredFiles;

internal static class StoredFileContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordWithNameNotFound, ConcurrencyConflict). ჩანაწერის "სახელი" მისი
    //გზაა
    public const string EntityName = "StoredFile";

    public static StsStoredFileInfoDataModel ToContractModel(this StoredFileInfo storedFileInfo)
    {
        return new StsStoredFileInfoDataModel
        {
            Path = storedFileInfo.Path,
            Sha256 = storedFileInfo.Sha256,
            Length = storedFileInfo.Length,
            UpdatedAtUtc = storedFileInfo.UpdatedAtUtc,
            Version = storedFileInfo.Version
        };
    }

    public static StsStoredFileDataModel ToContractModel(this StoredFile storedFile)
    {
        return new StsStoredFileDataModel
        {
            Path = storedFile.Path, Content = storedFile.Content, Version = storedFile.Version
        };
    }
}
