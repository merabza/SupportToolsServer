using System.Collections.Generic;
using System.Linq;
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
            //FileNameMaxLength = fileStorage.FileNameMaxLength,
            //FileSizeSplitPositionInRow = fileStorage.FileSizeSplitPositionInRow,
            FtpSiteLsFileOffset = fileStorage.FtpSiteLsFileOffset,
            Version = fileStorage.Version
        };
    }

    //სხვა აგრეგატები ფაილსაცავს Id-ით ინახავენ, კონტრაქტში კი მის სახელს გადასცემენ (მაგალითად,
    //GlobalSettings.FileStorageNameForExchange)
    public static Dictionary<FileStorageId, string> ToNamesById(this IEnumerable<FileStorage> fileStorages)
    {
        return fileStorages.ToDictionary(x => x.Id, x => x.Name);
    }
}
