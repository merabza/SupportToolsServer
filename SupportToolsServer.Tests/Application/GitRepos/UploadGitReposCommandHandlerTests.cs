using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.GitRepos.UploadGitRepos;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitRepos;

public sealed class UploadGitReposCommandHandlerTests
{
    private readonly List<GitIgnoreFileType> _added = [];
    private readonly List<GitRepo> _addedGitRepos = [];
    private readonly GitIgnoreFileType _cSharp = TestData.NewGitIgnoreFileType("CSharp", "old");
    private readonly Mock<IGitIgnoreFileTypeRepository> _gitIgnoreFileTypes = new();
    private readonly Mock<IGitRepoRepository> _gitRepos = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly List<GitIgnoreFileType> _updated = [];
    private readonly List<GitRepo> _updatedGitRepos = [];

    public UploadGitReposCommandHandlerTests()
    {
        _gitIgnoreFileTypes.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([_cSharp]);
        _gitIgnoreFileTypes.Setup(r => r.Add(It.IsAny<GitIgnoreFileType>())).Callback<GitIgnoreFileType>(_added.Add);
        _gitIgnoreFileTypes.Setup(r => r.Update(It.IsAny<GitIgnoreFileType>()))
            .Callback<GitIgnoreFileType>(_updated.Add);
        _gitRepos.Setup(r => r.Add(It.IsAny<GitRepo>())).Callback<GitRepo>(_addedGitRepos.Add);
        _gitRepos.Setup(r => r.Update(It.IsAny<GitRepo>())).Callback<GitRepo>(_updatedGitRepos.Add);
    }

    private void GivenGitRepos(params GitRepo[] gitRepos)
    {
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([.. gitRepos]);
    }

    private Task<Result> Handle(List<StsGitDataModel> gits, List<StsGitIgnoreFileTypeDataModel> gitIgnoreFiles)
    {
        var handler =
            new UploadGitReposCommandHandler(_gitRepos.Object, _gitIgnoreFileTypes.Object, _unitOfWork.Object);
        return handler.Handle(new UploadGitReposCommand(gits, gitIgnoreFiles), CancellationToken.None);
    }

    private void VerifyNothingChanged()
    {
        Assert.Empty(_added);
        Assert.Empty(_updated);
        Assert.Empty(_addedGitRepos);
        Assert.Empty(_updatedGitRepos);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AddsNewTypesAndGitsThatUseThem()
    {
        GivenGitRepos();

        Result result = await Handle([TestData.GitModel("RepoA", "react")], [TestData.GitIgnoreModel("React", "nm/")]);

        Assert.True(result.IsSuccess);
        GitIgnoreFileType react = Assert.Single(_added);
        Assert.Equal("React", react.Name);
        Assert.Equal("nm/", react.Content);
        GitRepo gitRepo = Assert.Single(_addedGitRepos);
        Assert.Equal("RepoA", gitRepo.Name);
        Assert.Equal(TestData.AddressOf("RepoA"), gitRepo.Address);
        Assert.Equal("RepoA", gitRepo.FolderName);
        Assert.Equal(react.Id, gitRepo.GitIgnoreFileTypeId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatesTheStoredTypeOfTheSameNameKeepingItsId()
    {
        GivenGitRepos();

        Result result = await Handle([], [TestData.GitIgnoreModel("csharp", "new")]);

        Assert.True(result.IsSuccess);
        Assert.Empty(_added);
        GitIgnoreFileType updated = Assert.Single(_updated);
        Assert.Equal(_cSharp.Id, updated.Id);
        Assert.Equal("csharp", updated.Name);
        Assert.Equal("new", updated.Content);
    }

    [Fact]
    public async Task Handle_UpdatesTheStoredGitOfTheSameNameKeepingItsId()
    {
        GitRepo stored = TestData.NewGitRepo("repoa", _cSharp, "git@github.com:test/old.git");
        GivenGitRepos(stored);

        Result result = await Handle([TestData.GitModel("RepoA", "CSharp")], []);

        Assert.True(result.IsSuccess);
        Assert.Empty(_addedGitRepos);
        GitRepo updated = Assert.Single(_updatedGitRepos);
        Assert.Equal(stored.Id, updated.Id);
        Assert.Equal("RepoA", updated.Name);
        Assert.Equal(TestData.AddressOf("RepoA"), updated.Address);
        Assert.Equal(_cSharp.Id, updated.GitIgnoreFileTypeId);
    }

    [Fact]
    public async Task Handle_NeverDeletes()
    {
        GivenGitRepos(TestData.NewGitRepo("RepoB", _cSharp));

        await Handle([TestData.GitModel("RepoA", "CSharp")], [TestData.GitIgnoreModel("React")]);

        _gitRepos.Verify(r => r.Delete(It.IsAny<GitRepo>()), Times.Never);
        _gitIgnoreFileTypes.Verify(r => r.Delete(It.IsAny<GitIgnoreFileType>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsTheUnknownPatternsOnce_AndChangesNothing()
    {
        GivenGitRepos();

        Result result = await Handle([
            TestData.GitModel("RepoA", "Missing"), TestData.GitModel("RepoB", "missing"),
            TestData.GitModel("RepoC", "Other"), TestData.GitModel("RepoD", "CSharp")
        ], [TestData.GitIgnoreModel("React")]);

        Assert.Equal("GitIgnoreFileTypeWithNameNotFound", result.Error.Code);
        Assert.Equal("GitIgnore File Type With Name Missing, Other Not Found", result.Error.Description);
        VerifyNothingChanged();
    }

    [Fact]
    public async Task Handle_ReturnsGitAddressIsInUse_WhenAStoredGitHasTheAddress_AndChangesNothing()
    {
        GivenGitRepos(TestData.NewGitRepo("RepoC", _cSharp));

        Result result = await Handle([TestData.GitModel("RepoH", "CSharp", TestData.AddressOf("RepoC"))],
            [TestData.GitIgnoreModel("React")]);

        Assert.Equal("GitAddressIsInUse", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"Git Address {TestData.AddressOf("RepoC")} Is Used By RepoC, RepoH", result.Error.Description);
        VerifyNothingChanged();
    }

    [Fact]
    public async Task Handle_AllowsTheAddressToMoveBetweenGitsOfTheUpload()
    {
        GivenGitRepos(TestData.NewGitRepo("RepoC", _cSharp));

        Result result = await Handle([
            TestData.GitModel("RepoC", "CSharp", "git@github.com:test/moved.git"),
            TestData.GitModel("RepoH", "CSharp", TestData.AddressOf("RepoC"))
        ], []);

        Assert.True(result.IsSuccess);
    }
}
