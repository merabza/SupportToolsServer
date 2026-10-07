namespace SupportToolsServer.Infrastructure.Options;

//appsettings.json-ის AppOptions სექცია
public sealed class AppOptions
{
    public const string SectionName = "AppOptions";

    //PeriodicTimer-ის პერიოდი 49 დღეზე ნაკლები უნდა იყოს, ამიტომ განახლების შუალედი თვით იზღუდება
    public const int MaxGitProjectsRefreshHours = 720;

    //სამუშაო ფოლდერი, რომლის Gits ქვეფოლდერშიც რეპოზიტორიები იკლონება
    public string? WorkFolder { get; set; }

    //რამდენ საათში ერთხელ განახლდეს ყველა რეპოზიტორიის კლონი და მისი პროექტები (GitProjects, B9). პირველი განახლება
    //სტარტზე ხდება, 0 კი ამ განახლებას თიშავს
    public int GitProjectsRefreshHours { get; set; } = 24;
}
