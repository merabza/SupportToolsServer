using System;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Validation;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.UpdateGitProject;

//SupportTools-ის GitOneProjectUpdater.UpdateOneGitProject-ის ანალოგი. კლიენტისგან განსხვავებით სერვერი
//ლოკალური ცვლილებების გაუქმებაზე თანხმობას არ ითხოვს: სამუშაო ფოლდერი ყოველთვის სერვერის ვერსიას უბრუნდება
public sealed class UpdateGitProjectCommandHandler : ICommandHandler<UpdateGitProjectCommand>
{
    private readonly IGitClient _gitClient;
    private readonly IGitsWorkFolder _gitsWorkFolder;

    public UpdateGitProjectCommandHandler(IGitsWorkFolder gitsWorkFolder, IGitClient gitClient)
    {
        _gitsWorkFolder = gitsWorkFolder;
        _gitClient = gitClient;
    }

    public Task<Result> Handle(UpdateGitProjectCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(UpdateGitProject(command));
    }

    private Result UpdateGitProject(UpdateGitProjectCommand command)
    {
        //Gits ფოლდერის გარეთ გასული გზა შეცდომაა, ამიტომ ასეთი ფოლდერი არც იშლება და არც იკლონება
        Result<string> projectFolderPathResult = _gitsWorkFolder.GetProjectFolderPath(GitsFolderName(command));
        if (projectFolderPathResult.IsFailure)
        {
            return projectFolderPathResult.Error;
        }

        string projectFolderPath = projectFolderPathResult.Value;

        //თუ ფოლდერი არსებობს, მაგრამ სხვა მისამართიდანაა დაკლონილი, წაიშალოს თავისი შიგთავსით
        if (_gitsWorkFolder.Exists(projectFolderPath))
        {
            Result<string> remoteOriginUrlResult = _gitClient.GetRemoteOriginUrl(projectFolderPath);
            if (remoteOriginUrlResult.IsFailure)
            {
                return remoteOriginUrlResult.Error;
            }

            if (remoteOriginUrlResult.Value != command.GitProjectAddress)
            {
                _gitsWorkFolder.Delete(projectFolderPath);
            }
        }

        if (!_gitsWorkFolder.Exists(projectFolderPath))
        {
            return _gitClient.Clone(command.GitProjectAddress, projectFolderPath);
        }

        //იდეაში სამუშაო ფოლდერში ცვლილებები არ უნდა მომხდარიყო, მაგრამ თუ მოხდა, უქმდება
        Result<bool> hasChangesResult = _gitClient.HasChanges(projectFolderPath);
        if (hasChangesResult.IsFailure)
        {
            return hasChangesResult.Error;
        }

        if (hasChangesResult.Value)
        {
            return _gitClient.Restore(projectFolderPath);
        }

        Result remoteUpdateResult = _gitClient.RemoteUpdate(projectFolderPath);
        if (remoteUpdateResult.IsFailure)
        {
            return remoteUpdateResult;
        }

        //ლოკალური და remote ვერსიები თუ განსხვავდება, გაკეთდეს pull
        Result<bool> needPullResult = _gitClient.NeedPull(projectFolderPath);
        if (needPullResult.IsFailure)
        {
            return needPullResult.Error;
        }

        return needPullResult.Value ? _gitClient.Pull(projectFolderPath) : Result.Success();
    }

    //SPA-ის წინსართიანი ფოლდერი კლიენტში SPA პროექტის ფოლდერის მიმართ ითვლება. Gits-ში, როგორც კლიენტის ქეშში
    //(GitRepos.Create, useGitRecordNameForComplexGitProjectFolderName), ასეთი რეპოზიტორია git-ის სახელით ინახება
    private static string GitsFolderName(UpdateGitProjectCommand command)
    {
        return command.GitProjectFolderName.StartsWith(GitRules.SpaProjectFolderRelativePathName,
            StringComparison.Ordinal)
            ? command.GitProjectName
            : command.GitProjectFolderName;
    }
}
