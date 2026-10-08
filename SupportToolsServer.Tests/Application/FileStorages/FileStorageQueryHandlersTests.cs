using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.FileStorages;
using SupportToolsServer.Application.FileStorages.GetFileStorageByName;
using SupportToolsServer.Application.FileStorages.GetFileStorages;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.FileStorages;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.FileStorages;

public sealed class FileStorageQueryHandlersTests
{
    private readonly Mock<IFileStorageRepository> _fileStorages = new();

    private Task<Result<StsFileStorageDataModel>> GetByName(string name)
    {
        return new GetFileStorageByNameQueryHandler(_fileStorages.Object).Handle(new GetFileStorageByNameQuery(name),
            CancellationToken.None);
    }

    //The order ignores case, so it differs from the ordinal one
    [Fact]
    public async Task GetFileStorages_ReturnsEveryFieldAndTheVersionInNameOrder()
    {
        _fileStorages.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewFileStorage("LocalBak", @"D:\Bak", null, null),
            TestData.NewFileStorage("exchange", version: 4),
            TestData.NewFileStorage("Archive", "ftp://ftp.example.com/a/", "user-a", "password-a", 2)
        ]);
        using var cancellation = new CancellationTokenSource();

        Result<List<StsFileStorageDataModel>> result =
            await new GetFileStoragesQueryHandler(_fileStorages.Object).Handle(new GetFileStoragesQuery(),
                cancellation.Token);

        Assert.Equal(["Archive", "exchange", "LocalBak"], result.Value.Select(x => x.Name));
        Assert.Equal(["ftp://ftp.example.com/a/", "ftp://ftp.example.com/x/", @"D:\Bak"],
            result.Value.Select(x => x.FileStoragePath));
        Assert.Equal(["user-a", TestData.MadeUpUser, null], result.Value.Select(x => x.UserName));
        Assert.Equal(["password-a", TestData.MadeUpPassword, null], result.Value.Select(x => x.Password));
        Assert.Equal([2, 4, 1], result.Value.Select(x => x.Version));
        _fileStorages.Verify(r => r.GetAll(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetFileStorageByName_ReturnsTheRecordWithItsVersion()
    {
        _fileStorages.Setup(r => r.GetByName("EXCHANGE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewFileStorage("Exchange", version: 5));

        Result<StsFileStorageDataModel> result = await GetByName("EXCHANGE");

        Assert.Equal("Exchange", result.Value.Name);
        Assert.Equal(TestData.MadeUpPassword, result.Value.Password);
        Assert.Equal(5, result.Value.Version);
    }

    [Fact]
    public async Task GetFileStorageByName_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _fileStorages.Setup(r => r.GetByName("LocalBak", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileStorage?)null);

        Result<StsFileStorageDataModel> result = await GetByName("LocalBak");

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("FileStorage With Name LocalBak Not Found", result.Error.Description);
    }

    [Fact]
    public void ToContractModel_CopiesEveryField()
    {
        StsFileStorageDataModel model =
            new FileStorage(FileStorageId.CreateUnique(), "Exchange", "ftp://ftp.example.com/x/", "user-a",
                "password-a", 
                //100, 5, 
                2, 7).ToContractModel();

        Assert.Equal("Exchange", model.Name);
        Assert.Equal("ftp://ftp.example.com/x/", model.FileStoragePath);
        Assert.Equal("user-a", model.UserName);
        Assert.Equal("password-a", model.Password);
        //Assert.Equal(100, model.FileNameMaxLength);
        //Assert.Equal(5, model.FileSizeSplitPositionInRow);
        Assert.Equal(2, model.FtpSiteLsFileOffset);
        Assert.Equal(7, model.Version);
    }
}
