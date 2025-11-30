using System.Threading;
using System.Threading.Tasks;
using Dapper;
using OneOf;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Repositories.Gits;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;
using SupportToolsServerApplication.Services.Gits.Models;
using SystemToolsShared.Errors;

namespace SupportToolsServerCommandRepositories;

public sealed class GitsCommandsRepository : IGitsCommandsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IGitIgnoreFileTypesCommandsRepository _gitIgnoreFileTypesCommandsRepo;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitsCommandsRepository(IGitIgnoreFileTypesCommandsRepository gitIgnoreFileTypesCommandsRepo,
        IDbConnectionFactory connectionFactory)
    {
        _gitIgnoreFileTypesCommandsRepo = gitIgnoreFileTypesCommandsRepo;
        _connectionFactory = connectionFactory;
    }

    //public async Task AddGit(GitData gitData, CancellationToken cancellationToken = default)
    //{
    //    await _dbContext.GitData.AddAsync(gitData, cancellationToken);
    //}

    public async Task<OneOf<int, Err[]>> UpdateGitRepo(GitDataForSave gitDataDto, CancellationToken cancellationToken)
    {
        var giftId = await _gitIgnoreFileTypesCommandsRepo.UpdateGitIgnoreFileType(
            new GitIgnoreFileTypeForSave { Name = gitDataDto.GitIgnorePathName }, cancellationToken);

        // ReSharper disable once using
        using var dbConnection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        return await dbConnection.QuerySingleOrDefaultAsync<int>("""
                                                                 IF NOT EXISTS (SELECT * FROM GitData WHERE [GdName] = @GitProjectName)
                                                                 	BEGIN
                                                                 		INSERT INTO GitData ( 
                                                                 		    GdName, 
                                                                 		    GdGitAddress, 
                                                                 		    GdFolderName,
                                                                            GitIgnoreFileTypeId
                                                                        )
                                                                 		VALUES (
                                                                 		    @GitProjectName, 
                                                                 		    @GitProjectAddress, 
                                                                 		    @GitProjectFolderName,
                                                                 		    @GitIgnoreFileTypeId
                                                                 		)
                                                                 	END
                                                                 ELSE
                                                                 	BEGIN
                                                                 		UPDATE GitData SET 
                                                                 		    GdGitAddress = @GitProjectAddress, 
                                                                 		    GdFolderName = @GitProjectFolderName,
                                                                            GitIgnoreFileTypeId = @GitIgnoreFileTypeId
                                                                 		WHERE GdName = @GitProjectName
                                                                 	END
                                                                 SELECT Id FROM GitData WHERE GdName = @GitProjectName
                                                                 """,
            new
            {
                gitDataDto.GitProjectName,
                gitDataDto.GitProjectAddress,
                gitDataDto.GitProjectFolderName,
                GitIgnoreFileTypeId = giftId
            });
    }

    public async Task DeleteGitRepo(string gitKey, CancellationToken cancellationToken)
    {
        // ReSharper disable once using
        using var dbConnection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await dbConnection.ExecuteAsync("DELETE FROM GitData WHERE GdName = @gitKey", new { gitKey });
    }
}