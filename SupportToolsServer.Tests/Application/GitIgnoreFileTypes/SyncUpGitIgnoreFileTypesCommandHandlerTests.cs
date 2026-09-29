using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.GitIgnoreFileTypes.SyncUp;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitIgnoreFileTypes;

public sealed class SyncUpGitIgnoreFileTypesCommandHandlerTests
{
    private readonly List<GitIgnoreFileType> _added = [];
    private readonly GitIgnoreFileType _cSharp = TestData.NewGitIgnoreFileType("CSharp", "old");
    private readonly List<GitIgnoreFileType> _deleted = [];
    private readonly Mock<IGitIgnoreFileTypeRepository> _gitIgnoreFileTypes = new();
    private readonly Mock<IGitRepoRepository> _gitRepos = new();
    private readonly GitIgnoreFileType _react = TestData.NewGitIgnoreFileType("React");
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly List<GitIgnoreFileType> _updated = [];

    public SyncUpGitIgnoreFileTypesCommandHandlerTests()
    {
        _gitIgnoreFileTypes.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(() => [_cSharp, _react]);
        _gitIgnoreFileTypes.Setup(r => r.Add(It.IsAny<GitIgnoreFileType>())).Callback<GitIgnoreFileType>(_added.Add);
        _gitIgnoreFileTypes.Setup(r => r.Update(It.IsAny<GitIgnoreFileType>()))
            .Callback<GitIgnoreFileType>(_updated.Add);
        _gitIgnoreFileTypes.Setup(r => r.Delete(It.IsAny<GitIgnoreFileType>()))
            .Callback<GitIgnoreFileType>(_deleted.Add);
    }

    private void GivenGitRepos(params GitRepo[] gitRepos)
    {
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([.. gitRepos]);
    }

    private Task<Result> Handle(bool merge, params StsGitIgnoreFileTypeDataModel[] uploaded)
    {
        var handler = new SyncUpGitIgnoreFileTypesCommandHandler(_gitIgnoreFileTypes.Object, _gitRepos.Object,
            _unitOfWork.Object);
        return handler.Handle(new SyncUpGitIgnoreFileTypesCommand(merge, [.. uploaded]), CancellationToken.None);
    }

    private static StsGitIgnoreFileTypeDataModel Uploaded(string name, string content)
    {
        //The client sends a new Id every time
        return new StsGitIgnoreFileTypeDataModel { Id = Guid.NewGuid(), Name = name, Content = content };
    }

    [Fact]
    public async Task Handle_KeepsTheServerIdOfATypeWithTheSameName()
    {
        GivenGitRepos();

        Result result = await Handle(false, Uploaded("csharp", "new"), Uploaded("React", "nm/"));

        Assert.True(result.IsSuccess);
        Assert.Empty(_added);
        Assert.Contains(_updated, t => t.Id == _cSharp.Id && t.Name == "csharp" && t.Content == "new");
        Assert.Contains(_updated, t => t.Id == _react.Id);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AddsANewNameWithAServerId()
    {
        GivenGitRepos();
        StsGitIgnoreFileTypeDataModel python = Uploaded("Python", "venv/");

        Result result = await Handle(true, python);

        Assert.True(result.IsSuccess);
        GitIgnoreFileType added = Assert.Single(_added);
        Assert.Equal("Python", added.Name);
        Assert.NotEqual(python.Id, added.Id.Value);
    }

    [Fact]
    public async Task Handle_WithoutMerge_DeletesTheUnusedTypesMissingFromTheList()
    {
        GivenGitRepos(TestData.NewGitRepo("RepoA", _cSharp));

        Result result = await Handle(false, Uploaded("CSharp", "bin/"));

        Assert.True(result.IsSuccess);
        Assert.Equal(_react.Id, Assert.Single(_deleted).Id);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutMerge_RefusesToDeleteATypeThatAGitUses()
    {
        GivenGitRepos(TestData.NewGitRepo("RepoB", _react));

        Result result = await Handle(false, Uploaded("CSharp", "bin/"));

        Assert.Equal("GitIgnoreFileTypeIsInUse", result.Error.Code);
        Assert.Equal("GitIgnore File Type Is Used By Gits: React (RepoB)", result.Error.Description);
        Assert.Empty(_added);
        Assert.Empty(_updated);
        Assert.Empty(_deleted);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMerge_KeepsTheMissingTypesWithoutCheckingTheGits()
    {
        GivenGitRepos(TestData.NewGitRepo("RepoB", _react));

        Result result = await Handle(true, Uploaded("CSharp", "bin/"));

        Assert.True(result.IsSuccess);
        Assert.Empty(_deleted);
        _gitRepos.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
