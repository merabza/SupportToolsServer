using System.Threading;
using System.Threading.Tasks;
using Dapper;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;

namespace SupportToolsServerCommandRepositories;

public sealed class GitIgnoreFileTypesCommandsRepository : IGitIgnoreFileTypesCommandsRepository
{
    //private readonly SupportToolsServerDbContext _dbContext;
    private readonly IDbConnectionFactory _connectionFactory;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitIgnoreFileTypesCommandsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    //public async Task AddGitIgnorePath(GitIgnoreFileType newGitIgnorePath,
    //    CancellationToken cancellationToken = default)
    //{
    //    await _dbContext.GitIgnoreFileTypes.AddAsync(newGitIgnorePath, cancellationToken);
    //}

    public async Task<int> UpdateGitIgnoreFileType(GitIgnoreFileTypeForSave dbGitIgnorePath,
        CancellationToken cancellationToken = default)
    {
        // ReSharper disable once using
        using var dbConnection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        return await dbConnection.QuerySingleOrDefaultAsync<int>("""
                                                          IF NOT EXISTS (SELECT * FROM GitIgnoreFileType WHERE [Name] = @Name)
                                                          	BEGIN
                                                          		INSERT INTO GitIgnoreFileType ( [Name], [Content])
                                                          		VALUES (@Name, @Content)
                                                          	END
                                                          ELSE
                                                          	BEGIN
                                                          		UPDATE GitIgnoreFileType SET [Content] = @Content
                                                          		WHERE [Name] = @Name
                                                          	END
                                                          SELECT Id FROM GitIgnoreFileType WHERE [Name] = @Name
                                                          """, dbGitIgnorePath);
    }

    //public async Task AddGitIgnorePath(GitIgnoreFileType newGitIgnorePath,
    //    CancellationToken cancellationToken = default)
    //{
    //    await _dbContext.GitIgnoreFileTypes.AddAsync(newGitIgnorePath, cancellationToken);
    //}

    //public void UpdateGitIgnorePath(GitIgnoreFileType dbGitIgnorePath, CancellationToken cancellationToken = default)
    //{
    //    _dbContext.GitIgnoreFileTypes.Update(dbGitIgnorePath);
    //}

    //public async Task AddGit(GitData gitData, CancellationToken cancellationToken = default)
    //{
    //    await _dbContext.GitData.AddAsync(gitData, cancellationToken);
    //}

    //public async Task UpdateGitRepo(string gitKey, GitDataDto requestNewRecord, CancellationToken cancellationToken)
    //{
    //    var gitIgnoreFileType =
    //        await _dbContext.GitIgnoreFileTypes.FirstOrDefaultAsync(x => x.Name == requestNewRecord.GitIgnorePathName,
    //            cancellationToken);

    //    if (gitIgnoreFileType is null)
    //    {
    //        gitIgnoreFileType = new GitIgnoreFileType { Name = requestNewRecord.GitIgnorePathName };
    //        await _dbContext.GitIgnoreFileTypes.AddAsync(gitIgnoreFileType, cancellationToken);
    //    }

    //    var gitData = await _dbContext.GitData.FirstOrDefaultAsync(x => x.GdName == gitKey, cancellationToken);

    //    if (gitData is null)
    //    {
    //        var newGitData = new GitData
    //        {
    //            GdName = gitKey,
    //            GdGitAddress = requestNewRecord.GitProjectAddress,
    //            GdFolderName = requestNewRecord.GitProjectFolderName,
    //            GitIgnoreFileTypeNavigation = gitIgnoreFileType
    //        };
    //        await _dbContext.GitData.AddAsync(newGitData, cancellationToken);
    //    }
    //    else
    //    {
    //        gitData.GdGitAddress = requestNewRecord.GitProjectAddress;
    //        gitData.GdFolderName = requestNewRecord.GitProjectFolderName;
    //        gitData.GitIgnoreFileTypeNavigation = gitIgnoreFileType;
    //        _dbContext.GitData.Update(gitData);
    //    }

    //    await _dbContext.SaveChangesAsync(cancellationToken);
    //}

    //public async Task DeleteGitRepo(string gitKey, CancellationToken cancellationToken)
    //{
    //    var gitData = await _dbContext.GitData.FirstOrDefaultAsync(x => x.GdName == gitKey, cancellationToken);
    //    if (gitData is null)
    //        return;
    //    _dbContext.GitData.Remove(gitData);
    //    await _dbContext.SaveChangesAsync(cancellationToken);
    //}
}