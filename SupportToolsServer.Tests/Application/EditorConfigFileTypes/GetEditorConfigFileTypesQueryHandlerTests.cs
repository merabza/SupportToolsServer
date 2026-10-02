using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.EditorConfigFileTypes.GetEditorConfigFileTypes;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.EditorConfigFileTypes;

public sealed class GetEditorConfigFileTypesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsTheTypesInNameOrder()
    {
        var editorConfigFileTypes = new Mock<IEditorConfigFileTypeRepository>();
        editorConfigFileTypes.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewEditorConfigFileType("React", "[*.ts]", 4), TestData.NewEditorConfigFileType("basic"),
            TestData.NewEditorConfigFileType("CSharp")
        ]);
        var handler = new GetEditorConfigFileTypesQueryHandler(editorConfigFileTypes.Object);

        Result<List<StsEditorConfigFileTypeDataModel>> result =
            await handler.Handle(new GetEditorConfigFileTypesQuery(), CancellationToken.None);

        Assert.Equal(["basic", "CSharp", "React"], result.Value.Select(x => x.Name));
        Assert.Equal("[*.ts]", result.Value[2].Content);
        Assert.Equal([1, 1, 4], result.Value.Select(x => x.Version));
    }
}
