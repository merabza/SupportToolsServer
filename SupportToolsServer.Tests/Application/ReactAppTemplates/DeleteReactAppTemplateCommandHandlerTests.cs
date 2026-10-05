using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.ReactAppTemplates.DeleteReactAppTemplate;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.ReactAppTemplates;

public sealed class DeleteReactAppTemplateCommandHandlerTests
{
    private readonly ReactAppTemplate _reactAppTemplate =
        TestData.NewReactAppTemplate("ReduxApp", "redux-typescript", 3);

    private readonly Mock<IProjectTemplateRepository> _projectTemplates = new();
    private readonly Mock<IReactAppTemplateRepository> _reactAppTemplates = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public DeleteReactAppTemplateCommandHandlerTests()
    {
        _projectTemplates.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private Task<Result> Handle(string name, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteReactAppTemplateCommandHandler(_reactAppTemplates.Object, _projectTemplates.Object,
            _unitOfWork.Object);
        return handler.Handle(new DeleteReactAppTemplateCommand(name, version), cancellationToken);
    }

    private void GivenStored(string name)
    {
        _reactAppTemplates.Setup(r => r.GetByName(name, It.IsAny<CancellationToken>())).ReturnsAsync(_reactAppTemplate);
    }

    private void VerifyNothingDeleted()
    {
        _reactAppTemplates.Verify(r => r.Delete(It.IsAny<ReactAppTemplate>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _reactAppTemplates.Setup(r => r.GetByName("VueApp", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReactAppTemplate?)null);

        Result result = await Handle("VueApp", 1);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("ReactAppTemplate With Name VueApp Not Found", result.Error.Description);
        VerifyNothingDeleted();
    }

    //A project template without a React template and one with another React template do not use it
    [Fact]
    public async Task Handle_DeletesTheRecord_WhenTheExpectedVersionIsStored()
    {
        GivenStored("REDUXAPP");
        _projectTemplates.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewProjectTemplate("Console"),
            TestData.NewProjectTemplate("ReactApp", TestData.NewReactAppTemplate("TypeScriptApp"))
        ]);
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("REDUXAPP", 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _projectTemplates.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _reactAppTemplates.Verify(r => r.Delete(_reactAppTemplate), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    //The users are named by their type and name, in name order
    [Fact]
    public async Task Handle_ReturnsRecordIsInUseWithTheProjectTemplatesThatUseIt()
    {
        GivenStored("ReduxApp");
        _projectTemplates.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewProjectTemplate("Reactredux", _reactAppTemplate), TestData.NewProjectTemplate("Console"),
            TestData.NewProjectTemplate("admin", _reactAppTemplate)
        ]);

        Result result = await Handle("ReduxApp", 3);

        Assert.Equal("RecordIsInUse", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("ReactAppTemplate ReduxApp Is Used By: ProjectTemplate admin, ProjectTemplate Reactredux",
            result.Error.Description);
        VerifyNothingDeleted();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflict_WhenAnotherVersionIsStored(int version)
    {
        GivenStored("ReduxApp");

        Result result = await Handle("ReduxApp", version);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"ReactAppTemplate ReduxApp Version Conflict: Expected {version}, Actual 3",
            result.Error.Description);
        VerifyNothingDeleted();
    }

    //The manual editors delete without a version
    [Fact]
    public async Task Handle_DeletesTheRecordOfAnyVersion_WithoutAnExpectedVersion()
    {
        GivenStored("ReduxApp");

        Result result = await Handle("ReduxApp", null);

        Assert.True(result.IsSuccess);
        _reactAppTemplates.Verify(r => r.Delete(_reactAppTemplate), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The DELETE is based on the version that was read, also without an expected version
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _reactAppTemplates.SetupSequence(r => r.GetByName("ReduxApp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_reactAppTemplate).ReturnsAsync(TestData.NewReactAppTemplate("ReduxApp", "Changed", 4));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("ReduxApp", null);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ReactAppTemplate ReduxApp Version Conflict: Expected 3, Actual 4", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheRecordIsDeletedBeforeTheSave()
    {
        _reactAppTemplates.SetupSequence(r => r.GetByName("ReduxApp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_reactAppTemplate).ReturnsAsync((ReactAppTemplate?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("ReduxApp", 3);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
