using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.DatabaseServerConnections.DeleteDatabaseServerConnection;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.DatabaseServerConnections;

public sealed class DeleteDatabaseServerConnectionCommandHandlerTests
{
    private readonly DatabaseServerConnection _connection =
        TestData.NewDatabaseServerConnection("Pc1.Sql", foldersSetNames: ["Default"], version: 3);

    private readonly Mock<IDatabaseServerConnectionRepository> _connections = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Task<Result> Handle(string name, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteDatabaseServerConnectionCommandHandler(_connections.Object, _unitOfWork.Object);
        return handler.Handle(new DeleteDatabaseServerConnectionCommand(name, version), cancellationToken);
    }

    private void VerifyNothingDeleted()
    {
        _connections.Verify(r => r.Delete(It.IsAny<DatabaseServerConnection>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _connections.Setup(r => r.GetByName("Pc2.Sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DatabaseServerConnection?)null);

        Result result = await Handle("Pc2.Sql", 1);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("DatabaseServerConnection With Name Pc2.Sql Not Found", result.Error.Description);
        VerifyNothingDeleted();
    }

    [Fact]
    public async Task Handle_DeletesTheRecord_WhenTheExpectedVersionIsStored()
    {
        _connections.Setup(r => r.GetByName("PC1.SQL", It.IsAny<CancellationToken>())).ReturnsAsync(_connection);
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("PC1.SQL", 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _connections.Verify(r => r.Delete(_connection), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflict_WhenAnotherVersionIsStored(int version)
    {
        _connections.Setup(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>())).ReturnsAsync(_connection);

        Result result = await Handle("Pc1.Sql", version);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"DatabaseServerConnection Pc1.Sql Version Conflict: Expected {version}, Actual 3",
            result.Error.Description);
        VerifyNothingDeleted();
    }

    //The manual editors delete without a version
    [Fact]
    public async Task Handle_DeletesTheRecordOfAnyVersion_WithoutAnExpectedVersion()
    {
        _connections.Setup(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>())).ReturnsAsync(_connection);

        Result result = await Handle("Pc1.Sql", null);

        Assert.True(result.IsSuccess);
        _connections.Verify(r => r.Delete(_connection), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The DELETE is based on the version that was read, also without an expected version
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _connections.SetupSequence(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_connection).ReturnsAsync(TestData.NewDatabaseServerConnection("Pc1.Sql", version: 4));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("Pc1.Sql", null);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("DatabaseServerConnection Pc1.Sql Version Conflict: Expected 3, Actual 4",
            result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheRecordIsDeletedBeforeTheSave()
    {
        _connections.SetupSequence(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_connection).ReturnsAsync((DatabaseServerConnection?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("Pc1.Sql", 3);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
