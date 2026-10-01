using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.EditorConfigFileTypes.DeleteEditorConfigFileType;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.EditorConfigFileTypes;

public sealed class DeleteEditorConfigFileTypeCommandHandlerTests
{
    private readonly EditorConfigFileType _baGetter = TestData.NewEditorConfigFileType("BaGetter");
    private readonly EditorConfigFileType _default = TestData.NewEditorConfigFileType("default");
    private readonly Mock<IEditorConfigFileTypeRepository> _editorConfigFileTypes = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public DeleteEditorConfigFileTypeCommandHandlerTests()
    {
        _editorConfigFileTypes.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => [_default, _baGetter]);
    }

    private Task<Result> Handle(string name)
    {
        var handler = new DeleteEditorConfigFileTypeCommandHandler(_editorConfigFileTypes.Object, _unitOfWork.Object);
        return handler.Handle(new DeleteEditorConfigFileTypeCommand(name), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenThereIsNoSuchType()
    {
        Result result = await Handle("React");

        Assert.Equal("EditorConfigFileTypeWithNameNotFound", result.Error.Code);
        Assert.Equal("EditorConfig File Type With Name React Not Found", result.Error.Description);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        _editorConfigFileTypes.Verify(r => r.Delete(It.IsAny<EditorConfigFileType>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    //სახელი რეგისტრის გარეშე ედრება, როგორც სახელის უნიკალურ ინდექსში
    [Fact]
    public async Task Handle_DeletesTheTypeWithTheSameNameInAnyCaseAndSaves()
    {
        Result result = await Handle("BAGETTER");

        Assert.True(result.IsSuccess);
        _editorConfigFileTypes.Verify(r => r.Delete(_baGetter), Times.Once);
        _editorConfigFileTypes.Verify(r => r.Delete(_default), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
