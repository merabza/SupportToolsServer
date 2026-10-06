using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SupportToolsServer.Application.StoredFiles.UpdateStoredFile;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Host;

//The path of a stored file cannot be a route key (\ and :), so it goes in the query. The client escapes it with
//Uri.EscapeDataString and ASP.NET Core unescapes a query value fully. GET files/content of the factory is a stub that
//returns the path it received
public sealed class StoredFilePathTests : IClassFixture<SupportToolsServerHostFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SupportToolsServerHostFactory _factory;

    public StoredFilePathTests(SupportToolsServerHostFactory factory)
    {
        _factory = factory;
    }

    public static TheoryData<string> Paths =>
    [
        @"D:\1WorkSecurity\AppA\PAZISI\Prod\appsettings.json",
        @"D:\1WorkSecurity\My App\a b+c&d=e#f%g;h,i'j(k)l!m~n@o$p[q]r{s}t^u`v.json",
        @"D:\1WorkSecurity\%2F%5C%26\a.json",
        @"D:\1WorkSecurity\ქართული\ფაილი.json"
    ];

    [Theory]
    [MemberData(nameof(Paths))]
    public async Task GetStoredFile_GivesTheHandlerThePathThatTheClientEscaped(string path)
    {
        using HttpClient client = _factory.CreateClient();

        const string apiKey = SupportToolsServerHostFactory.ValidApiKey;

        using HttpResponseMessage response = await client.GetAsync(new Uri(
            $"api/v1/files/content?path={Uri.EscapeDataString(path)}&apikey={apiKey}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var storedFile = JsonSerializer.Deserialize<StsStoredFileDataModel>(
            await response.Content.ReadAsStringAsync(), WebJsonOptions);
        Assert.NotNull(storedFile);
        Assert.Equal(path, storedFile.Path);
    }

    //The real client of SupportTools, with its API key added to the query after the path
    [Theory]
    [MemberData(nameof(Paths))]
    public async Task SupportToolsServerApiClient_GetsTheFileOfThePath(string path)
    {
        var apiClient = new SupportToolsServerApiClient(null, new TestServerHttpClientFactory(_factory),
            "http://localhost/api/v1", SupportToolsServerHostFactory.ValidApiKey, false);

        Result<StsStoredFileDataModel> result = await apiClient.GetStoredFile(path);

        Assert.True(result.IsSuccess);
        Assert.Equal(path, result.Value.Path);
        Assert.Equal(TestData.MadeUpFileContent, result.Value.Content);
    }

    //The upsert handler takes the time of the change from the clock that the host registers
    [Fact]
    public void Host_BuildsTheUpsertHandlerOfTheStoredFiles()
    {
        using IServiceScope scope = _factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateStoredFileCommand, int>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<TimeProvider>());
    }

    private sealed class TestServerHttpClientFactory : IHttpClientFactory
    {
        private readonly SupportToolsServerHostFactory _factory;

        public TestServerHttpClientFactory(SupportToolsServerHostFactory factory)
        {
            _factory = factory;
        }

        public HttpClient CreateClient(string name)
        {
            return _factory.CreateClient();
        }
    }
}
