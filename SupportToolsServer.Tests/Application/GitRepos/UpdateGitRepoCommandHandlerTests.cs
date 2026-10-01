using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.GitRepos.UpdateGitRepo;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitRepos;

public sealed class UpdateGitRepoCommandHandlerTests
{
    private readonly GitIgnoreFileType _cSharp = TestData.NewGitIgnoreFileType("CSharp");
    private readonly Mock<IGitIgnoreFileTypeRepository> _gitIgnoreFileTypes = new();
    private readonly Mock<IGitRepoRepository> _gitRepos = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateGitRepoCommandHandlerTests()
    {
        _gitIgnoreFileTypes.Setup(r => r.GetByName("CSharp", It.IsAny<CancellationToken>())).ReturnsAsync(_cSharp);
    }

    private void GivenGitRepos(params GitRepo[] gitRepos)
    {
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([.. gitRepos]);
    }

    private Task<Result> Handle(string name, string gitIgnorePatternName, string? address = null)
    {
        var handler = new UpdateGitRepoCommandHandler(_gitRepos.Object, _gitIgnoreFileTypes.Object, _unitOfWork.Object);
        return handler.Handle(new UpdateGitRepoCommand(TestData.GitModel(name, gitIgnorePatternName, address)),
            CancellationToken.None);
    }

    private void VerifyNothingSaved()
    {
        _gitRepos.Verify(r => r.Add(It.IsAny<GitRepo>()), Times.Never);
        _gitRepos.Verify(r => r.Update(It.IsAny<GitRepo>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsGitIgnoreFileTypeWithNameNotFound_WhenThePatternIsUnknown()
    {
        GivenGitRepos();

        Result result = await Handle("RepoA", "Missing");

        Assert.Equal("GitIgnoreFileTypeWithNameNotFound", result.Error.Code);
        Assert.Equal("GitIgnore File Type With Name Missing Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_AddsANewGitWithTheIdOfItsPattern()
    {
        GivenGitRepos(TestData.NewGitRepo("RepoB", _cSharp));

        Result result = await Handle("RepoA", "CSharp");

        Assert.True(result.IsSuccess);
        _gitRepos.Verify(
            r => r.Add(It.Is<GitRepo>(g =>
                g.Name == "RepoA" && g.Address == TestData.AddressOf("RepoA") && g.FolderName == "RepoA" &&
                g.GitIgnoreFileTypeId == _cSharp.Id)), Times.Once);
        _gitRepos.Verify(r => r.Update(It.IsAny<GitRepo>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatesTheStoredGitOfTheSameNameKeepingItsId()
    {
        GitRepo stored = TestData.NewGitRepo("repoa", _cSharp, "git@github.com:test/old.git");
        GivenGitRepos(stored);

        Result result = await Handle("RepoA", "CSharp");

        Assert.True(result.IsSuccess);
        _gitRepos.Verify(
            r => r.Update(It.Is<GitRepo>(g =>
                g.Id == stored.Id && g.Name == "RepoA" && g.Address == TestData.AddressOf("RepoA"))), Times.Once);
        _gitRepos.Verify(r => r.Add(It.IsAny<GitRepo>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RaisesGitRepoAddedDomainEvent_ForANewGit()
    {
        GivenGitRepos();
        GitRepo? added = null;
        _gitRepos.Setup(r => r.Add(It.IsAny<GitRepo>())).Callback<GitRepo>(g => added = g);

        await Handle("RepoA", "CSharp");

        Assert.NotNull(added);
        var domainEvent = Assert.IsType<GitRepoAddedDomainEvent>(Assert.Single(added.DomainEvents));
        Assert.Equal(new GitRepoAddedDomainEvent(added.Id, "RepoA", TestData.AddressOf("RepoA"), "RepoA"), domainEvent);
    }

    [Fact]
    public async Task Handle_ChangesTheStoredInstanceAndRaisesGitRepoUpdatedDomainEvent()
    {
        GitRepo stored = TestData.NewGitRepo("repoa", _cSharp, "git@github.com:test/old.git");
        GivenGitRepos(stored);

        await Handle("RepoA", "CSharp");

        _gitRepos.Verify(r => r.Update(stored), Times.Once);
        var domainEvent = Assert.IsType<GitRepoUpdatedDomainEvent>(Assert.Single(stored.DomainEvents));
        Assert.Equal(new GitRepoUpdatedDomainEvent(stored.Id, "RepoA", TestData.AddressOf("RepoA"), "RepoA"),
            domainEvent);
    }

    [Fact]
    public async Task Handle_KeepsTheAddressOfTheGitItself()
    {
        GivenGitRepos(TestData.NewGitRepo("RepoA", _cSharp));

        Result result = await Handle("RepoA", "CSharp");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ReturnsGitAddressIsInUse_WhenAnotherGitHasTheAddress()
    {
        GivenGitRepos(TestData.NewGitRepo("RepoB", _cSharp, "GIT@github.com:test/shared.git"));

        Result result = await Handle("RepoA", "CSharp", "git@github.com:test/shared.git");

        Assert.Equal("GitAddressIsInUse", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Git Address git@github.com:test/shared.git Is Used By RepoB", result.Error.Description);
        VerifyNothingSaved();
    }
}
