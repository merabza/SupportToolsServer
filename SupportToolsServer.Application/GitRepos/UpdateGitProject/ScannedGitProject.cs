using System.Collections.Generic;

namespace SupportToolsServer.Application.GitRepos.UpdateGitProject;

//სკანირებით ნაპოვნი პროექტის ფაილი, კლიენტის GitProjectDataModel-ის ველებით: Gits-ის მიმართ შეფარდებითი ფოლდერი (\-ით),
//ფაილის სახელი და ProjectReference-ების ფაილების სახელები გაფართოების გარეშე, დოკუმენტის რიგით. სახელი სიაში ერთხელ
//გვხვდება, რეგისტრის გაუთვალისწინებლად
public sealed record ScannedGitProject(
    string ProjectRelativePath,
    string ProjectFileName,
    IReadOnlyList<string> DependsOnProjectNames);
