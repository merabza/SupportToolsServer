//Created by RepositoryClassCreator at 2/4/2025 7:31:10 PM

using System;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SupportToolsServerDb;

namespace LibSupportToolsServerRepositories;

public sealed class SupportToolsServerRepository : ISupportToolsServerRepository
{
    private readonly SupportToolsServerDbContext _context;
    private readonly ILogger<SupportToolsServerRepository> _logger;

    public SupportToolsServerRepository(SupportToolsServerDbContext ctx, ILogger<SupportToolsServerRepository> logger)
    {
        _context = ctx;
        _logger = logger;
    }

    public int SaveChanges()
    {
        try
        {
            return _context.SaveChanges();
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error occurred executing {nameof(SaveChanges)}.");
            throw;
        }
    }

    public int SaveChangesWithTransaction()
    {
        try
        {
            // ReSharper disable once using
            using var transaction = GetTransaction();
            try
            {
                var ret = _context.SaveChanges();
                transaction.Commit();
                return ret;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error occurred executing {nameof(SaveChangesWithTransaction)}.");
            throw;
        }
    }

    public IDbContextTransaction GetTransaction()
    {
        return _context.Database.BeginTransaction();
    }
}