namespace SupportToolsServerDb.Models;

public class ApiKeyByRemoteIpAddress
{
    public int Id { get; set; }
    public required string ApiKey { get; set; }
    public required string RemoteIpAddress { get; set; }
}