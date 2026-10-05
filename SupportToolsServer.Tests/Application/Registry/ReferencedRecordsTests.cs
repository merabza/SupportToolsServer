using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Registry;

public sealed class ReferencedRecordsTests
{
    private readonly List<(string Name, CancellationToken Token)> _reads = [];

    private Task<string?> GetByName(string name, CancellationToken cancellationToken)
    {
        _reads.Add((name, cancellationToken));
        return Task.FromResult(name.StartsWith('x') ? null : $"record {name}");
    }

    [Fact]
    public async Task Find_ReturnsTheRecordOfTheName()
    {
        var references = new ReferencedRecords();
        using var cancellation = new CancellationTokenSource();

        string? found = await references.Find("a", "Type", GetByName, cancellation.Token);

        Assert.Equal("record a", found);
        Assert.True(references.AreAllFound);
        Assert.Equal([("a", cancellation.Token)], _reads);
    }

    //No name is no reference: nothing is read and nothing is missing
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Find_ReturnsNullWithoutReading_WhenThereIsNoName(string? name)
    {
        var references = new ReferencedRecords();

        Assert.Null(await references.Find(name, "Type", GetByName, CancellationToken.None));
        Assert.True(references.AreAllFound);
        Assert.Empty(_reads);
    }

    [Fact]
    public async Task MissingError_NamesEveryMissingRecordOnceGroupedByTypeInTheOrderTheyWereLookedFor()
    {
        var references = new ReferencedRecords();

        await references.Find("x1", "Second", GetByName, CancellationToken.None);
        await references.Find("a", "First", GetByName, CancellationToken.None);
        await references.Find("x2", "First", GetByName, CancellationToken.None);
        await references.Find("x3", "Second", GetByName, CancellationToken.None);
        await references.Find("x1", "Second", GetByName, CancellationToken.None);

        Assert.False(references.AreAllFound);
        Error error = references.MissingError();
        Assert.Equal("ReferencedRecordsNotFound", error.Code);
        Assert.Equal(ErrorType.NotFound, error.Type);
        Assert.Equal("Referenced Second Records Not Found: x1, x3; Referenced First Records Not Found: x2",
            error.Description);
    }

    //The records that were read before, e.g. every git of a project at once
    [Fact]
    public void Find_FromTheRecordsThatWereRead_ReturnsTheRecordOfTheName()
    {
        var references = new ReferencedRecords();

        string? found = references.Find("a", "Type", name => $"record {name}");

        Assert.Equal("record a", found);
        Assert.True(references.AreAllFound);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Find_FromTheRecordsThatWereRead_ReturnsNullWithoutLooking_WhenThereIsNoName(string? name)
    {
        var references = new ReferencedRecords();
        List<string> looked = [];

        Assert.Null(references.Find(name, "Type", x =>
        {
            looked.Add(x);
            return "record";
        }));
        Assert.True(references.AreAllFound);
        Assert.Empty(looked);
    }

    //Both kinds of lookup add their missing names to the same error, in the order they were looked for
    [Fact]
    public async Task Find_FromTheRecordsThatWereRead_AddsAMissingNameToTheSameError()
    {
        var references = new ReferencedRecords();

        await references.Find("x1", "First", GetByName, CancellationToken.None);
        references.Find("x2", "Second", _ => (string?)null);
        references.Find("x3", "First", _ => (string?)null);

        Assert.False(references.AreAllFound);
        Assert.Equal("Referenced First Records Not Found: x1, x3; Referenced Second Records Not Found: x2",
            references.MissingError().Description);
    }
}
