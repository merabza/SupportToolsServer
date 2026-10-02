using System;

namespace SupportToolsServer.WebApi.Endpoints;

//რეესტრის route-ის key (CLAUDE.md, Registry conventions). კლიენტი key-ს Uri.EscapeDataString-ით აგზავნის.
//ASP.NET Core route-ის მნიშვნელობაში escape-ებს ხსნის, %2F-ის გარდა, რომ ის / გამყოფში არ აერიოს. ამიტომ სახელს,
//რომელშიც / წერია (მაგალითად, npm-ის @scope/name), / აქ უბრუნდება
internal static class RouteKeys
{
    public static string Decode(string key)
    {
        return key.Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
    }
}
