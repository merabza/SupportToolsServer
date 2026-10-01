namespace SupportToolsServer.Infrastructure.Options;

//appsettings.json-ის AppOptions სექცია
public sealed class AppOptions
{
    public const string SectionName = "AppOptions";

    //სამუშაო ფოლდერი, რომლის Gits ქვეფოლდერშიც რეპოზიტორიები იკლონება
    public string? WorkFolder { get; set; }
}
