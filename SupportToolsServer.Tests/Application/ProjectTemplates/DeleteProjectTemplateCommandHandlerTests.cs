using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.ProjectTemplates.DeleteProjectTemplate;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.ProjectTemplates;

public sealed class DeleteProjectTemplateCommandHandlerTests
{
    private readonly ProjectTemplate _projectTemplate = TestData.NewProjectTemplate("Console", version: 3);
    private readonly Mock<IProjectTemplateRepository> _projectTemplates = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Task<Result> Handle(string name, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteProjectTemplateCommandHandler(_projectTemplates.Object, _unitOfWork.Object);
        return handler.Handle(new DeleteProjectTemplateCommand(name, version), cancellationToken);
    }

    private void GivenStored(string name)
    {
        _projectTemplates.Setup(r => r.GetByName(name, It.IsAny<CancellationToken>())).ReturnsAsync(_projectTemplate);
    }

    private void VerifyNothingDeleted()
    {
        _projectTemplates.Verify(r => r.Delete(It.IsAny<ProjectTemplate>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _projectTemplates.Setup(r => r.GetByName("Vue", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTemplate?)null);

        Result result = await Handle("Vue", 1);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("ProjectTemplate With Name Vue Not Found", result.Error.Description);
        VerifyNothingDeleted();
    }

    //No other aggregate references a project template, so nothing is checked for usages
    [Fact]
    public async Task Handle_DeletesTheRecord_WhenTheExpectedVersionIsStored()
    {
        GivenStored("CONSOLE");
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("CONSOLE", 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _projectTemplates.Verify(r => r.Delete(_projectTemplate), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflict_WhenAnotherVersionIsStored(int version)
    {
        GivenStored("Console");

        Result result = await Handle("Console", version);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"ProjectTemplate Console Version Conflict: Expected {version}, Actual 3",
            result.Error.Description);
        VerifyNothingDeleted();
    }

    //The manual editors delete without a version
    [Fact]
    public async Task Handle_DeletesTheRecordOfAnyVersion_WithoutAnExpectedVersion()
    {
        GivenStored("Console");

        Result result = await Handle("Console", null);

        Assert.True(result.IsSuccess);
        _projectTemplates.Verify(r => r.Delete(_projectTemplate), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The DELETE is based on the version that was read, also without an expected version
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _projectTemplates.SetupSequence(r => r.GetByName("Console", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_projectTemplate).ReturnsAsync(TestData.NewProjectTemplate("Console", version: 4));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("Console", null);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ProjectTemplate Console Version Conflict: Expected 3, Actual 4", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheRecordIsDeletedBeforeTheSave()
    {
        _projectTemplates.SetupSequence(r => r.GetByName("Console", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_projectTemplate).ReturnsAsync((ProjectTemplate?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("Console", 3);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
