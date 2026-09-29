using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.EditorConfigFileTypes.SyncUp;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.EditorConfigFileTypes;

public sealed class SyncUpEditorConfigFileTypesCommandHandlerTests
{
    private readonly List<EditorConfigFileType> _added = [];
    private readonly EditorConfigFileType _baGetter = TestData.NewEditorConfigFileType("BaGetter");
    private readonly EditorConfigFileType _default = TestData.NewEditorConfigFileType("default", "old");
    private readonly List<EditorConfigFileType> _deleted = [];
    private readonly Mock<IEditorConfigFileTypeRepository> _editorConfigFileTypes = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly List<EditorConfigFileType> _updated = [];

    public SyncUpEditorConfigFileTypesCommandHandlerTests()
    {
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
        var handler = new SyncUpEditorConfigFileTypesCommandHandler(_editorConfigFileTypes.Object, _unitOfWork.Object);
        return handler.Handle(new SyncUpEditorConfigFileTypesCommand(merge, [.. uploaded]), CancellationToken.None);
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

    [Fact]
    public async Task Handle_AddsANewNameWithANewServerId()
    {
        Result result = await Handle(true, TestData.EditorConfigModel("React", "root = true"));

        Assert.True(result.IsSuccess);
        EditorConfigFileType added = Assert.Single(_added);
        Assert.Equal("React", added.Name);
        Assert.Equal("root = true", added.Content);
        Assert.NotEqual(_default.Id, added.Id);
        Assert.NotEqual(_baGetter.Id, added.Id);
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
