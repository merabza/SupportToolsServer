using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.ProjectTemplates.UpdateProjectTemplate;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.ProjectTemplates;

public sealed class UpdateProjectTemplateCommandHandlerTests
{
    private readonly Mock<IProjectTemplateRepository> _projectTemplates = new();
    private readonly ReactAppTemplate _reactTemplate = TestData.NewReactAppTemplate("redux-typescript");
    private readonly Mock<IReactAppTemplateRepository> _reactAppTemplates = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateProjectTemplateCommandHandlerTests()
    {
        _reactAppTemplates.Setup(r => r.GetByName("redux-typescript", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_reactTemplate);
        _reactAppTemplates.Setup(r => r.GetByName("vue", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReactAppTemplate?)null);
    }

    private void GivenStored(string name, ProjectTemplate? stored)
    {
        _projectTemplates.Setup(r => r.GetByName(name, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(StsProjectTemplateDataModel model, CancellationToken cancellationToken = default)
    {
        var handler = new UpdateProjectTemplateCommandHandler(_projectTemplates.Object, _reactAppTemplates.Object,
            _unitOfWork.Object);
        return handler.Handle(new UpdateProjectTemplateCommand(model), cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _projectTemplates.Verify(r => r.Add(It.IsAny<ProjectTemplate>()), Times.Never);
        _projectTemplates.Verify(r => r.Update(It.IsAny<ProjectTemplate>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CreatesTheRecordWithItsReactTemplateAndTheFirstVersion()
    {
        GivenStored("Reactredux", null);
        ProjectTemplate? added = null;
        _projectTemplates.Setup(r => r.Add(It.IsAny<ProjectTemplate>())).Callback<ProjectTemplate>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result =
            await Handle(TestData.ProjectTemplateModel("Reactredux", "redux-typescript"), cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal("Reactredux", added.Name);
        Assert.Equal("Api", added.SupportProjectType);
        Assert.Equal("ReactTest", added.TestProjectName);
        Assert.Equal("Rt", added.TestProjectShortName);
        Assert.True(added.UseDatabase);
        Assert.False(added.UseDbPartFolderForDatabaseProjects);
        Assert.True(added.UseMenu);
        Assert.False(added.UseHttps);
        Assert.True(added.UseReact);
        Assert.False(added.UseCarcass);
        Assert.True(added.UseIdentity);
        Assert.False(added.UseReCounter);
        Assert.True(added.UseSignalR);
        Assert.False(added.UseFluentValidation);
        Assert.Equal(_reactTemplate.Id, added.ReactTemplateId);
        Assert.Equal(1, added.Version);
        _reactAppTemplates.Verify(r => r.GetByName("redux-typescript", cancellation.Token), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    //No name is no reference: the React templates are not even read
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Handle_StoresNoReactTemplate_WhenTheBodyNamesNone(string? name)
    {
        GivenStored("Console", null);
        ProjectTemplate? added = null;
        _projectTemplates.Setup(r => r.Add(It.IsAny<ProjectTemplate>())).Callback<ProjectTemplate>(x => added = x);

        Result<int> result = await Handle(TestData.ProjectTemplateModel("Console", name));

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Null(added.ReactTemplateId);
        _reactAppTemplates.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsReferencedRecordsNotFound_WhenTheReactTemplateIsMissing()
    {
        GivenStored("Vue", null);

        Result<int> result = await Handle(TestData.ProjectTemplateModel("Vue", "vue"));

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Referenced ReactAppTemplate Records Not Found: vue", result.Error.Description);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButTheNameExists()
    {
        GivenStored("console", TestData.NewProjectTemplate("Console", version: 2));

        Result<int> result = await Handle(TestData.ProjectTemplateModel("console"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("ProjectTemplate console Version Conflict: Expected 0, Actual 2", result.Error.Description);
        VerifyNothingSaved();
    }

    //The version is checked first: a stale request reads no React template, even a missing one
    [Fact]
    public async Task Handle_ChecksTheVersionBeforeTheReactTemplate()
    {
        GivenStored("Vue", TestData.NewProjectTemplate("Vue", version: 3));

        Result<int> result = await Handle(TestData.ProjectTemplateModel("Vue", "vue", 2));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        _reactAppTemplates.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingSaved();
    }

    //The route key gives the name, so changing its case is an update; the React template may be dropped
    [Fact]
    public async Task Handle_UpdatesTheStoredRecordAndReturnsItsNextVersion()
    {
        ProjectTemplate stored = TestData.NewProjectTemplate("reactredux", _reactTemplate, 3);
        GivenStored("Reactredux", stored);
        StsProjectTemplateDataModel model = TestData.ProjectTemplateModel("Reactredux", version: 3);
        model.SupportProjectType = "Razor";
        model.TestProjectShortName = null;
        model.UseDatabase = false;
        model.UseFluentValidation = true;

        Result<int> result = await Handle(model);

        Assert.Equal(4, result.Value);
        _projectTemplates.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal("Reactredux", stored.Name);
        Assert.Equal("Razor", stored.SupportProjectType);
        Assert.Equal("ReactTest", stored.TestProjectName);
        Assert.Null(stored.TestProjectShortName);
        Assert.False(stored.UseDatabase);
        Assert.True(stored.UseFluentValidation);
        Assert.Null(stored.ReactTemplateId);
        Assert.Equal(4, stored.Version);
        _projectTemplates.Verify(r => r.Add(It.IsAny<ProjectTemplate>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored("Console", TestData.NewProjectTemplate("Console", version: 3));

        Result<int> result = await Handle(TestData.ProjectTemplateModel("Console", version: expectedVersion));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"ProjectTemplate Console Version Conflict: Expected {expectedVersion}, Actual 3",
            result.Error.Description);
        VerifyNothingSaved();
    }

    //The record was deleted on the server after the client last saw it
    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoRecord()
    {
        GivenStored("Console", null);

        Result<int> result = await Handle(TestData.ProjectTemplateModel("Console", version: 2));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("ProjectTemplate With Name Console Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _projectTemplates.SetupSequence(r => r.GetByName("Console", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProjectTemplate("Console"))
            .ReturnsAsync(TestData.NewProjectTemplate("Console", version: 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle(TestData.ProjectTemplateModel("Console", version: 1));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ProjectTemplate Console Version Conflict: Expected 1, Actual 2", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSameNameIsCreatedBeforeTheSave()
    {
        _projectTemplates.SetupSequence(r => r.GetByName("Console", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTemplate?)null).ReturnsAsync(TestData.NewProjectTemplate("Console"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique index"));

        Result<int> result = await Handle(TestData.ProjectTemplateModel("Console"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ProjectTemplate Console Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }

    //A save that fails for another reason (here: the React template was deleted after it was read) keeps its
    //exception. The second read is another instance, as from the database: the handler incremented the version of the
    //first one
    [Fact]
    public async Task Handle_ThrowsTheSaveException_WhenTheVersionStillMatches()
    {
        _projectTemplates.SetupSequence(r => r.GetByName("Reactredux", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProjectTemplate("Reactredux"))
            .ReturnsAsync(TestData.NewProjectTemplate("Reactredux"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("foreign key"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            Handle(TestData.ProjectTemplateModel("Reactredux", "redux-typescript", 1)));
    }
}
