using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.FileStorages;

namespace SupportToolsServer.Application.FileStorages;

internal static class FileStorageContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordWithNameNotFound, ConcurrencyConflict, RecordIsInUse)
    public const string EntityName = "FileStorage";

    public static StsFileStorageDataModel ToContractModel(this FileStorage fileStorage)
    {
        return new StsFileStorageDataModel
        {
            Name = fileStorage.Name,
            FileStoragePath = fileStorage.FileStoragePath,
            UserName = fileStorage.UserName,
            Password = fileStorage.Password,
            FileNameMaxLength = fileStorage.FileNameMaxLength,
            FileSizeSplitPositionInRow = fileStorage.FileSizeSplitPositionInRow,
            FtpSiteLsFileOffset = fileStorage.FtpSiteLsFileOffset,
            Version = fileStorage.Version
        };
    }
}
