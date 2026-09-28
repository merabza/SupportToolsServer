using System;
using System.Collections.Generic;
using System.Linq;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitIgnoreFileTypes;

//gitignore ფაილის ტიპი მხოლოდ მაშინ იშლება, როცა მას არც ერთი git რეპოზიტორია არ იყენებს
internal static class GitIgnoreFileTypeDeletion
{
    public static Result CheckNotUsed(IEnumerable<GitIgnoreFileType> gitIgnoreFileTypes, List<GitRepo> gitRepos)
    {
        string[] usages = [.. gitIgnoreFileTypes.Select(x => DescribeUsage(x, gitRepos)).OfType<string>()];
        return usages.Length == 0
            ? Result.Success()
            : Result.Failure(SupportToolsServerApiClientErrors.GitIgnoreFileTypeIsInUse(string.Join("; ", usages)));
    }

    private static string? DescribeUsage(GitIgnoreFileType gitIgnoreFileType, List<GitRepo> gitRepos)
    {
        string[] gitNames =
        [
            .. gitRepos.Where(x => x.GitIgnoreFileTypeId == gitIgnoreFileType.Id).Select(x => x.Name)
                .Order(StringComparer.OrdinalIgnoreCase)
        ];
        return gitNames.Length == 0 ? null : $"{gitIgnoreFileType.Name} ({string.Join(", ", gitNames)})";
    }
}
