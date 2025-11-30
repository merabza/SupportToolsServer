using SupportToolsServerApiContracts.Models;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;

namespace SupportToolsServerMappers;

public static class GitIgnoreFileTypeMapper
{
    public static StsGitIgnoreFileTypeDataModel ToContractModel(this GitIgnoreFileTypeDto gitIgnoreFileType)
    {
        return new StsGitIgnoreFileTypeDataModel
        {
            Name = gitIgnoreFileType.Name, Content = gitIgnoreFileType.Content
        };
    }

    public static GitIgnoreFileTypeForSave AdaptTo(this StsGitIgnoreFileTypeDataModel gitIgnoreFileTypeModel)
    {
        return new GitIgnoreFileTypeForSave
        {
            Name = gitIgnoreFileTypeModel.Name, Content = gitIgnoreFileTypeModel.Content
        };
    }
}