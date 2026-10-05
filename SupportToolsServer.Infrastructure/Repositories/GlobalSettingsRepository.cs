using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.Settings;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class GlobalSettingsRepository : IGlobalSettingsRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public GlobalSettingsRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //ჩანაწერი თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update-ით რეგისტრირდება. ცხრილში მეორე ჩანაწერს
    //გასაღების CHECK კრძალავს, ამიტომ ერთადერთის წაკითხვას ფილტრი არ სჭირდება
    public Task<GlobalSettings?> Get(CancellationToken cancellationToken)
    {
        return _dbContext.GlobalSettings.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    }

    public void Add(GlobalSettings globalSettings)
    {
        _dbContext.GlobalSettings.Add(globalSettings);
    }

    //owned DatabasesBackupFilesExchange იმავე UPDATE-ში იწერება, რომელსაც Version-ის შემოწმება აქვს
    public void Update(GlobalSettings globalSettings)
    {
        _dbContext.GlobalSettings.Update(globalSettings).ExpectPreviousVersion();
    }
}
