using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.Projects.DeleteProject;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.Projects;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Projects;

public sealed class DeleteProjectCommandHandlerTests
{
    private readonly Project _project = TestData.NewProject("AppA", version: 3);
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Task<Result> Handle(string name, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteProjectCommandHandler(_projects.Object, _unitOfWork.Object);
        return handler.Handle(new DeleteProjectCommand(name, version), cancellationToken);
    }

    private void VerifyNothingDeleted()
    {
        _projects.Verify(r => r.Delete(It.IsAny<Project>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _projects.Setup(r => r.GetByName("AppZ", It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

        Result result = await Handle("AppZ", 1);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Project With Name AppZ Not Found", result.Error.Description);
        VerifyNothingDeleted();
    }

    //Nothing references a project, so it is deleted without a usage check; its children go with it
    [Fact]
    public async Task Handle_DeletesTheProject_WhenTheExpectedVersionIsStored()
    {
        _projects.Setup(r => r.GetByName("APPA", It.IsAny<CancellationToken>())).ReturnsAsync(_project);
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("APPA", 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _projects.Verify(r => r.Delete(_project), Times.Once);
        _projects.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflict_WhenAnotherVersionIsStored(int version)
    {
        _projects.Setup(r => r.GetByName("AppA", It.IsAny<CancellationToken>())).ReturnsAsync(_project);

        Result result = await Handle("AppA", version);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"Project AppA Version Conflict: Expected {version}, Actual 3", result.Error.Description);
        VerifyNothingDeleted();
    }

    //The manual editors delete without a version
    [Fact]
    public async Task Handle_DeletesTheProjectOfAnyVersion_WithoutAnExpectedVersion()
    {
        _projects.Setup(r => r.GetByName("AppA", It.IsAny<CancellationToken>())).ReturnsAsync(_project);

        Result result = await Handle("AppA", null);

        Assert.True(result.IsSuccess);
        _projects.Verify(r => r.Delete(_project), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The DELETE is based on the version that was read, also without an expected version
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheProjectChangesBeforeTheSave()
    {
        _projects.SetupSequence(r => r.GetByName("AppA", It.IsAny<CancellationToken>())).ReturnsAsync(_project)
            .ReturnsAsync(TestData.NewProject("AppA", version: 4));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("AppA", null);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Project AppA Version Conflict: Expected 3, Actual 4", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheProjectIsDeletedBeforeTheSave()
    {
        _projects.SetupSequence(r => r.GetByName("AppA", It.IsAny<CancellationToken>())).ReturnsAsync(_project)
            .ReturnsAsync((Project?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("AppA", 3);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
