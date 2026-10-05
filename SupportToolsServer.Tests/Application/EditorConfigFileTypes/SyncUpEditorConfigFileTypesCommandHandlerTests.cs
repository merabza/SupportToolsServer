using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.EditorConfigFileTypes.SyncUp;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.Projects;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.EditorConfigFileTypes;

public sealed class SyncUpEditorConfigFileTypesCommandHandlerTests
{
    private readonly List<EditorConfigFileType> _added = [];
    private readonly EditorConfigFileType _baGetter = TestData.NewEditorConfigFileType("BaGetter");
    private readonly EditorConfigFileType _default = TestData.NewEditorConfigFileType("default", "old", 6);
    private readonly List<EditorConfigFileType> _deleted = [];
    private readonly Mock<IEditorConfigFileTypeRepository> _editorConfigFileTypes = new();
    private readonly List<Project> _projectList = [];
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly List<EditorConfigFileType> _updated = [];

    public SyncUpEditorConfigFileTypesCommandHandlerTests()
    {
        _projects.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(() => _projectList);
        _editorConfigFileTypes.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => [_default, _baGetter]);
        _editorConfigFileTypes.Setup(r => r.Add(It.IsAny<EditorConfigFileType>()))
            .Callback<EditorConfigFileType>(_added.Add);
        _editorConfigFileTypes.Setup(r => r.Update(It.IsAny<EditorConfigFileType>()))
            .Callback<EditorConfigFileType>(_updated.Add);
        _editorConfigFileTypes.Setup(r => r.Delete(It.IsAny<EditorConfigFileType>()))
            .Callback<EditorConfigFileType>(_deleted.Add);
    }

    private Task<Result> Handle(bool merge, params StsEditorConfigFileTypeDataModel[] uploaded)
    {
        var handler = new SyncUpEditorConfigFileTypesCommandHandler(_editorConfigFileTypes.Object, _projects.Object,
            _unitOfWork.Object);
        return handler.Handle(new SyncUpEditorConfigFileTypesCommand(merge, [.. uploaded]), CancellationToken.None);
    }

    //Without merge the types missing from the list would be deleted, so none of them may be in use. Every type in use
    //is named in one error, in name order, with its projects
    [Fact]
    public async Task Handle_WithoutMerge_ReturnsRecordIsInUseAndChangesNothing_WhenAMissingTypeIsInUse()
    {
        _projectList.AddRange(TestData.NewProject("AppB", _default), TestData.NewProject("AppA", _baGetter),
            TestData.NewProject("AppC", _default));

        Result result = await Handle(false);

        Assert.Equal("RecordIsInUse", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("EditorConfigFileType BaGetter Is Used By: Project AppA; " +
                     "EditorConfigFileType default Is Used By: Project AppB, Project AppC", result.Error.Description);
        Assert.Empty(_deleted);
        Assert.Empty(_updated);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    //A type in use that stays in the list is not deleted
    [Fact]
    public async Task Handle_WithoutMerge_DeletesTheUnusedTypes_WhenTheTypesInUseAreInTheList()
    {
        _projectList.Add(TestData.NewProject("AppA", _default));

        Result result = await Handle(false, TestData.EditorConfigModel("DEFAULT"));

        Assert.True(result.IsSuccess);
        Assert.Equal(_baGetter.Id, Assert.Single(_deleted).Id);
    }

    //With merge nothing is deleted, so the projects are not read
    [Fact]
    public async Task Handle_WithMerge_DoesNotCheckTheProjects()
    {
        _projectList.Add(TestData.NewProject("AppA", _baGetter));

        Result result = await Handle(true, TestData.EditorConfigModel("default"));

        Assert.True(result.IsSuccess);
        _projects.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_KeepsTheServerIdOfATypeWithTheSameName()
    {
        Result result = await Handle(false, TestData.EditorConfigModel("Default", "new"),
            TestData.EditorConfigModel("BaGetter"));

        Assert.True(result.IsSuccess);
        Assert.Empty(_added);
        Assert.Empty(_deleted);
        Assert.Contains(_updated, t => t.Id == _default.Id && t.Name == "Default" && t.Content == "new");
        Assert.Contains(_updated, t => t.Id == _baGetter.Id);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //Every uploaded type is written again, so the version of each stored one grows by one
    [Fact]
    public async Task Handle_GivesTheUpdatedTypesTheNextVersion()
    {
        await Handle(true, TestData.EditorConfigModel("default", "new"), TestData.EditorConfigModel("BaGetter"));

        Assert.Equal(7, Assert.Single(_updated, t => t.Id == _default.Id).Version);
        Assert.Equal(2, Assert.Single(_updated, t => t.Id == _baGetter.Id).Version);
    }

    [Fact]
    public async Task Handle_AddsANewNameWithANewServerIdAndTheFirstVersion()
    {
        Result result = await Handle(true, TestData.EditorConfigModel("React"));

        Assert.True(result.IsSuccess);
        EditorConfigFileType added = Assert.Single(_added);
        Assert.Equal("React", added.Name);
        Assert.Equal("root = true", added.Content);
        Assert.NotEqual(_default.Id, added.Id);
        Assert.NotEqual(_baGetter.Id, added.Id);
        Assert.Equal(1, added.Version);
    }

    [Fact]
    public async Task Handle_WithoutMerge_DeletesTheTypesMissingFromTheList()
    {
        Result result = await Handle(false, TestData.EditorConfigModel("default"));

        Assert.True(result.IsSuccess);
        Assert.Equal(_baGetter.Id, Assert.Single(_deleted).Id);
        Assert.Equal(_default.Id, Assert.Single(_updated).Id);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMerge_KeepsTheTypesMissingFromTheList()
    {
        Result result = await Handle(true, TestData.EditorConfigModel("default"));

        Assert.True(result.IsSuccess);
        Assert.Empty(_deleted);
        Assert.Equal(_default.Id, Assert.Single(_updated).Id);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutMerge_DeletesEveryType_WhenTheListIsEmpty()
    {
        Result result = await Handle(false);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, _deleted.Count);
        Assert.Empty(_added);
        Assert.Empty(_updated);
    }
}
