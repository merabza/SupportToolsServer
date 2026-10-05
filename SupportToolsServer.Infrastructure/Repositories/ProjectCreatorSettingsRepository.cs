using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.Settings;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class ProjectCreatorSettingsRepository : IProjectCreatorSettingsRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public ProjectCreatorSettingsRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //ჩანაწერი თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update-ით რეგისტრირდება. ცხრილში მეორე ჩანაწერს
    //გასაღების CHECK კრძალავს, ამიტომ ერთადერთის წაკითხვას ფილტრი არ სჭირდება
    public Task<ProjectCreatorSettings?> Get(CancellationToken cancellationToken)
    {
        return _dbContext.ProjectCreatorSettings.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    }

    public void Add(ProjectCreatorSettings projectCreatorSettings)
    {
        _dbContext.ProjectCreatorSettings.Add(projectCreatorSettings);
    }

    public void Update(ProjectCreatorSettings projectCreatorSettings)
    {
        _dbContext.ProjectCreatorSettings.Update(projectCreatorSettings).ExpectPreviousVersion();
    }
}
