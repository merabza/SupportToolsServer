using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.ReactAppTemplates.UpdateReactAppTemplate;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.ReactAppTemplates;

public sealed class UpdateReactAppTemplateCommandHandlerTests
{
    private readonly Mock<IReactAppTemplateRepository> _reactAppTemplates = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void GivenStored(string name, ReactAppTemplate? stored)
    {
        _reactAppTemplates.Setup(r => r.GetByName(name, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(string name, string template, int version,
        CancellationToken cancellationToken = default)
    {
        var handler = new UpdateReactAppTemplateCommandHandler(_reactAppTemplates.Object, _unitOfWork.Object);
        return handler.Handle(
            new UpdateReactAppTemplateCommand(TestData.ReactAppTemplateModel(name, template, version)),
            cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _reactAppTemplates.Verify(r => r.Add(It.IsAny<ReactAppTemplate>()), Times.Never);
        _reactAppTemplates.Verify(r => r.Update(It.IsAny<ReactAppTemplate>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CreatesTheRecordWithTheFirstVersion_WhenVersionZeroIsExpected()
    {
        GivenStored("ReduxApp", null);
        ReactAppTemplate? added = null;
        _reactAppTemplates.Setup(r => r.Add(It.IsAny<ReactAppTemplate>())).Callback<ReactAppTemplate>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result = await Handle("ReduxApp", "redux-typescript", 0, cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal("ReduxApp", added.Name);
        Assert.Equal("redux-typescript", added.Template);
        Assert.Equal(1, added.Version);
        _reactAppTemplates.Verify(r => r.Update(It.IsAny<ReactAppTemplate>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButTheNameExists()
    {
        GivenStored("ReduxApp", TestData.NewReactAppTemplate("REDUXAPP", "redux-typescript", 2));

        Result<int> result = await Handle("ReduxApp", "redux-typescript", 0);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("ReactAppTemplate ReduxApp Version Conflict: Expected 0, Actual 2", result.Error.Description);
        VerifyNothingSaved();
    }

    //The name is matched without case, so the update also takes the spelling of the route key
    [Fact]
    public async Task Handle_UpdatesTheStoredRecordAndReturnsItsNextVersion_WhenTheExpectedVersionIsStored()
    {
        ReactAppTemplate stored = TestData.NewReactAppTemplate("ReduxApp", "Old", 3);
        GivenStored("REDUXAPP", stored);

        Result<int> result = await Handle("REDUXAPP", "cra-template-redux", 3);

        Assert.Equal(4, result.Value);
        _reactAppTemplates.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal("REDUXAPP", stored.Name);
        Assert.Equal("cra-template-redux", stored.Template);
        Assert.Equal(4, stored.Version);
        _reactAppTemplates.Verify(r => r.Add(It.IsAny<ReactAppTemplate>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored("ReduxApp", TestData.NewReactAppTemplate("ReduxApp", "redux-typescript", 3));

        Result<int> result = await Handle("ReduxApp", "New", expectedVersion);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"ReactAppTemplate ReduxApp Version Conflict: Expected {expectedVersion}, Actual 3",
            result.Error.Description);
        VerifyNothingSaved();
    }

    //The record was deleted on the server after the client last saw it
    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoRecord()
    {
        GivenStored("ReduxApp", null);

        Result<int> result = await Handle("ReduxApp", "redux-typescript", 2);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("ReactAppTemplate With Name ReduxApp Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    //Another request saved version 2 between the read and the save, so the concurrency token stopped the UPDATE
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _reactAppTemplates.SetupSequence(r => r.GetByName("ReduxApp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewReactAppTemplate("ReduxApp", "redux-typescript"))
            .ReturnsAsync(TestData.NewReactAppTemplate("ReduxApp", "Changed", 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle("ReduxApp", "Mine", 1);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ReactAppTemplate ReduxApp Version Conflict: Expected 1, Actual 2", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSameNameIsCreatedBeforeTheSave()
    {
        _reactAppTemplates.SetupSequence(r => r.GetByName("ReduxApp", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReactAppTemplate?)null).ReturnsAsync(TestData.NewReactAppTemplate("ReduxApp", "Theirs"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique index"));

        Result<int> result = await Handle("ReduxApp", "Mine", 0);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ReactAppTemplate ReduxApp Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }
}
