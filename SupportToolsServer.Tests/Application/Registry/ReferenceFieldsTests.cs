using System.Collections.Generic;
using SupportToolsServer.Application.Registry;
using SupportToolsServerCore.Domain.FileStorages;
using Xunit;

namespace SupportToolsServer.Tests.Application.Registry;

public sealed class ReferenceFieldsTests
{
    private readonly FileStorageId _exchange = FileStorageId.CreateUnique();
    private readonly FileStorageId _other = FileStorageId.CreateUnique();

    [Fact]
    public void GetName_ReturnsTheNameOfTheId_OrNullWithoutAnId()
    {
        IReadOnlyDictionary<FileStorageId, string> names =
            new Dictionary<FileStorageId, string> { [_exchange] = "Exchange" };

        Assert.Equal("Exchange", names.GetName(new FileStorageId(_exchange.Value)));
        Assert.Null(names.GetName<FileStorageId>(null));
    }

    //The fields that hold the id, in the given order, named after the aggregate
    [Fact]
    public void Usages_NamesTheFieldsThatHoldTheId()
    {
        IEnumerable<string> usages = ReferenceFields.Usages("Settings", _exchange, ("Second", _exchange),
            ("Other", _other), ("None", null), ("First", new FileStorageId(_exchange.Value)));

        Assert.Equal(["Settings.Second", "Settings.First"], usages);
    }

    [Fact]
    public void Usages_IsEmpty_WhenNoFieldHoldsTheId()
    {
        Assert.Empty(ReferenceFields.Usages("Settings", _exchange, ("Other", _other), ("None", null)));
    }
}
