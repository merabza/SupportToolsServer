using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.GitIgnoreFileTypes.DeleteGitIgnoreFileType;
using SupportToolsServer.Application.GitIgnoreFileTypes.EnsureGitIgnoreFileType;
using SupportToolsServer.Application.GitIgnoreFileTypes.GetGitIgnoreFileTypes;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitIgnoreFileTypes;

public sealed class GitIgnoreFileTypeHandlersTests
{
    private readonly GitIgnoreFileType _cSharp = TestData.NewGitIgnoreFileType("CSharp");
    private readonly Mock<IGitIgnoreFileTypeRepository> _gitIgnoreFileTypes = new();
    private readonly Mock<IGitRepoRepository> _gitRepos = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task GetGitIgnoreFileTypes_ReturnsTheTypesInNameOrder()
    {
        _gitIgnoreFileTypes.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewGitIgnoreFileType("React", "nm/"), TestData.NewGitIgnoreFileType("basic"), _cSharp
        ]);
        var handler = new GetGitIgnoreFileTypesQueryHandler(_gitIgnoreFileTypes.Object);

        Result<List<StsGitIgnoreFileTypeDataModel>> result =
            await handler.Handle(new GetGitIgnoreFileTypesQuery(), CancellationToken.None);

        Assert.Equal(["basic", "CSharp", "React"], result.Value.Select(x => x.Name));
        Assert.Equal(_cSharp.Id.Value, result.Value[1].Id);
        Assert.Equal("nm/", result.Value[2].Content);
    }

    [Fact]
    public async Task EnsureGitIgnoreFileType_LeavesAStoredTypeUnchanged()
    {
        _gitIgnoreFileTypes.Setup(r => r.GetByName("CSharp", It.IsAny<CancellationToken>())).ReturnsAsync(_cSharp);
        var handler = new EnsureGitIgnoreFileTypeCommandHandler(_gitIgnoreFileTypes.Object, _unitOfWork.Object);

        Result result = await handler.Handle(new EnsureGitIgnoreFileTypeCommand("CSharp"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _gitIgnoreFileTypes.Verify(r => r.Add(It.IsAny<GitIgnoreFileType>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureGitIgnoreFileType_AddsAMissingTypeWithEmptyContent()
    {
        _gitIgnoreFileTypes.Setup(r => r.GetByName("Python", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitIgnoreFileType?)null);
        var handler = new EnsureGitIgnoreFileTypeCommandHandler(_gitIgnoreFileTypes.Object, _unitOfWork.Object);

        Result result = await handler.Handle(new EnsureGitIgnoreFileTypeCommand("Python"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _gitIgnoreFileTypes.Verify(r => r.Add(It.Is<GitIgnoreFileType>(t => t.Name == "Python" && t.Content == "")),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteGitIgnoreFileType_ReturnsNotFound_WhenThereIsNoSuchType()
    {
        _gitIgnoreFileTypes.Setup(r => r.GetByName("Python", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitIgnoreFileType?)null);
        var handler = new DeleteGitIgnoreFileTypeCommandHandler(_gitIgnoreFileTypes.Object, _gitRepos.Object,
            _unitOfWork.Object);

        Result result = await handler.Handle(new DeleteGitIgnoreFileTypeCommand("Python"), CancellationToken.None);

        Assert.Equal("GitIgnoreFileTypeWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGitIgnoreFileType_RefusesATypeThatAGitUses()
    {
        _gitIgnoreFileTypes.Setup(r => r.GetByName("CSharp", It.IsAny<CancellationToken>())).ReturnsAsync(_cSharp);
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.NewGitRepo("RepoA", _cSharp)]);
        var handler = new DeleteGitIgnoreFileTypeCommandHandler(_gitIgnoreFileTypes.Object, _gitRepos.Object,
            _unitOfWork.Object);

        Result result = await handler.Handle(new DeleteGitIgnoreFileTypeCommand("CSharp"), CancellationToken.None);

        Assert.Equal("GitIgnoreFileTypeIsInUse", result.Error.Code);
        Assert.Equal("GitIgnore File Type Is Used By Gits: CSharp (RepoA)", result.Error.Description);
        _gitIgnoreFileTypes.Verify(r => r.Delete(It.IsAny<GitIgnoreFileType>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGitIgnoreFileType_DeletesAnUnusedTypeAndSaves()
    {
        _gitIgnoreFileTypes.Setup(r => r.GetByName("CSharp", It.IsAny<CancellationToken>())).ReturnsAsync(_cSharp);
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var handler = new DeleteGitIgnoreFileTypeCommandHandler(_gitIgnoreFileTypes.Object, _gitRepos.Object,
            _unitOfWork.Object);

        Result result = await handler.Handle(new DeleteGitIgnoreFileTypeCommand("CSharp"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _gitIgnoreFileTypes.Verify(r => r.Delete(_cSharp), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
