using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.GitRepos.GetGitRepoByKey;
using SupportToolsServer.Application.GitRepos.GetGitRepos;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitRepos;

public sealed class GitRepoQueryHandlersTests
{
    private readonly GitIgnoreFileType _cSharp = TestData.NewGitIgnoreFileType("CSharp");
    private readonly Mock<IGitIgnoreFileTypeRepository> _gitIgnoreFileTypes = new();
    private readonly Mock<IGitRepoRepository> _gitRepos = new();
    private readonly GitIgnoreFileType _react = TestData.NewGitIgnoreFileType("React");

    public GitRepoQueryHandlersTests()
    {
        _gitIgnoreFileTypes.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([_cSharp, _react]);
    }

    [Fact]
    public async Task GetGitRepos_ReturnsTheGitsInNameOrderWithTheirPatternNames()
    {
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewGitRepo("RepoC", _react), TestData.NewGitRepo("repoB", _cSharp, null, 7),
            TestData.NewGitRepo("RepoA", _react)
        ]);
        var handler = new GetGitReposQueryHandler(_gitRepos.Object, _gitIgnoreFileTypes.Object);

        Result<List<StsGitDataModel>> result = await handler.Handle(new GetGitReposQuery(), CancellationToken.None);

        Assert.Equal(["RepoA", "repoB", "RepoC"], result.Value.Select(x => x.GitProjectName));
        Assert.Equal(["React", "CSharp", "React"], result.Value.Select(x => x.GitIgnorePatternName));
        Assert.Equal(TestData.AddressOf("RepoA"), result.Value[0].GitProjectAddress);
        Assert.Equal([1, 7, 1], result.Value.Select(x => x.Version));
    }

    [Fact]
    public async Task GetGitRepoByKey_ReturnsGitWithKeyNotFound_WhenThereIsNoSuchGit()
    {
        _gitRepos.Setup(r => r.GetByName("RepoA", It.IsAny<CancellationToken>())).ReturnsAsync((GitRepo?)null);
        var handler = new GetGitRepoByKeyQueryHandler(_gitRepos.Object, _gitIgnoreFileTypes.Object);

        Result<StsGitDataModel> result =
            await handler.Handle(new GetGitRepoByKeyQuery("RepoA"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("GitWithKeyNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Git With Key RepoA Not Found", result.Error.Description);
    }

    [Fact]
    public async Task GetGitRepoByKey_ReturnsTheGitWithItsPatternName()
    {
        _gitRepos.Setup(r => r.GetByName("RepoA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewGitRepo("RepoA", _react, null, 2));
        var handler = new GetGitRepoByKeyQueryHandler(_gitRepos.Object, _gitIgnoreFileTypes.Object);

        Result<StsGitDataModel> result =
            await handler.Handle(new GetGitRepoByKeyQuery("RepoA"), CancellationToken.None);

        Assert.Equal("RepoA", result.Value.GitProjectName);
        Assert.Equal("React", result.Value.GitIgnorePatternName);
        Assert.Equal(2, result.Value.Version);
    }
}
