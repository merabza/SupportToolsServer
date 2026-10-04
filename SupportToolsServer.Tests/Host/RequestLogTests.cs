using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using SupportToolsServer.Tests.TestInfrastructure;
using Xunit;

namespace SupportToolsServer.Tests.Host;

//The clients send the API key in the query, and the request log of ASP.NET Core (Request starting/finished) writes the
//whole address. UseSerilogLogger (WebSystemTools) masks the key, so the log of the real host keeps the requests without
//the keys. The test hosts write the log only to the console (SupportToolsServerHostFactory), and the test reads it there
[Collection(SerialHostCollection.Name)]
public sealed class RequestLogTests
{
    private const string RefusedApiKey = "refused-test-key";

    private static async Task<HttpStatusCode> Send(SupportToolsServerHostFactory factory, HttpMethod method,
        string route)
    {
        using var request = new HttpRequestMessage(method, new Uri(route, UriKind.Relative));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.SendAsync(request);
        return response.StatusCode;
    }

    //The host writes Request finished after the request, which can be after the client has got the answer
    private static async Task<string> WaitForFinishedRequests(ConsoleCapture console, int requestCount)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        string log = console.Text;
        while (log.Split("Request finished").Length - 1 < requestCount && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
            log = console.Text;
        }

        return log;
    }

    //The REST client sends ?apikey=, the message hub ?ApiKey=...&negotiateVersion=1
    [Fact]
    public async Task RequestLog_KeepsTheRequestsButNeitherTheValidNorTheRefusedApiKey()
    {
        const string validKey = SupportToolsServerHostFactory.ValidApiKey;
        using var console = new ConsoleCapture();
        using var factory = new SupportToolsServerHostFactory();

        HttpStatusCode gitRepos = await Send(factory, HttpMethod.Get, $"api/v1/git/gitrepos?apikey={validKey}");
        HttpStatusCode npmPackage =
            await Send(factory, HttpMethod.Get, $"api/v1/npmpackages/react?version=1&apikey={validKey}");
        HttpStatusCode negotiate = await Send(factory, HttpMethod.Post,
            $"api/v1/messages/negotiate?ApiKey={validKey}&negotiateVersion=1");
        HttpStatusCode refused = await Send(factory, HttpMethod.Get, $"api/v1/git/gitrepos?apikey={RefusedApiKey}");
        string log = await WaitForFinishedRequests(console, 4);

        Assert.Equal(HttpStatusCode.OK, gitRepos);
        Assert.Equal(HttpStatusCode.OK, npmPackage);
        Assert.Equal(HttpStatusCode.OK, negotiate);
        Assert.Equal(HttpStatusCode.Unauthorized, refused);
        Assert.Contains("GET http://localhost/api/v1/git/gitrepos?apikey=***", log, StringComparison.Ordinal);
        Assert.Contains("GET http://localhost/api/v1/npmpackages/react?version=1&apikey=***", log,
            StringComparison.Ordinal);
        Assert.Contains("POST http://localhost/api/v1/messages/negotiate?ApiKey=***&negotiateVersion=1", log,
            StringComparison.Ordinal);
        Assert.DoesNotContain(validKey, log, StringComparison.Ordinal);
        Assert.DoesNotContain(RefusedApiKey, log, StringComparison.Ordinal);
    }
}
