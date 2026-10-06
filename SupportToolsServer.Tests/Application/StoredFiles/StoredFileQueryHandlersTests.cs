using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.StoredFiles;
using SupportToolsServer.Application.StoredFiles.GetStoredFileByPath;
using SupportToolsServer.Application.StoredFiles.GetStoredFiles;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.StoredFiles;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.StoredFiles;

public sealed class StoredFileQueryHandlersTests
{
    private static readonly DateTime UpdatedAt = new(2026, 10, 7, 9, 30, 15, DateTimeKind.Utc);

    private readonly Mock<IStoredFileRepository> _storedFiles = new();

    [Fact]
    public async Task GetStoredFiles_ReturnsTheMetadataOfEveryFileInPathOrder()
    {
        _storedFiles.Setup(r => r.GetInfos(It.IsAny<CancellationToken>())).ReturnsAsync([
            new StoredFileInfo(@"D:\b.json", "B", 2, UpdatedAt, 1),
            new StoredFileInfo(@"d:\C\x.json", "C", 3, UpdatedAt.AddDays(1), 4),
            new StoredFileInfo(@"D:\a.json", "A", 1, UpdatedAt.AddDays(-1), 2)
        ]);
        using var cancellation = new CancellationTokenSource();

        Result<List<StsStoredFileInfoDataModel>> result =
            await new GetStoredFilesQueryHandler(_storedFiles.Object).Handle(new GetStoredFilesQuery(),
                cancellation.Token);

        Assert.Equal([@"D:\a.json", @"D:\b.json", @"d:\C\x.json"], result.Value.Select(x => x.Path));
        Assert.Equal(["A", "B", "C"], result.Value.Select(x => x.Sha256));
        Assert.Equal([1, 2, 3], result.Value.Select(x => x.Length));
        Assert.Equal([UpdatedAt.AddDays(-1), UpdatedAt, UpdatedAt.AddDays(1)],
            result.Value.Select(x => x.UpdatedAtUtc));
        Assert.Equal([2, 1, 4], result.Value.Select(x => x.Version));
        _storedFiles.Verify(r => r.GetInfos(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetStoredFileByPath_ReturnsTheFileWithItsContentAndVersion()
    {
        _storedFiles.Setup(r => r.GetByPath(@"d:\1worksecurity\a.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewStoredFile(@"D:\1WorkSecurity\a.json", version: 5));
        using var cancellation = new CancellationTokenSource();

        Result<StsStoredFileDataModel> result =
            await new GetStoredFileByPathQueryHandler(_storedFiles.Object).Handle(
                new GetStoredFileByPathQuery(@"d:\1worksecurity\a.json"), cancellation.Token);

        Assert.Equal(@"D:\1WorkSecurity\a.json", result.Value.Path);
        Assert.Equal(TestData.MadeUpFileContent, result.Value.Content);
        Assert.Equal(5, result.Value.Version);
        _storedFiles.Verify(r => r.GetByPath(@"d:\1worksecurity\a.json", cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetStoredFileByPath_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchPath()
    {
        _storedFiles.Setup(r => r.GetByPath(@"D:\x.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoredFile?)null);

        Result<StsStoredFileDataModel> result =
            await new GetStoredFileByPathQueryHandler(_storedFiles.Object).Handle(
                new GetStoredFileByPathQuery(@"D:\x.json"), CancellationToken.None);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal(@"StoredFile With Name D:\x.json Not Found", result.Error.Description);
    }

    [Fact]
    public void ToContractModel_CopiesEveryFieldOfTheMetadata()
    {
        StsStoredFileInfoDataModel model = new StoredFileInfo(@"D:\a.json", "ABC", 7, UpdatedAt, 3).ToContractModel();

        Assert.Equal(@"D:\a.json", model.Path);
        Assert.Equal("ABC", model.Sha256);
        Assert.Equal(7, model.Length);
        Assert.Equal(UpdatedAt, model.UpdatedAtUtc);
        Assert.Equal(3, model.Version);
    }

    [Fact]
    public void ToContractModel_CopiesThePathTheContentAndTheVersionOfTheFile()
    {
        StsStoredFileDataModel model = TestData.NewStoredFile(@"D:\a.json", "{}", 6).ToContractModel();

        Assert.Equal(@"D:\a.json", model.Path);
        Assert.Equal("{}", model.Content);
        Assert.Equal(6, model.Version);
    }
}
