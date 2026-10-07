using System.Collections.Generic;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.UpdateGitProject;

//რეპოზიტორიის კლონში csproj და esproj ფაილების ძებნა და მათი ProjectReference-ების წაკითხვა, როგორც SupportTools-ის
//Update Git Projects-ში (GitProjectsUpdater.ProcessFolder)
public interface IGitProjectFilesScanner
{
    //projectFolderPath Gits ფოლდერის პირდაპირი ქვეფოლდერია (IGitsWorkFolder.GetProjectFolderPath), ამიტომ შედეგის გზები
    //Gits-ის მიმართ ითვლება და რეპოზიტორიის ფოლდერის სახელით იწყება
    Result<List<ScannedGitProject>> Scan(string projectFolderPath);
}
