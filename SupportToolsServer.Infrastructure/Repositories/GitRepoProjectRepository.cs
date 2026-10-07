using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.GitRepoProjects;
using SupportToolsServerCore.Domain.GitRepos;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class GitRepoProjectRepository : IGitRepoProjectRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public GitRepoProjectRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //პროექტები დამოკიდებულებებით და თვალყურის დევნების გარეშე იკითხება
    public Task<List<GitRepoProject>> GetAll(CancellationToken cancellationToken)
    {
        return _dbContext.GitRepoProjects.AsNoTracking().Include(x => x.Dependencies).ToListAsync(cancellationToken);
    }

    //რეპოზიტორიის შენახული პროექტები დამოკიდებულებებით თვალყურის დევნით იკითხება და წასაშლელად მოინიშნება, ახლები კი
    //ემატება. შენახვისას EF ძველ სტრიქონებს ახლების ჩასმამდე შლის, ამიტომ იგივე ფაილი უნიკალურ ინდექსს არ არღვევს
    public async Task Replace(GitRepoId gitRepoId, IEnumerable<GitRepoProject> gitRepoProjects,
        CancellationToken cancellationToken)
    {
        List<GitRepoProject> storedGitRepoProjects = await _dbContext.GitRepoProjects.Include(x => x.Dependencies)
            .Where(x => x.GitRepoId == gitRepoId).ToListAsync(cancellationToken);
        _dbContext.GitRepoProjects.RemoveRange(storedGitRepoProjects);
        _dbContext.GitRepoProjects.AddRange(gitRepoProjects);
    }
}
