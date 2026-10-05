using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.NpmPackages;
using SupportToolsServer.Application.NpmPackages.GetNpmPackageByName;
using SupportToolsServer.Application.NpmPackages.GetNpmPackages;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.NpmPackages;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.NpmPackages;

public sealed class NpmPackageQueryHandlersTests
{
    private readonly Mock<INpmPackageRepository> _npmPackages = new();

    //The order ignores case, so it differs from the ordinal one
    [Fact]
    public async Task GetNpmPackages_ReturnsEveryFieldAndTheVersionInNameOrder()
    {
        _npmPackages.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewNpmPackage("yup", "Schema validation"), TestData.NewNpmPackage("bootstrap", null, 4),
            TestData.NewNpmPackage("React-Dom", "DOM bindings", 2)
        ]);
        using var cancellation = new CancellationTokenSource();

        Result<List<StsNpmPackageDataModel>> result =
            await new GetNpmPackagesQueryHandler(_npmPackages.Object).Handle(new GetNpmPackagesQuery(),
                cancellation.Token);

        Assert.Equal(["bootstrap", "React-Dom", "yup"], result.Value.Select(x => x.Name));
        Assert.Equal([null, "DOM bindings", "Schema validation"], result.Value.Select(x => x.Description));
        Assert.Equal([4, 2, 1], result.Value.Select(x => x.Version));
        _npmPackages.Verify(r => r.GetAll(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetNpmPackageByName_ReturnsTheRecordWithItsVersion()
    {
        _npmPackages.Setup(r => r.GetByName("REACT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewNpmPackage("react", "UI library", 5));

        Result<StsNpmPackageDataModel> result =
            await new GetNpmPackageByNameQueryHandler(_npmPackages.Object).Handle(new GetNpmPackageByNameQuery("REACT"),
                CancellationToken.None);

        Assert.Equal("react", result.Value.Name);
        Assert.Equal("UI library", result.Value.Description);
        Assert.Equal(5, result.Value.Version);
    }

    [Fact]
    public async Task GetNpmPackageByName_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _npmPackages.Setup(r => r.GetByName("left-pad", It.IsAny<CancellationToken>())).ReturnsAsync((NpmPackage?)null);

        Result<StsNpmPackageDataModel> result =
            await new GetNpmPackageByNameQueryHandler(_npmPackages.Object).Handle(
                new GetNpmPackageByNameQuery("left-pad"), CancellationToken.None);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("NpmPackage With Name left-pad Not Found", result.Error.Description);
    }

    [Fact]
    public void ToContractModel_CopiesEveryField()
    {
        StsNpmPackageDataModel model = TestData.NewNpmPackage("react", "UI library", 7).ToContractModel();

        Assert.Equal("react", model.Name);
        Assert.Equal("UI library", model.Description);
        Assert.Equal(7, model.Version);
    }

    //Projects store a package by its id and give its name in the contract
    [Fact]
    public void ToNamesById_MapsTheIdOfEveryPackageToItsName()
    {
        NpmPackage react = TestData.NewNpmPackage("react");
        NpmPackage toolkit = TestData.NewNpmPackage("@reduxjs/toolkit");

        Dictionary<NpmPackageId, string> names = new[] { react, toolkit }.ToNamesById();

        Assert.Equal(2, names.Count);
        Assert.Equal("react", names[react.Id]);
        Assert.Equal("@reduxjs/toolkit", names[toolkit.Id]);
    }
}
