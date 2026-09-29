using SupportToolsServer.Application.Validation;
using Xunit;

namespace SupportToolsServer.Tests.Application.Validation;

public sealed class UniqueValuesTests
{
    [Fact]
    public void AreUnique_ReturnsTrue_ForDifferentValues()
    {
        Assert.True(UniqueValues.AreUnique(["CSharp", "React"]));
    }

    [Fact]
    public void AreUnique_ReturnsFalse_ForValuesThatDifferOnlyInCase()
    {
        Assert.False(UniqueValues.AreUnique(["CSharp", "React", "csharp"]));
    }

    [Fact]
    public void AreUnique_IgnoresMissingAndBlankValues()
    {
        Assert.True(UniqueValues.AreUnique(["CSharp", null, null, string.Empty, string.Empty, "  ", "  ", "React"]));
    }

    [Fact]
    public void AreUnique_ReturnsTrue_ForAnEmptyList()
    {
        Assert.True(UniqueValues.AreUnique([]));
    }
}
